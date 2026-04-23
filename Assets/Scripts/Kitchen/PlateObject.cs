using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kitchen
{
    public class PlateObject : KitchenObject, IKitchenObjectParent
    {
        [SerializeField] private Transform topPoint;
        private KitchenObject _kitchenObject;

        public override void OnDespawn()
        {
            base.OnDespawn();
            if (HasKitchenObject())
            {
                _kitchenObject.DestroySelf();
            }
        }

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
