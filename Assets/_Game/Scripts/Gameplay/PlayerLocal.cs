using Counter;
using Kitchen;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCore;

namespace _Game.Scripts.Gameplay
{

    public class PlayerLocal : MonoBehaviour, IPlayer
    {
        private static readonly int s_MovingValue = Animator.StringToHash("MovingValue");
        private static readonly int s_IsChopping  = Animator.StringToHash("IsChopping");
        private static readonly int s_HasObject   = Animator.StringToHash("HasObject");
        private static readonly int s_IsPicked    = Animator.StringToHash("IsPicked");
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

        private Vector2 _moveInput;
        private Vector3 _lastInteractDir;
        private BaseCounter _selectedCounter;
        private KitchenObject _kitchenObject;


        private Quaternion _currentRotation;

        private bool _interactPressed;
        private bool _alternatePressed;

        private CuttingCounter _currentCuttingCounter;
        private bool _isCutting;

        private float _footstepTimer = 0f;
        private const float FOOTSTEP_INTERVAL = 0.35f;

        private void Start()
        {
            if (bodyRend != null)
            {
                Material[] mats = bodyRend.materials;
                if (mats.Length > 1 && mats[1] != null)
                {
                    mats[1].color = new Color(34f/255f, 196f/255f, 66f/255f);
                    bodyRend.materials = mats;
                }
            }

            _currentRotation = transform.rotation;
            _lastInteractDir = transform.forward;

            SnapToGround();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != EGameState.Play)
            {
                _moveInput = Vector2.zero;
                _interactPressed = false;
                _alternatePressed = false;
                UpdateAnimation();
                return;
            }

            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            float mag = Mathf.Sqrt(x * x + z * z);
            if (mag > 0f) { x /= mag; z /= mag; }
            _moveInput = new Vector2(x, z);

            if (Input.GetKeyDown(KeyCode.Space)) _interactPressed = true;
            if (Input.GetKeyDown(KeyCode.R))     _alternatePressed = true;

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != EGameState.Play)
            {
                _moveInput = Vector2.zero;
                if (_rb != null)
                {
                    _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                }
                if (_isCutting)
                    StopCutting();
                _interactPressed = false;
                _alternatePressed = false;
                return;
            }

            Move();
            HandleInteractions();

            if (_isCutting && _currentCuttingCounter != null)
                _currentCuttingCounter.InteractAlternate(this);

            if (_moveInput != Vector2.zero && _isCutting)
                StopCutting();

            _interactPressed  = false;
            _alternatePressed = false;
        }


        private void Move()
        {
            if (_rb == null) return;

            Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (moveDir != Vector3.zero)
            {
                _rb.velocity = new Vector3(
                    moveDir.x * moveSpeed,
                    _rb.velocity.y,
                    moveDir.z * moveSpeed
                );

                _lastInteractDir = moveDir.normalized;

                Quaternion targetRot = Quaternion.LookRotation(_lastInteractDir);
                _currentRotation = Quaternion.Slerp(
                    _currentRotation,
                    targetRot,
                    Time.fixedDeltaTime * rotateSpeed
                );
                transform.rotation = _currentRotation;

                _footstepTimer += Time.fixedDeltaTime;
                if (_footstepTimer >= FOOTSTEP_INTERVAL)
                {
                    _footstepTimer = 0f;
                    _Game.Scripts.DesignPattern.Observer.MessageManager.Instance.SendMessage(
                        new _Game.Scripts.DesignPattern.Observer.Message(
                            _Game.Scripts.DesignPattern.Observer.ProjectMessageType.OnFootstep,
                            new object[] { transform.position }
                        )
                    );
                }
            }
            else
            {
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                _footstepTimer = 0f;
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
            animator.SetFloat(s_MovingValue, _moveInput.magnitude);
            animator.SetBool(s_IsChopping, _isCutting);
            animator.SetBool(s_HasObject, HasKitchenObject());
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

                    if (_interactPressed)
                    {
                        if ((!HasKitchenObject() && _selectedCounter is ContainerCounter) ||
                            (HasKitchenObject() && _selectedCounter is ClearCounter))
                        {
                            animator?.SetTrigger(s_IsPicked);
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
                            animator?.SetTrigger(s_IsPicked);
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
        public Fusion.NetworkObject GetNetworkObject() => null;

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
