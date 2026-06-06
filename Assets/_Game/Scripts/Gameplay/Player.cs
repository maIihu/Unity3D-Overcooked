using Counter;
using Kitchen;
using UnityEngine;
using Fusion;
using GameCore;
using GameCore.Network;

namespace _Game.Scripts.Gameplay
{
    public class Player : NetworkBehaviour, IKitchenObjectParent, IPlayer
    {
        [Header("Player Stats")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float rotateSpeed = 10f;
        [SerializeField] private float interactDistance = 1f;
        [SerializeField] private float sphereRadius = 0.5f;

        [Header("References")]
        [SerializeField] private Transform _handPoint;
        [SerializeField] private Transform interactPoint;

        [Header("Components")]
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Renderer bodyRend;

        // ── Animator hash cache (static → chỉ tính 1 lần cho tất cả instances) ──
        private static readonly int s_MovingValue = Animator.StringToHash("MovingValue");
        private static readonly int s_IsChopping  = Animator.StringToHash("IsChopping");
        private static readonly int s_HasObject   = Animator.StringToHash("HasObject");
        private static readonly int s_IsPicked    = Animator.StringToHash("IsPicked");

        // ── Local-only state ─────────────────────────────────────────────────────
        private Vector2    _moveInput;
        private Vector3    _lastInteractDir;
        private BaseCounter _selectedCounter;
        private KitchenObject _kitchenObject;

        // Canonical rotation — dùng làm source cho Slerp, KHÔNG đọc _rb.rotation
        // (đọc _rb.rotation trong FixedUpdateNetwork có thể trả về giá trị interpolated → feedback loop giật)
        [Networked] private Quaternion _currentRotation { get; set; }

        // Cached material (tránh clone Material[] array mỗi lần UpdateVisualColor)
        private Material _bodyMaterial;

        // ── Cutting state ─────────────────────────────────────────────────────────
        private CuttingCounter _currentCuttingCounter;
        private bool           _isCutting;

        // -------------------------------------------------------
        #region Networked Properties

        // Sync animation sang remote clients
        [Networked] public Vector2 NetworkMoveInput { get; set; }

        // Sync hướng nhìn cuối sang remote clients (dùng cho rotation)
        [Networked] public Vector3 NetworkTargetForward { get; set; }

        [Networked]
        [OnChangedRender(nameof(OnColorChanged))]
        public EPlayerColor PlayerColor { get; set; }

        [Networked] public NetworkBool NetworkIsChopping      { get; set; }
        [Networked] public NetworkBool NetworkIsHoldingObject { get; set; }

        // Toggle để trigger OnChangedRender — remote client sẽ thấy animation IsPicked
        [Networked]
        [OnChangedRender(nameof(OnPickedChanged))]
        public NetworkBool NetworkIsPicked { get; set; }

        #endregion

        // -------------------------------------------------------
        #region Color System

        private static readonly Color[] s_PlayerColors = new Color[]
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            new Color(0.5f, 0f, 0.5f), // Purple
            new Color(1f, 0.5f, 0f),   // Orange
        };

        public static Color GetColorByEnum(EPlayerColor color)
        {
            int idx = (int)color;
            if (idx < 0 || idx >= s_PlayerColors.Length) return Color.white;
            return s_PlayerColors[idx];
        }

        public void UpdateVisualColor(EPlayerColor color)
        {
            if (_bodyMaterial == null) return;
            _bodyMaterial.color = GetColorByEnum(color);
            // Không cần set lại bodyRend.materials → tránh clone array + GC pressure
        }

        private void OnColorChanged()
        {
            UpdateVisualColor(PlayerColor);
        }

        private void OnPickedChanged()
        {
            // Chạy trên tất cả client (kể cả remote) nhờ OnChangedRender
            animator.SetTrigger(s_IsPicked);
        }

        #endregion

        // -------------------------------------------------------
        #region Fusion Lifecycle

        public override void Spawned()
        {
            Runner.SetIsSimulated(Object, true);

            // Snap xuống sàn ngay khi spawn
            // Chỉ thực hiện trên local player — remote player dùng vị trí sync từ network
            if ((HasInputAuthority || HasStateAuthority) && _rb != null)
            {
                int groundMask = ~(1 << gameObject.layer);
                if (Physics.Raycast(_rb.position + Vector3.up * 5f, Vector3.down,
                        out RaycastHit groundHit, 10f, groundMask))
                {
                    _rb.velocity = Vector3.zero;
                    _rb.position = new Vector3(_rb.position.x, groundHit.point.y, _rb.position.z);
                }
            }

            // Khởi tạo canonical rotation từ transform (đã được set ở spawn point)
            _currentRotation = transform.rotation;

            NetworkTargetForward = transform.forward;
            _lastInteractDir     = transform.forward;

            // Cache material instance (đã được clone sẵn bởi Unity lần đầu dùng bodyRend.materials)
            if (bodyRend != null && bodyRend.materials.Length > 1)
                _bodyMaterial = bodyRend.materials[1];

            UpdateVisualColor(PlayerColor);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Unsubscribe event để tránh NullReferenceException khi despawn trong lúc đang cắt
            StopCutting();
        }

