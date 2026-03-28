using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    protected IKitchenObjectParent KitchenObjectParent;

    public void Init()
    {
    }

    public virtual void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent)
    {
        if (kitchenObjectParent.HasKitchenObject())
        {
            //Debug.Log("KitchenObject has been set");
            return;
        }
        if (this.KitchenObjectParent != null) KitchenObjectParent.ClearKitchenObject();
        
        this.KitchenObjectParent = kitchenObjectParent;
        kitchenObjectParent.SetKitchenObject(this);
        transform.parent = kitchenObjectParent.GetKitchenObjectToTransform();
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public IKitchenObjectParent GetKitchenObjectParent()
    {
        return this.KitchenObjectParent;
    }

    public void DestroySelf()
    {
        KitchenObjectParent.ClearKitchenObject();
        Destroy(this.gameObject);
    }
    
    
}