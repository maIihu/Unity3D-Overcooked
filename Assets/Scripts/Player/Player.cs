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
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private float playerRadius = 0.7f;
    
    [SerializeField] private Transform _handPoint;
    [SerializeField] private Transform interactPoint;
    
    [SerializeField] private Animator animator;
    
    private Vector2 _moveInput;
    private Vector3 _lastInteractDir;
    private BaseCounter _selectedCounter;
    private KitchenObject _kitchenObject;

    private CuttingCounter _currentCuttingCounter;
    private bool _isCutting;
    private Coroutine _cutCoroutine;
    
    private void Awake()
    {
    }

    private void Update()
    {
        InputHandler();
        UpdateAnimation();
        Move();
        HandleInteractions();
        if (_moveInput != Vector2.zero && _isCutting)
        {
            StopCutting();
        }
    }

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
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(this.transform.position, this.transform.position + Vector3.up * playerHeight);
        
        Vector3 point1 = transform.position;
        Vector3 point2 = transform.position + Vector3.up * playerHeight;

        Gizmos.color = Color.blue;

        DrawCapsule(point1, point2, playerRadius);

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
        float moveDistance = moveSpeed  * Time.deltaTime;
        
        Vector3 pos = transform.position;
        Vector3 upOffset = Vector3.up * playerHeight;
        
        // tach truc X
        Vector3 moveDirX = new Vector3(moveDir.x, 0, 0);
        bool canMoveX = !Physics.CapsuleCast(pos, pos + upOffset, 
            playerRadius, moveDirX, moveDistance);
        
        // tach truc Z
        Vector3 moveDirZ = new Vector3(0, 0, moveDir.z);
        bool canMoveZ =  !Physics.CapsuleCast(pos, pos + upOffset, 
            playerRadius, moveDirZ, moveDistance);
        
        if(canMoveX)
            transform.position += moveDirX * moveDistance;
        if (canMoveZ)
            transform.position += moveDirZ * moveDistance;
        
        Vector3 finalMove = new Vector3(canMoveX ? moveDirX.x : 0,
            0, canMoveZ ? moveDirZ.z : 0);
        
        if (finalMove != Vector3.zero)
        {
            transform.forward = Vector3.Slerp(transform.forward, finalMove.normalized, Time.deltaTime * rotateSpeed);
        }
       
    }


    #endregion

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
                    if((!HasKitchenObject() && _selectedCounter is ContainerCounter) ||
                       (HasKitchenObject() && _selectedCounter is ClearCounter)) 
                        animator.SetTrigger("IsPicked");
                    
                    baseCounter.Interact(this);
                    animator.SetBool("HasObject", this.HasKitchenObject());
                }
                
                if (_selectedCounter == baseCounter && Input.GetKeyDown(KeyCode.R) && _moveInput == Vector2.zero)
                {
                    if (baseCounter is CuttingCounter cuttingCounter && cuttingCounter.HasKitchenObject())
                        StartCutting(cuttingCounter);
                }

                
                //if (Input.GetKey(KeyCode.R)) baseCounter.InteractAlternate(this);
            }
            else SetSelectedCounter(null);
        }
        else SetSelectedCounter(null);
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
        _isCutting = true;
        _currentCuttingCounter = counter;
        _currentCuttingCounter.CuttingSoundAndAnimation();
        _cutCoroutine = StartCoroutine(CutRoutine());
    }

    private void StopCutting()
    {
        _isCutting = false;
        _currentCuttingCounter.StopAnimationCut();
        _currentCuttingCounter = null;
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
        this._kitchenObject =  kitchenObject;
    }

    public KitchenObject GetKitchenObject()
    {
        return  this._kitchenObject;
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