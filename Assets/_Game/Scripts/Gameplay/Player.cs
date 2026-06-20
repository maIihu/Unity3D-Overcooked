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

        private static readonly int s_MovingValue = Animator.StringToHash("MovingValue");
        private static readonly int s_IsChopping  = Animator.StringToHash("IsChopping");
        private static readonly int s_HasObject   = Animator.StringToHash("HasObject");
        private static readonly int s_IsPicked    = Animator.StringToHash("IsPicked");

        private Vector2    _moveInput;
        private Vector3    _lastInteractDir;
        private BaseCounter _selectedCounter;
        private KitchenObject _kitchenObject;

        [Networked] private Quaternion _currentRotation { get; set; }

        private Material _bodyMaterial;

        private CuttingCounter _currentCuttingCounter;
        private bool           _isCutting;

        private float _footstepTimer = 0f;
        private const float FOOTSTEP_INTERVAL = 0.35f;

        #region Networked Properties

        [Networked] public Vector2 NetworkMoveInput { get; set; }

        [Networked] public Vector3 NetworkTargetForward { get; set; }

        [Networked]
        [OnChangedRender(nameof(OnColorChanged))]
        public EPlayerColor PlayerColor { get; set; }

        [Networked] public NetworkBool NetworkIsChopping      { get; set; }
        [Networked] public NetworkBool NetworkIsHoldingObject { get; set; }

        [Networked]
        [OnChangedRender(nameof(OnPickedChanged))]
        public NetworkBool NetworkIsPicked { get; set; }

        #endregion

        #region Color System

        private static readonly Color[] s_PlayerColors = new Color[]
        {
            new Color(204f/255f, 50f/255f, 22f/255f),
            new Color(38f/255f, 147f/255f, 204f/255f),
            new Color(34f/255f, 196f/255f, 66f/255f),
            new Color(215f/255f, 209f/255f, 49f/255f),
            new Color(0.5f, 0f, 0.5f), 
            new Color(1f, 0.5f, 0f),   
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
        }

        private void OnColorChanged()
        {
            UpdateVisualColor(PlayerColor);
        }

        private void OnPickedChanged()
        {
            animator.SetTrigger(s_IsPicked);
        }

        #endregion

        // -------------------------------------------------------
        #region Fusion Lifecycle

        public override void Spawned()
        {
            Runner.SetIsSimulated(Object, true);

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

            _currentRotation = transform.rotation;

            NetworkTargetForward = transform.forward;
            _lastInteractDir     = transform.forward;

            if (bodyRend != null && bodyRend.materials.Length > 1)
                _bodyMaterial = bodyRend.materials[1];

            UpdateVisualColor(PlayerColor);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            StopCutting();
        }

        public override void FixedUpdateNetwork()
        {
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
                _moveInput = Vector2.ClampMagnitude(
                    new Vector2(inputData.MoveX, inputData.MoveY), 1f
                );

                if (HasStateAuthority)
                    NetworkMoveInput = _moveInput;

                HandleInteractions(inputData);

                if (_moveInput != Vector2.zero && _isCutting)
                    StopCutting();
            }

            Move();

            if (_isCutting && _currentCuttingCounter != null)
                _currentCuttingCounter.InteractAlternate(this);
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
            if (_rb == null) return;

            if (HasInputAuthority && _rb.isKinematic)
            {
                _rb.isKinematic = false;
            }

            Vector3 moveDir = new Vector3(_moveInput.x, 0f, _moveInput.y);

            if (moveDir != Vector3.zero)
            {
                if (_rb.IsSleeping()) _rb.WakeUp(); 

                _rb.velocity = new Vector3(
                    moveDir.x * moveSpeed,
                    _rb.velocity.y,
                    moveDir.z * moveSpeed
                );

                _lastInteractDir = moveDir.normalized;

                if (HasStateAuthority)
                    NetworkTargetForward = _lastInteractDir;

                Quaternion targetRot = Quaternion.LookRotation(_lastInteractDir);

                _currentRotation = Quaternion.Slerp(
                    _currentRotation,
                    targetRot,
                    Runner.DeltaTime * rotateSpeed
                );
                _rb.MoveRotation(_currentRotation);

                if (HasInputAuthority || (Runner.IsServer && !Object.HasInputAuthority))
                {
                    _footstepTimer += Runner.DeltaTime;
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
            }
            else
            {
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                _footstepTimer = 0f;
            }
        }

        private void UpdateAnimation()
        {
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