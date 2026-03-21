
using System;
using UnityEngine;

public class PlatesCounter : BaseCounter, IKitchenObjectParent
{
    private KitchenObject _kitchenObject;
    
    private float _spawnPlateTimer;
    private float _spawnPlateTimerMax = 4f;

    protected override void Start()
    {
        base.Start();
        SpawnPlate();
    }

    private void Update()
    {
        if (!HasKitchenObject())
        {
            _spawnPlateTimer += Time.deltaTime;
            if (_spawnPlateTimer > _spawnPlateTimerMax)
            {
                SpawnPlate();
                _spawnPlateTimer = 0;
            }
        }
    }

    public override void Interact(Player player)
    {
        base.Interact(player);
        if (HasKitchenObject())
        { // player carrying kitchen obj
            
        }
        else 
        {
            if(player.HasKitchenObject())
            {
                player.GetKitchenObject().SetKitchenObjectParent(this);
            }
        }
    }

    private void SpawnPlate()
    {

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
