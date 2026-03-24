using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttingCounter : BaseCounter, IKitchenObjectParent, IHasProgress
{
    [SerializeField] private float cuttingTime;
    public event EventHandler<IHasProgress.OnProgressBarChangedEventArgs> OnProgressBarChanged;

    public event Action OnCutComplete;
    
    private FoodObject _kitchenObject;
    private float _cuttingProgress;
    //[SerializeField] private Animator _ani;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Interact(Player player)
    { 
        base.Interact(player);
        if (HasKitchenObject())
        {

        }
        else
        {        
            player.GetKitchenObject().SetKitchenObjectParent(this);
        }
    }
    
    public override void InteractAlternate(Player player)
    {
        base.InteractAlternate(player);
        if (HasKitchenObject())
        {
            _cuttingProgress += Time.deltaTime;
            if (_cuttingProgress >= cuttingTime)
            {
                _cuttingProgress = 0f;
                _kitchenObject.Cut();
                OnCutComplete?.Invoke();
            }
        }
        
    }

    public void CuttingSoundAndAnimation()
    {
        //_ani.SetBool(ContainString.Cut, true);
    }
    
    public void StopAnimationCut()
    {
        //_ani.SetBool(ContainString.Cut, false);
    }
    

    #region IKitchenObjectParent

    public Transform GetKitchenObjectToTransform()
    {
        return CounterTopPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this._kitchenObject = kitchenObject as FoodObject;
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