        public override void FixedUpdateNetwork()
        {
            // Remote spectators (không có input, không có state authority) → bỏ qua
            if (!HasStateAuthority && !HasInputAuthority) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != EGameState.Play)
            {
                _moveInput = Vector2.zero;
                if (HasStateAuthority)
                    NetworkMoveInput = Vector2.zero;
                if (_rb != null)
                {
                    _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                }
                if (_isCutting)
                    StopCutting();
                return;
            }

            if (GetInput(out NetworkInputData inputData))
            {
                // ClampMagnitude để tránh client gửi giá trị > 1 (cheating/diagonal speed boost)
                _moveInput = Vector2.ClampMagnitude(
                    new Vector2(inputData.MoveX, inputData.MoveY), 1f
                );

                // CHỈ Host sync NetworkMoveInput → dùng cho remote client render animation
                if (HasStateAuthority)
                    NetworkMoveInput = _moveInput;

                HandleInteractions(inputData);

                if (_moveInput != Vector2.zero && _isCutting)
                    StopCutting();
            }
            // Khi GetInput() trả false (resimulation tick chưa xác nhận),
            // KHÔNG reset _moveInput → Player giữ nguyên hướng di chuyển cuối cùng,
            // tránh micro-stop gây giật cục.

            // Di chuyển: cả Host (HasStateAuthority) và Client (HasInputAuthority) đều chạy
            // → Client có client-side prediction, Host có authoritative simulation
            Move();

            if (_isCutting && _currentCuttingCounter != null)
                _currentCuttingCounter.InteractAlternate(this);
        }

        private void Update()
        {
            if (Object == null || !Object.IsValid) return;
            UpdateAnimation();
        }

        // Render() KHÔNG override ở đây.
        // NetworkRigidbody3D tự động lo việc nội suy mượt mà cho Rigidbody.

        #endregion

        // -------------------------------------------------------
        #region Move

        private void Move()
        {
            if (_rb == null) return;

            // Đảm bảo client có quyền điều khiển thì vật lý phải chạy (không bị Kinematic đè)
            if (HasInputAuthority && _rb.isKinematic)
            {
                _rb.isKinematic = false;
            }

            Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (moveDir != Vector3.zero)
            {
                if (_rb.IsSleeping()) _rb.WakeUp(); // Đánh thức Rigidbody để tránh Server bị trễ nhịp do Physics ngủ đông

                // Set velocity ngang, GIỮ NGUYÊN velocity.y để gravity hoạt động bình thường
                _rb.velocity = new Vector3(
                    moveDir.x * moveSpeed,
                    _rb.velocity.y,
                    moveDir.z * moveSpeed
                );

                _lastInteractDir = moveDir.normalized;

                if (HasStateAuthority)
                    NetworkTargetForward = _lastInteractDir;

                // Dùng _currentRotation làm source (KHÔNG dùng _rb.rotation — có thể bị interpolated)
                // Kỹ thuật này học từ PlayerLocal.cs tránh feedback loop giật với Rigidbody interpolation
                Quaternion targetRot = Quaternion.LookRotation(_lastInteractDir);

                // Khôi phục Slerp mượt mà. Vì _currentRotation nay đã là [Networked],
                // nó sẽ được Rollback và Resimulate cực kỳ chính xác! Không còn bị lỗi giật xoay người nhanh!
                _currentRotation = Quaternion.Slerp(
                    _currentRotation,
                    targetRot,
                    Runner.DeltaTime * rotateSpeed
                );
                _rb.MoveRotation(_currentRotation);
            }
            else
            {
                // Đứng yên: triệt tiêu velocity ngang, giữ velocity.y (gravity tiếp tục)
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
            }
        }

        private void UpdateAnimation()
        {
            // Local player dùng _moveInput trực tiếp (đã cập nhật ở FixedUpdateNetwork)
            // Remote player dùng NetworkMoveInput được sync từ Host
            Vector2 inputToUse = (HasStateAuthority || HasInputAuthority)
                ? _moveInput
                : NetworkMoveInput;

            animator.SetFloat(s_MovingValue, inputToUse.magnitude);
            animator.SetBool(s_IsChopping, NetworkIsChopping);
            animator.SetBool(s_HasObject, NetworkIsHoldingObject);
        }

