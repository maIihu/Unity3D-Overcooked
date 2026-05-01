using UnityEngine;

namespace Kitchen
{
    public class PanObject : KitchenObject, IKitchenObjectParent
    {
        [SerializeField] private Transform topPoint;
        
        private KitchenObject _kitchenObject;
        
        #region IKitchenObjectParent

        public Transform GetKitchenObjectToTransform()
        {
            return topPoint;
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
}