using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IKitchenObjectParent
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float interactDistance = 1f;
    [SerializeField] private float sphereRadius = 0.5f;

    [SerializeField] private Transform _handPoint;
    [SerializeField] private Transform interactPoint;

    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody _rb;

    private Vector2 _moveInput;
    private Vector3 _lastInteractDir;
    private BaseCounter _selectedCounter;
    private KitchenObject _kitchenObject;

    private CuttingCounter _currentCuttingCounter;
    private bool _isCutting;
    private Coroutine _cutCoroutine;

    private void Update()
    {
        InputHandler();
        UpdateAnimation();
        HandleInteractions();
        if (_moveInput != Vector2.zero && _isCutting)
        {
            StopCutting();
        }
    }

    private void FixedUpdate()
    {
        Move();
    }

    #region Move

    private void InputHandler()
    {
        _moveInput = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) _moveInput.y += 1;
        if (Input.GetKey(KeyCode.A)) _moveInput.x -= 1;
        if (Input.GetKey(KeyCode.S)) _moveInput.y -= 1;
        if (Input.GetKey(KeyCode.D)) _moveInput.x += 1;
        _moveInput.Normalize();
    }

    private void UpdateAnimation()
    {
        animator.SetFloat("MovingValue", _moveInput.magnitude);
    }

    private void Move()
    {
        Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);

        // Áp dụng vận tốc cho Rigidbody
        Vector3 targetVelocity = moveDir * moveSpeed;
        if (_rb != null)
        {
            targetVelocity.y = _rb.velocity.y; // Giữ nguyên trọng lực
            _rb.velocity = targetVelocity;
        }

        if (moveDir != Vector3.zero)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDir.normalized, Time.fixedDeltaTime * rotateSpeed);
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
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

    private void DrawCapsule(Vector3 point1, Vector3 point2, float radius)
    {
        Gizmos.DrawWireSphere(point1, radius);
        Gizmos.DrawWireSphere(point2, radius);

        Vector3 up = (point2 - point1).normalized;
        Vector3 right = Vector3.Cross(up, Vector3.forward).normalized * radius;
        Vector3 forward = Vector3.Cross(up, right).normalized * radius;

        Gizmos.DrawLine(point1 + right, point2 + right);
        Gizmos.DrawLine(point1 - right, point2 - right);
        Gizmos.DrawLine(point1 + forward, point2 + forward);
        Gizmos.DrawLine(point1 - forward, point2 - forward);
    }

    #region Interactions

    private void HandleInteractions()
    {
        Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);
        if (moveDir != Vector3.zero)
            _lastInteractDir = moveDir;

        if (Physics.SphereCast(interactPoint.position, sphereRadius, _lastInteractDir, out RaycastHit hit, interactDistance))
        {
            if (hit.collider.TryGetComponent(out BaseCounter baseCounter))
            {
                if (_selectedCounter != baseCounter)
                {
                    SetSelectedCounter(baseCounter);
                }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if ((!HasKitchenObject() && _selectedCounter is ContainerCounter) ||
                       (HasKitchenObject() && _selectedCounter is ClearCounter))
                        animator.SetTrigger("IsPicked");

                    baseCounter.Interact(this);
                    animator.SetBool("HasObject", this.HasKitchenObject());
                }

                if (_selectedCounter == baseCounter && Input.GetKeyDown(KeyCode.R) && _moveInput == Vector2.zero)
                {
                    if (baseCounter is CuttingCounter cuttingCounter && cuttingCounter.HasKitchenObject())
                    {
                        if (cuttingCounter.GetKitchenObject() is FoodObject { FoodState: FoodState.Normal })
                            StartCutting(cuttingCounter);
                    }
                }
                return;
            }

            // Kiểm tra vật phẩm dưới đất
            if (hit.collider.TryGetComponent(out KitchenObject kitchenObject))
            {
                // Chỉ xử lý nếu không phải là vật phẩm đang cầm trên tay
                if (kitchenObject != GetKitchenObject())
                {
                    SetSelectedCounter(null);
                    if (Input.GetKeyDown(KeyCode.Space) && !HasKitchenObject() && kitchenObject.GetKitchenObjectParent() == null)
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

        // Logic thả đồ xuống đất khi không nhìn vào bàn
        if (Input.GetKeyDown(KeyCode.Space) && HasKitchenObject())
        {
            DropKitchenObject();
        }
    }

    private void SetSelectedCounter(BaseCounter baseCounter)
    {
        if (_selectedCounter != null)
        {
            _selectedCounter.Hide();
        }

        _selectedCounter = baseCounter;

        if (_selectedCounter != null)
        {
            _selectedCounter.Show();
        }
    }

    private void DropKitchenObject()
    {
        KitchenObject kitchenObject = GetKitchenObject();
        if (kitchenObject == null) return;

        // Thả vật phẩm từ vị trí ngang tay (chest height) và để nó tự rơi theo trọng lực
        Vector3 dropPosition = transform.position + transform.forward * 1f + Vector3.up * 0.5f;

        kitchenObject.SetKitchenObjectParent(null);
        kitchenObject.transform.position = dropPosition;

        animator.SetBool("HasObject", false);
    }

    #endregion

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

    #region IKitchenObjectParent

    public Transform GetKitchenObjectToTransform()
    {
        return this._handPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this._kitchenObject = kitchenObject;
    }

    public KitchenObject GetKitchenObject()
    {
        return this._kitchenObject;
    }

    public void ClearKitchenObject()
    {
        this._kitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return this._kitchenObject != null;
    }

    #endregion

}