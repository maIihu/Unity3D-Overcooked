using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttingCounter : BaseCounter, IKitchenObjectParent, IHasProgress
{
    public event EventHandler<IHasProgress.OnProgressBarChangedEventArgs> OnProgressBarChanged;
    
    private KitchenObject _kitchenObject;
    private float _cuttingProgress;
    private Animator _ani;

    protected override void Awake()
    {
        base.Awake();
        _ani = GetComponentInChildren<Animator>();
    }

    public override void Interact(Player player)
    { 
        base.Interact(player);

        if (HasKitchenObject())
        {
            
        }
        else
        {
            
        }
    }
    
    public override void InteractAlternate(Player player)
    {
        base.InteractAlternate(player);
        if (HasKitchenObject())
        {
            
        }
        
    }

    public void CuttingSoundAndAnimation()
    {
        _ani.SetBool(ContainString.Cut, true);
    }
    
    public void StopAnimationCut()
    {
        _ani.SetBool(ContainString.Cut, false);
    }
    

    #region IKitchenObjectParent

    public Transform GetKitchenObjectToTransform()
    {
        return CounterTopPoint;
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
