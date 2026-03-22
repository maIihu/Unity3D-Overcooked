using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttingCounter : BaseCounter, IKitchenObjectParent, IHasProgress
{
    public event EventHandler<IHasProgress.OnProgressBarChangedEventArgs> OnProgressBarChanged;
    
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
        Debug.Log("Hello");
        if (HasKitchenObject())
        {
            Debug.Log("Hello1");

        }
        else
        {        
            Debug.Log("Hello3");
            player.GetKitchenObject().SetKitchenObjectParent(this);
            //_kitchenObject = player.GetKitchenObject() is FoodObject ? player.GetKitchenObject() as FoodObject : null;
        }
    }
    
    public override void InteractAlternate(Player player)
    {
        base.InteractAlternate(player);
        if (HasKitchenObject())
        {
            _kitchenObject.Cut();
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
