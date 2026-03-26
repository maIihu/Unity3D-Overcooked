using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounter : BaseCounter, IKitchenObjectParent, IHasProgress
{
    private enum State
    {
        Idle, Frying, Fried, Burned
    }
    
    public event EventHandler<IHasProgress.OnProgressBarChangedEventArgs> OnProgressBarChanged;

    [SerializeField] private PotObject potObject;
    [SerializeField] private Transform potPoint;

    private KitchenObject _kitchenObject;
    
    private State _state;
    private float _fryingTimer;
    private float _burningTimer;
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        _state = State.Idle;
        HideEffect();
        var go = Instantiate(potObject, potPoint.position, Quaternion.identity);
        go.SetKitchenObjectParent(this);
    }

    private void Update()
    {
        if (HasKitchenObject())
        {
          
        }
    }

    private void ShowEffect()
    {

    }

    private void HideEffect()
    {

    }

    public override void Interact(Player player)
    {
        base.Interact(player);
        if (HasKitchenObject())
        {
            if(!player.HasKitchenObject())
            {
                GetKitchenObject().SetKitchenObjectParent(player);
                _state = State.Idle;
                OnProgressBarChanged?.Invoke(this, new IHasProgress.OnProgressBarChangedEventArgs()
                {
                    progressNormalized = 0
                });
                HideEffect();
            }
            else
            {
                
            }
        }
        else
        {
            if (player.HasKitchenObject() && player.GetKitchenObject() is PotObject)
            {
                player.GetKitchenObject().SetKitchenObjectParent(this);
            }
        }
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
