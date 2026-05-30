using Counter;
using Kitchen;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCore;

namespace _Game.Scripts.Gameplay
{
    /// <summary>
    /// Player dành riêng cho Single Mode — thuần MonoBehaviour, ZERO Fusion overhead.
    /// Đọc input trực tiếp từ Old Input System, di chuyển qua Rigidbody.velocity,
    /// xử lý tất cả gameplay logic cục bộ mà không cần Runner hay NetworkObject.
    /// </summary>
    public class PlayerLocal : MonoBehaviour, IPlayer
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

        // ─── Local State ────────────────────────────────────────
        private Vector2 _moveInput;
        private Vector3 _lastInteractDir;
        private BaseCounter _selectedCounter;
        private KitchenObject _kitchenObject;

        // Rotation: giữ canonical value để tránh đọc transform.rotation bị
        // internal interpolation gây feedback loop giật
        private Quaternion _currentRotation;

        // Input buttons (tích lũy ở Update, tiêu thụ ở FixedUpdate)
        private bool _interactPressed;
        private bool _alternatePressed;

        // Cutting state
        private CuttingCounter _currentCuttingCounter;
        private bool _isCutting;

        // ─── Unity Lifecycle ─────────────────────────────────────

        private void Start()
        {
            // Set default green color for Single Mode
            if (bodyRend != null)
            {
                Material[] mats = bodyRend.materials;
                if (mats.Length > 1 && mats[1] != null)
                {
                    mats[1].color = Color.green;
                    bodyRend.materials = mats;
                }
            }

            _currentRotation = transform.rotation;
            _lastInteractDir = transform.forward;

            // Snap xuống sàn ngay khi spawn để tránh rơi vô tận
            SnapToGround();
        }

        private void Update()
        {
            // Đọc movement input (normalized để tránh diagonal speed boost)
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            float mag = Mathf.Sqrt(x * x + z * z);
            if (mag > 0f) { x /= mag; z /= mag; }
            _moveInput = new Vector2(x, z);

            // Tích lũy one-frame button presses (GetKeyDown chỉ dùng được trong Update)
            if (Input.GetKeyDown(KeyCode.Space)) _interactPressed = true;
            if (Input.GetKeyDown(KeyCode.R))     _alternatePressed = true;

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            Move();
            HandleInteractions();

            // Cutting tick
            if (_isCutting && _currentCuttingCounter != null)
                _currentCuttingCounter.InteractAlternate(this);

            // Dừng cắt nếu di chuyển
            if (_moveInput != Vector2.zero && _isCutting)
                StopCutting();

            // Reset one-frame buttons sau khi đã xử lý
            _interactPressed  = false;
            _alternatePressed = false;
        }

        // ─── Movement ────────────────────────────────────────────

        private void Move()
        {
            if (_rb == null) return;

            Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (moveDir != Vector3.zero)
            {
                // Set velocity ngang, giữ velocity.y để gravity hoạt động
                _rb.velocity = new Vector3(
                    moveDir.x * moveSpeed,
                    _rb.velocity.y,
                    moveDir.z * moveSpeed
                );

                _lastInteractDir = moveDir.normalized;

                // Xoay mượt — dùng _currentRotation (canonical) làm source,
                // KHÔNG dùng transform.rotation để tránh feedback loop với Rigidbody.Interpolate
                Quaternion targetRot = Quaternion.LookRotation(_lastInteractDir);
                _currentRotation = Quaternion.Slerp(
                    _currentRotation,
                    targetRot,
                    Time.fixedDeltaTime * rotateSpeed
                );
                transform.rotation = _currentRotation;
            }
            else
            {
                // Đứng yên: triệt tiêu velocity ngang, giữ velocity.y
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
            }
        }

        private void SnapToGround()
        {
            if (_rb == null) return;
            int groundMask = ~(1 << gameObject.layer);
            if (Physics.Raycast(_rb.position + Vector3.up * 5f, Vector3.down,
                    out RaycastHit hit, 10f, groundMask))
            {
                _rb.velocity = Vector3.zero;
                _rb.position = new Vector3(_rb.position.x, hit.point.y, _rb.position.z);
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;
            animator.SetFloat("MovingValue", _moveInput.magnitude);
            animator.SetBool("IsChopping", _isCutting);
            animator.SetBool("HasObject", HasKitchenObject());
        }

        // ─── Interactions ────────────────────────────────────────

        private void HandleInteractions()
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

                    // Single Mode: không cần HasStateAuthority — local player luôn có authority
                    if (_interactPressed)
                    {
                        if ((!HasKitchenObject() && _selectedCounter is ContainerCounter) ||
                            (HasKitchenObject() && _selectedCounter is ClearCounter))
                        {
                            animator?.SetTrigger("IsPicked");
                        }

                        baseCounter.Interact(this);
                    }

                    if (_alternatePressed && _moveInput == Vector2.zero)
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
                        if (_interactPressed && !HasKitchenObject() &&
                            kitchenObject.GetKitchenObjectParent() == null)
                        {
                            kitchenObject.SetKitchenObjectParent(this);
                            animator?.SetTrigger("IsPicked");
                        }
                        return;
                    }
                }
            }

            SetSelectedCounter(null);

            if (_interactPressed && HasKitchenObject())
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
        }

        // ─── Cutting ─────────────────────────────────────────────

        private void StartCutting(CuttingCounter counter)
        {
            if (_isCutting && _currentCuttingCounter == counter) return;
            if (_isCutting) StopCutting();

            _isCutting = true;
            _currentCuttingCounter = counter;
            _currentCuttingCounter.OnCutComplete += StopCutting;
            _currentCuttingCounter.CuttingSoundAndAnimation();
        }

        private void StopCutting()
        {
            _isCutting = false;
            if (_currentCuttingCounter != null)
            {
                _currentCuttingCounter.OnCutComplete -= StopCutting;
                _currentCuttingCounter.StopAnimationCut();
                _currentCuttingCounter = null;
            }
        }

        // ─── IKitchenObjectParent ─────────────────────────────────

        public Transform GetKitchenObjectToTransform() => _handPoint;
        public void SetKitchenObject(KitchenObject kitchenObject) => _kitchenObject = kitchenObject;
        public KitchenObject GetKitchenObject() => _kitchenObject;
        public void ClearKitchenObject() => _kitchenObject = null;
        public bool HasKitchenObject() => _kitchenObject != null;

        // ─── Gizmos ───────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (interactPoint == null) return;
            Vector3 origin = interactPoint.position;
            Vector3 dir = _lastInteractDir == Vector3.zero ? transform.forward : _lastInteractDir.normalized;
            Vector3 endPoint = origin + dir * interactDistance;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, sphereRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(endPoint, sphereRadius);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(origin, endPoint);
        }
    }
}
