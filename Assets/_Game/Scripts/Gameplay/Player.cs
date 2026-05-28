using Counter;
using Kitchen;
using UnityEngine;
using Fusion;
using GameCore.Network;

namespace _Game.Scripts.Gameplay
{
    public class Player : NetworkBehaviour, IKitchenObjectParent
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

        // Local-only state (chỉ dùng trên client sở hữu)
        private Vector2 _moveInput;
        private Vector3 _lastInteractDir;
        private BaseCounter _selectedCounter;
        private KitchenObject _kitchenObject;

        // Cutting state — local tracking, logic chạy trong FixedUpdateNetwork
        private CuttingCounter _currentCuttingCounter;
        private bool _isCutting;

        // -------------------------------------------------------
        #region Networked Properties

        // Sync animation sang remote clients
        [Networked] public Vector2 NetworkMoveInput { get; set; }

        // Sync rotation sang remote clients
        [Networked] public Vector3 NetworkTargetForward { get; set; }

        [Networked]
        [OnChangedRender(nameof(OnColorChanged))]
        public EPlayerColor PlayerColor { get; set; }

        [Networked] public NetworkBool NetworkIsChopping { get; set; }
        [Networked] public NetworkBool NetworkIsHoldingObject { get; set; }

        #endregion

        // -------------------------------------------------------
        #region Color System

        // Static readonly: tránh allocation mỗi lần gọi
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
            {
                mats[1].color = targetColor;
            }
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
            if (_rb != null)
                _rb.isKinematic = !(HasStateAuthority || HasInputAuthority);

            NetworkTargetForward = transform.forward;
            _lastInteractDir = transform.forward;
            UpdateVisualColor(PlayerColor);
        }

        public override void FixedUpdateNetwork()
        {
            // Chỉ owner (HasStateAuthority hoặc HasInputAuthority) mới chạy prediction
            if (!HasStateAuthority && !HasInputAuthority) return;

            // --- Cập nhật input từ Fusion ---
            // GetInput() trả về true khi có dữ liệu mới (không phải resimulation).
            // Ta cache _moveInput để Move() luôn dùng giá trị mới nhất,
            // kể cả trên các tick resimulation khi GetInput() trả về false.
            NetworkInputData inputData = default;
            bool hasInput = GetInput(out inputData);

            if (hasInput)
            {
                _moveInput = new Vector2(inputData.MoveX, inputData.MoveY);

                if (HasStateAuthority)
                {
                    NetworkMoveInput = _moveInput;
                }
            }

            // --- Luôn di chuyển dựa trên _moveInput đã cache ---
            Move();

            // --- Chỉ StateAuthority xử lý game logic, nhưng InputAuthority cũng chạy để hiển thị Highlight ---
            if ((HasStateAuthority || HasInputAuthority) && hasInput)
            {
                HandleInteractions(inputData);

                if (HasStateAuthority)
                {
                    // Dừng cắt nếu người chơi di chuyển
                    if (_moveInput != Vector2.zero && _isCutting)
                        StopCutting();
                }
            }

            // --- Cutting tick: chạy mỗi fixed tick thay vì Coroutine ---
            if (HasStateAuthority && _isCutting && _currentCuttingCounter != null)
            {
                _currentCuttingCounter.InteractAlternate(this);
            }
        }

        private void Update()
        {
            if (Object == null || !Object.IsValid) return;
            UpdateAnimation();
        }

        #endregion

        // -------------------------------------------------------
        #region Move

        private void Move()
        {
            Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (_rb != null)
            {
                Vector3 targetVelocity = moveDir * moveSpeed;
                targetVelocity.y = _rb.velocity.y; // Giữ trọng lực
                _rb.velocity = targetVelocity;
            }

            if (moveDir != Vector3.zero)
            {
                _lastInteractDir = moveDir.normalized;

                if (HasStateAuthority)
                {
                    NetworkTargetForward = _lastInteractDir;
                }

                Quaternion targetRot = Quaternion.LookRotation(_lastInteractDir);
                _rb.MoveRotation(
                    Quaternion.Slerp(_rb.rotation, targetRot, Runner.DeltaTime * rotateSpeed)
                );
            }
        }

        private void UpdateAnimation()
        {
            // Remote clients dùng NetworkMoveInput để animate (không có GetInput)
            Vector2 inputToUse = (HasStateAuthority || HasInputAuthority) ? _moveInput : NetworkMoveInput;
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
                        // Interact (Space)
                        if (inputData.IsInteractPressed)
                        {
                            if ((!HasKitchenObject() && _selectedCounter is ContainerCounter) ||
                                (HasKitchenObject() && _selectedCounter is ClearCounter))
                            {
                                // Animation logic runs locally via network state later, 
                                // but we trigger the trigger parameter manually here (could be networked too if needed)
                                animator.SetTrigger("IsPicked");
                            }

                            baseCounter.Interact(this);
                            NetworkIsHoldingObject = HasKitchenObject();
                        }

                        // Alternate / Bắt đầu cắt (R) — chỉ khi đứng yên
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

                // Nhặt vật phẩm nằm dưới đất
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

            // Thả đồ khi không nhìn vào bàn
            if (HasStateAuthority && inputData.IsInteractPressed && HasKitchenObject())
                DropKitchenObject();
        }

        private void SetSelectedCounter(BaseCounter baseCounter)
        {
            _selectedCounter?.Hide();
            _selectedCounter = baseCounter;
            _selectedCounter?.Show();
        }

        private void DropKitchenObject()
        {
            KitchenObject kitchenObject = GetKitchenObject();
            if (kitchenObject == null) return;

            Vector3 dropPosition = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
            kitchenObject.SetKitchenObjectParent(null);
            kitchenObject.transform.position = dropPosition;
            NetworkIsHoldingObject = false;
        }

        #endregion

        // -------------------------------------------------------
        #region Cutting

        private void StartCutting(CuttingCounter counter)
        {
            if (_isCutting && _currentCuttingCounter == counter) return; // Tránh start lại nếu đang cắt cùng counter

            // Dừng counter cũ nếu có
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