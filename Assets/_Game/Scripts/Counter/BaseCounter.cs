using _Game.Scripts.Gameplay;
using Kitchen;
using Pooling;
using UnityEngine;

namespace Counter
{
    public class BaseCounter : MonoBehaviour, IKitchenObjectParent, IPoolable
    {
        [SerializeField] private Transform counterTopPoint;
        [SerializeField] private GameObject selectedCounter;

        protected KitchenObject _kitchenObject;
        
        #region Public Methods

        public virtual void Init()
        {
            Hide();
        }

        public virtual void OnSpawn() { }

        public virtual void OnDespawn()
        {
            if (HasKitchenObject())
            {
                _kitchenObject.DestroySelf();
            }
            Hide();
        }

        public void Show()
        {
            if (selectedCounter != null) selectedCounter.SetActive(true);
        }

        public void Hide()
        {
            if (selectedCounter != null) selectedCounter.SetActive(false);
        }

        #endregion

        #region Interaction
        public virtual void Interact(Player player) { }

        public virtual void InteractAlternate(Player player) { }
        #endregion

        #region IKitchenObjectParent Implementation

        public Transform GetKitchenObjectToTransform() => counterTopPoint;

        public virtual void SetKitchenObject(KitchenObject kitchenObject)
        {
            this._kitchenObject = kitchenObject;
        }

        public KitchenObject GetKitchenObject() => _kitchenObject;

        public virtual void ClearKitchenObject() => _kitchenObject = null;

        public bool HasKitchenObject() => _kitchenObject != null;

        #endregion

        #region Pooling Helpers
        
        protected KitchenObject SpawnKitchenObject(EFoodType type)
        {
            KitchenObject instance = PoolManager.Instance.Kitchen.Get(type);
            instance.SetKitchenObjectParent(this);
            return instance;
        }
        
        protected KitchenObject SpawnKitchenObject(KitchenType type)
        {
            KitchenObject instance = PoolManager.Instance.Kitchen.Get(type);
            instance.SetKitchenObjectParent(this);
            return instance;
        }

        #endregion
    }

    public enum CounterType
    {
        ClearCounter = 1,
        ContainerCounter = 2,
        CuttingCounter = 3,
        StoveCounter = 4,
        TrashCounter = 5,
        PlatesCounter = 6,
        DeliveryCounter = 7
    }
}


