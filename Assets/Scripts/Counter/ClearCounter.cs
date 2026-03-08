using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearCounter : BaseCounter, IKitchenObjectParent
{
    private KitchenObject _kitchenObject;
    public override void Interact(Player player)
    { 
        base.Interact(player);
        
        if (HasKitchenObject())
        { // player carrying kitchen obj
            if(!player.HasKitchenObject())
                _kitchenObject.SetKitchenObjectParent(player);
            else
            {
                if (player.GetKitchenObject() is PlateKitchenObject plateKitchenObject)
                {
                    if(plateKitchenObject.TryAddIngredient(GetKitchenObject().GetDataObjectSo))
                    {
                        GetKitchenObject().DestroySelf();
                    }
                }
                else if(GetKitchenObject() is PlateKitchenObject plateKitchenObject1)
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
