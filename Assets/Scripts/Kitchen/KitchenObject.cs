using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    private KitchenObjectSO data;
    
    protected IKitchenObjectParent KitchenObjectParent;

    public void Init(KitchenObjectSO kitchenObjectSO)
    {
        data = kitchenObjectSO;
    }

    public void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent)
    {
        if (kitchenObjectParent.HasKitchenObject())
        {
            Debug.Log("KitchenObject has been set");
            return;
        }
        if (this.KitchenObjectParent != null) KitchenObjectParent.ClearKitchenObject();
        
        this.KitchenObjectParent = kitchenObjectParent;
        kitchenObjectParent.SetKitchenObject(this);
        transform.parent = kitchenObjectParent.GetKitchenObjectToTransform();
        transform.localPosition = Vector3.zero;
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
    
    public KitchenObjectSO GetDataObjectSo => data;
    
}