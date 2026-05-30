using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kitchen
{
    public interface IKitchenObjectParent
    {
        public Transform GetKitchenObjectToTransform();
        public void SetKitchenObject(KitchenObject kitchenObject);
        public KitchenObject GetKitchenObject();
        public void ClearKitchenObject();
        public bool HasKitchenObject();
        public Fusion.NetworkObject GetNetworkObject();
    }

}
