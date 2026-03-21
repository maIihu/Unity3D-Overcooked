using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IKitchenObjectParent
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private Transform _handPoint;
    
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
    }
    private void Move()
    {
        float playerHeight = 2f;
        float playerRadius = 0.7f;
        
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
            transform.forward = Vector3.Slerp(transform.forward, finalMove.normalized,
                Time.deltaTime * rotateSpeed);
        }
       
    }


    #endregion

    #region Interactions

    private void HandleInteractions()
    {
        Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);
        if (moveDir != Vector3.zero)
            _lastInteractDir = moveDir;

        float interactDistance = 1f;
        float sphereRadius = 0.5f;

        if (Physics.SphereCast(transform.position, sphereRadius, 
                _lastInteractDir, out RaycastHit hit, interactDistance))
        {
            if (hit.collider.TryGetComponent(out BaseCounter baseCounter))
            {
                if (_selectedCounter != baseCounter)
                {
                    SetSelectedCounter(baseCounter);
                }
                
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    baseCounter.Interact(this);
                }
                
                if (_selectedCounter == baseCounter &&
                    Input.GetKeyDown(KeyCode.R) && _moveInput == Vector2.zero)
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