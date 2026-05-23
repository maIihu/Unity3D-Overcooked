using System.Collections;
using Counter;
using Kitchen;
using UnityEngine;
using Fusion;

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

        // Local-only (chỉ dùng trên client sở hữu)
        private Vector2 _moveInput;
        private Vector3 _lastInteractDir;
        private BaseCounter _selectedCounter;
        private KitchenObject _kitchenObject;

        private CuttingCounter _currentCuttingCounter;
        private bool _isCutting;
        private Coroutine _cutCoroutine;

        // --- Networked properties ---
        [Networked] public Vector2 NetworkMoveInput { get; set; }
        [Networked] public NetworkBool IsReady { get; set; }  // Fix: dùng NetworkBool

        // -------------------------------------------------------
        #region Ready System

        public void ToggleReady()
        {
            // HasInputAuthority: đúng owner của object này mới gọi
            if (!HasInputAuthority) return;
            RPC_SetReady(!IsReady);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetReady(NetworkBool ready)
        {
            IsReady = ready;
            Debug.Log($"[Player] Player {Object.InputAuthority.PlayerId} IsReady = {ready}");
        }

        #endregion

        // -------------------------------------------------------
        #region Fusion Lifecycle

// Thêm field
        private Vector3 _targetForward;
        [Networked] public Vector3 NetworkTargetForward { get; set; } // Sync rotation cho remote

        public override void Spawned()
        {
            if (_rb != null)
                _rb.isKinematic = !(HasStateAuthority || HasInputAuthority);

            _targetForward = transform.forward;
        }

        public override void FixedUpdateNetwork()
        {
            // Cho phép cả StateAuthority (Host) và InputAuthority (Client điều khiển) chạy logic di chuyển để hỗ trợ Client-Side Prediction
            if (!HasStateAuthority && !HasInputAuthority) return;

            if (GetInput(out NetworkInputData inputData))
            {
                _moveInput = new Vector2(inputData.MoveX, inputData.MoveY);
                
                if (HasStateAuthority)
                {
                    NetworkMoveInput = _moveInput;
                }

                Move();
                
                // Chỉ StateAuthority mới xử lý tương tác vật lý và logic game quan trọng để tránh cheat/desync
                if (HasStateAuthority)
                {
                    HandleInteractions(inputData);

                    if (_moveInput != Vector2.zero && _isCutting)
                        StopCutting();
                }
            }
        }
        
        private void Update()
        {
            if (Object == null || !Object.IsValid) return;

            // Đã loại bỏ logic xoay thủ công Slerp tại đây để tránh tranh chấp với nội suy (Interpolation)
            // của component NetworkTransform trên Remote Clients. NetworkTransform sẽ tự động đồng bộ
            // và nội suy vị trí/rotation của Player cực kỳ mượt mà.

            UpdateAnimation();
        }

        #endregion

        // -------------------------------------------------------
        #region Move
        
        private void Move()
        {
            Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);

            if (_rb != null)
            {
                Vector3 targetVelocity = moveDir * moveSpeed;
                targetVelocity.y = _rb.velocity.y;
                _rb.velocity = targetVelocity;
            }

            if (moveDir != Vector3.zero)
            {
                _targetForward = moveDir.normalized;
                
                if (HasStateAuthority)
                {
                    NetworkTargetForward = _targetForward;
                }

                transform.rotation = Quaternion.LookRotation(_targetForward);
            }
        }

        // private void Move()
        // {
        //     Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);
        //
        //     if (_rb != null)
        //     {
        //         Vector3 targetVelocity = moveDir * moveSpeed;
        //         targetVelocity.y = _rb.velocity.y; // Giữ trọng lực
        //         _rb.velocity = targetVelocity;
        //     }
        //
        //     if (moveDir != Vector3.zero)
        //     {
        //         // Dùng Runner.DeltaTime trong FixedUpdateNetwork
        //         transform.forward = Vector3.Slerp(
        //             transform.forward,
        //             moveDir.normalized,
        //             Runner.DeltaTime * rotateSpeed
        //         );
        //     }
        // }

        private void UpdateAnimation()
        {
            // Remote clients dùng NetworkMoveInput để animate
            Vector2 inputToUse = HasStateAuthority ? _moveInput : NetworkMoveInput;
            animator.SetFloat("MovingValue", inputToUse.magnitude);
        }

        #endregion

        // -------------------------------------------------------
        #region Interactions

        private void HandleInteractions(NetworkInputData inputData)
        {
            Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);
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

                    // Interact (Space)
                    if (inputData.IsInteractPressed)
                    {
                        if ((!HasKitchenObject() && _selectedCounter is ContainerCounter) ||
                            (HasKitchenObject() && _selectedCounter is ClearCounter))
                            animator.SetTrigger("IsPicked");

                        baseCounter.Interact(this);
                        animator.SetBool("HasObject", HasKitchenObject());
                    }

                    // Alternate interact / Cut (R)
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

                // Nhặt vật phẩm dưới đất
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
                            animator.SetBool("HasObject", true);
                            animator.SetTrigger("IsPicked");
                        }
                        return;
                    }
                }
            }

            SetSelectedCounter(null);

            // Thả đồ khi không nhìn vào bàn
            if (inputData.IsInteractPressed && HasKitchenObject())
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
            animator.SetBool("HasObject", false);
        }

        #endregion

        // -------------------------------------------------------
        #region Cutting

        private IEnumerator CutRoutine()
        {
            while (_isCutting && _currentCuttingCounter != null)
            {
                if (_moveInput != Vector2.zero)
                {
                    StopCutting();
                    yield break;
                }
                _currentCuttingCounter.InteractAlternate(this);
                yield return null;
            }
        }

        private void StartCutting(CuttingCounter counter)
        {
            animator.SetBool("IsChopping", true);
            _isCutting = true;
            _currentCuttingCounter = counter;
            _currentCuttingCounter.CuttingSoundAndAnimation();
            _cutCoroutine = StartCoroutine(CutRoutine());
            _currentCuttingCounter.OnCutComplete += StopCutting;
        }

        private void StopCutting()
        {
            animator.SetBool("IsChopping", false);
            _isCutting = false;
            if (_currentCuttingCounter != null)
            {
                _currentCuttingCounter.OnCutComplete -= StopCutting;
                _currentCuttingCounter.StopAnimationCut();
                _currentCuttingCounter = null;
            }
            if (_cutCoroutine != null)
            {
                StopCoroutine(_cutCoroutine);
                _cutCoroutine = null;
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