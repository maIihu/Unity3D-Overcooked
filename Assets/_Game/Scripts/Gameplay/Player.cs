using Counter;
using Kitchen;
using UnityEngine;
using Fusion;
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

        // Local-only state
        private Vector2 _moveInput;
        private Vector3 _lastInteractDir;
        private BaseCounter _selectedCounter;
        private KitchenObject _kitchenObject;

        // Canonical rotation — dùng làm source cho Slerp thay vì transform.rotation
        // (transform.rotation có thể bị NetworkTransform interpolate, tạo feedback loop giật)
        private Quaternion _currentRotation;

        // Cutting state
        private CuttingCounter _currentCuttingCounter;
        private bool _isCutting;

        // -------------------------------------------------------
        #region Networked Properties

        // Sync animation sang remote clients
        [Networked] public Vector2 NetworkMoveInput { get; set; }

        // Sync hướng nhìn cuối sang remote clients (dùng cho rotation)
        [Networked] public Vector3 NetworkTargetForward { get; set; }

        [Networked]
        [OnChangedRender(nameof(OnColorChanged))]
        public EPlayerColor PlayerColor { get; set; }

        [Networked] public NetworkBool NetworkIsChopping { get; set; }
        [Networked] public NetworkBool NetworkIsHoldingObject { get; set; }

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
            if (bodyRend == null) return;
            Color targetColor = GetColorByEnum(color);
            Material[] mats = bodyRend.materials;
            if (mats.Length > 1 && mats[1] != null)
                mats[1].color = targetColor;
            bodyRend.materials = mats;
        }

        private void OnColorChanged()
        {
            UpdateVisualColor(PlayerColor);
        }

        #endregion

        // -------------------------------------------------------
        #region Fusion Lifecycle

        public override void Spawned()
        {
            // Khởi tạo canonical rotation từ transform thực, không phải từ interpolated value
            _currentRotation = transform.rotation;

            // Snap xuống sàn ngay khi spawn (tránh spawn trên không rồi rơi xuống vô tận)
            // Chỉ thực hiện trên local player — remote player sẽ dùng vị trí sync từ network
            if ((HasInputAuthority || HasStateAuthority) && _rb != null)
            {
                int groundMask = ~(1 << gameObject.layer);
                if (Physics.Raycast(_rb.position + Vector3.up * 5f, Vector3.down,
                        out RaycastHit groundHit, 10f, groundMask))
                {
                    // Dừng hẳn velocity trước khi snap — tránh bị nẩy lên sau snap
                    _rb.velocity = Vector3.zero;
                    _rb.position = new Vector3(_rb.position.x, groundHit.point.y, _rb.position.z);
                }
            }

            NetworkTargetForward = transform.forward;
            _lastInteractDir = transform.forward;
            UpdateVisualColor(PlayerColor);
        }

        public override void FixedUpdateNetwork()
        {
            // Chỉ owner chạy movement prediction
            if (!HasStateAuthority && !HasInputAuthority) return;

            NetworkInputData inputData = default;
            bool hasInput = GetInput(out inputData);

            if (hasInput)
            {
                _moveInput = new Vector2(inputData.MoveX, inputData.MoveY);

                if (HasStateAuthority)
                    NetworkMoveInput = _moveInput;
            }

            // Di chuyển dựa trên _moveInput đã cache (kể cả resimulation ticks)
            Move();

            if ((HasStateAuthority || HasInputAuthority) && hasInput)
            {
                HandleInteractions(inputData);

                if (HasStateAuthority)
                {
                    if (_moveInput != Vector2.zero && _isCutting)
                        StopCutting();
                }
            }

            if (HasStateAuthority && _isCutting && _currentCuttingCounter != null)
                _currentCuttingCounter.InteractAlternate(this);
        }

        private void Update()
        {
            if (Object == null || !Object.IsValid) return;
            UpdateAnimation();
        }

        // Render() KHÔNG override ở đây.
        // NetworkTransform component đã xử lý interpolation vị trí giữa các tick.
        // Rigidbody.interpolation = Interpolate xử lý visual smoothness giữa physics steps.
        // Override Render() để set transform.position sẽ CONFLICT với NetworkTransform!

        #endregion

        // -------------------------------------------------------
        #region Move

        private void Move()
        {
            if (_rb == null) return;

            Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (moveDir != Vector3.zero)
            {
                // Set velocity ngang, GIỮ NGUYÊN velocity.y để gravity hoạt động bình thưỜng.
                _rb.velocity = new Vector3(
                    moveDir.x * moveSpeed,
                    _rb.velocity.y,
                    moveDir.z * moveSpeed
                );

                _lastInteractDir = moveDir.normalized;

                if (HasStateAuthority)
                    NetworkTargetForward = _lastInteractDir;

                // FIX ROTATION JITTER:
                // Dùng _currentRotation (canonical, stable) làm source của Slerp.
                // KHAIÔNG dùng transform.rotation — nó đang bị NetworkTransform interpolate,
                // tạo feedback loop: đọc interpolated → Slerp → ghi lại → NT đọc lại → giật.
                Quaternion targetRot = Quaternion.LookRotation(_lastInteractDir);
                _currentRotation = Quaternion.Slerp(
                    _currentRotation,   // ← stable canonical value
                    targetRot,
                    Runner.DeltaTime * rotateSpeed
                );
                transform.rotation = _currentRotation;
            }
            else
            {
                // Đứng yên: triệt tiêu velocity ngang, giữ velocity.y (gravity tiếp tục)
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
            }
        }

        private void UpdateAnimation()
        {
            Vector2 inputToUse = (HasStateAuthority || HasInputAuthority)
                ? _moveInput
                : NetworkMoveInput;

            animator.SetFloat("MovingValue", inputToUse.magnitude);
            animator.SetBool("IsChopping", NetworkIsChopping);
            animator.SetBool("HasObject", NetworkIsHoldingObject);
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

                    if (HasStateAuthority)
                    {
                        if (inputData.IsInteractPressed)
                        {
                            if ((!HasKitchenObject() && _selectedCounter is ContainerCounter) ||
                                (HasKitchenObject() && _selectedCounter is ClearCounter))
                            {
                                animator.SetTrigger("IsPicked");
                            }

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
                    }
                    return;
                }

                if (hit.collider.TryGetComponent(out KitchenObject kitchenObject))
                {
                    if (kitchenObject != GetKitchenObject())
                    {
                        SetSelectedCounter(null);
                        if (HasStateAuthority && inputData.IsInteractPressed &&
                            !HasKitchenObject() &&
                            kitchenObject.GetKitchenObjectParent() == null)
                        {
                            kitchenObject.SetKitchenObjectParent(this);
                            NetworkIsHoldingObject = true;
                            animator.SetTrigger("IsPicked");
                        }
                        return;
                    }
                }
            }

            SetSelectedCounter(null);
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

            _isCutting = true;
            _currentCuttingCounter = counter;
            _currentCuttingCounter.OnCutComplete += StopCutting;
            _currentCuttingCounter.CuttingSoundAndAnimation();
            NetworkIsChopping = true;
        }

        private void StopCutting()
        {
            NetworkIsChopping = false;
            _isCutting = false;
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
            Vector3 origin = interactPoint.position;
            Vector3 dir = _lastInteractDir == Vector3.zero ? transform.forward : _lastInteractDir.normalized;
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