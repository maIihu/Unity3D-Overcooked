
using System;
using UnityEngine;

public class PlatesCounter : BaseCounter, IKitchenObjectParent
{
    [SerializeField] private KitchenObjectSO plateKitchenObjectSO;

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
            if(!player.HasKitchenObject())
                _kitchenObject.SetKitchenObjectParent(player);
            else
            {
                if(GetKitchenObject() is PlateKitchenObject plateKitchenObject1)
                {
                    if(plateKitchenObject1.TryAddIngredient(player.GetKitchenObject().GetDataObjectSo))
                    {
                        player.GetKitchenObject().DestroySelf();
                        SoundManagerScript.PlaySound(SoundManagerScript.GetAudioClipRefesSO().objectDrop, this.transform.position);
                    }
                }
            }
        }
        else 
        {
            if(player.HasKitchenObject())
            {
                player.GetKitchenObject().SetKitchenObjectParent(this);
                SoundManagerScript.PlaySound(SoundManagerScript.GetAudioClipRefesSO().objectDrop, this.transform.position);
            }
        }
    }

    private void SpawnPlate()
    {
        //KitchenObject.SpawnKitchenObject(plateKitchenObjectSO, this);
        var go = Instantiate(plateKitchenObjectSO.prefab);
        go.Init(plateKitchenObjectSO);
        go.SetKitchenObjectParent(this);
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