        #endregion

        // -------------------------------------------------------
        #region Interactions

        private void HandleInteractions(NetworkInputData inputData)
        {
            Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (moveDir != Vector3.zero)
                _lastInteractDir = moveDir;

            if (_lastInteractDir == Vector3.zero)
                _lastInteractDir = transform.forward;

            if (Physics.SphereCast(
                    interactPoint.position,
                    sphereRadius,
                    _lastInteractDir,
                    out RaycastHit hit,
                    interactDistance))
            {
                if (hit.collider.TryGetComponent(out BaseCounter baseCounter))
                {
                    if (_selectedCounter != baseCounter)
                        SetSelectedCounter(baseCounter);

                    if (inputData.IsInteractPressed)
                    {
                        HandlePickupAnimation(baseCounter);
                        baseCounter.Interact(this);
                        NetworkIsHoldingObject = HasKitchenObject();
                    }

                    if (inputData.IsAlternatePressed && _moveInput == Vector2.zero)
                    {
                        if (baseCounter is CuttingCounter cuttingCounter &&
                            cuttingCounter.HasKitchenObject() &&
                            cuttingCounter.GetKitchenObject() is FoodObject { FoodState: FoodState.Normal })
                        {
                            StartCutting(cuttingCounter);
                        }
                    }
                    return;
                }

                if (hit.collider.TryGetComponent(out KitchenObject kitchenObject))
                {
                    if (kitchenObject != GetKitchenObject())
                    {
                        SetSelectedCounter(null);
                        if (inputData.IsInteractPressed &&
                            !HasKitchenObject() &&
                            kitchenObject.GetKitchenObjectParent() == null)
                        {
                            kitchenObject.SetKitchenObjectParent(this);
                            NetworkIsHoldingObject = true;
                            // Toggle NetworkIsPicked → OnPickedChanged sẽ fire trên tất cả clients
                            NetworkIsPicked = !NetworkIsPicked;
                        }
                        return;
                    }
                }
            }

            SetSelectedCounter(null);
        }

        private void HandlePickupAnimation(BaseCounter baseCounter)
        {
            // Toggle NetworkIsPicked để trigger OnChangedRender trên tất cả clients
            bool shouldTrigger = (!HasKitchenObject() && baseCounter is ContainerCounter) ||
                                 (HasKitchenObject()  && baseCounter is ClearCounter);
            if (shouldTrigger)
                NetworkIsPicked = !NetworkIsPicked;
        }

        private void SetSelectedCounter(BaseCounter baseCounter)
        {
            _selectedCounter?.Hide();
            _selectedCounter = baseCounter;
            _selectedCounter?.Show();
        }

        #endregion

        // -------------------------------------------------------
        #region Cutting

        private void StartCutting(CuttingCounter counter)
        {
            if (_isCutting && _currentCuttingCounter == counter) return;

            if (_isCutting) StopCutting();

            _isCutting              = true;
            _currentCuttingCounter  = counter;
            _currentCuttingCounter.OnCutComplete += StopCutting;
            _currentCuttingCounter.CuttingSoundAndAnimation();
            NetworkIsChopping = true;
        }

        private void StopCutting()
        {
            NetworkIsChopping = false;
            _isCutting        = false;
            if (_currentCuttingCounter != null)
            {
                _currentCuttingCounter.OnCutComplete -= StopCutting;
                _currentCuttingCounter.StopAnimationCut();
                _currentCuttingCounter = null;
            }
        }

        #endregion

        // -------------------------------------------------------
        #region IKitchenObjectParent

        public Transform GetKitchenObjectToTransform() => _handPoint;
        public void SetKitchenObject(KitchenObject kitchenObject) => _kitchenObject = kitchenObject;
        public KitchenObject GetKitchenObject() => _kitchenObject;
        public void ClearKitchenObject() => _kitchenObject = null;
        public bool HasKitchenObject() => _kitchenObject != null;
        public Fusion.NetworkObject GetNetworkObject() => Object;

        #endregion

        // -------------------------------------------------------
        #region Gizmos

        private void OnDrawGizmos()
        {
            if (interactPoint == null) return;
            Vector3 origin   = interactPoint.position;
            Vector3 dir      = _lastInteractDir == Vector3.zero ? transform.forward : _lastInteractDir.normalized;
            Vector3 endPoint = origin + dir * interactDistance;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, sphereRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint, sphereRadius);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(origin, endPoint);
        }

        #endregion
    }
}