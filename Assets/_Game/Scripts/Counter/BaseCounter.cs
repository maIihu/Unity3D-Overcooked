using _Game.Scripts.Gameplay;
using Kitchen;
using Pooling;
using UnityEngine;
using Fusion;

namespace Counter
{
    public class BaseCounter : NetworkBehaviour, IKitchenObjectParent, IPoolable
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
        public virtual void Interact(IPlayer player) { }

        public virtual void InteractAlternate(IPlayer player) { }
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
        
        public Fusion.NetworkObject GetNetworkObject() => Object;

        #endregion

        #region Network Spawning
        
        protected KitchenObject SpawnKitchenObject(EFoodType type)
        {
            if (!HasStateAuthority) return null;
            var prefab = PoolManager.Instance.Kitchen.GetPrefab(type);
            if (prefab == null)
            {
                Debug.LogError($"[BaseCounter] SpawnKitchenObject: Prefab for {type} is null!");
                return null;
            }

            var networkObject = Runner.Spawn(prefab, transform.position, Quaternion.identity);
            if (networkObject == null)
            {
                Debug.LogError($"[BaseCounter] SpawnKitchenObject: Runner.Spawn returned null for {prefab.name}!");
                return null;
            }
            
            var instance = networkObject.GetComponent<KitchenObject>();
            if (instance != null)
            {
                instance.SetKitchenObjectParent(this);
            }
            else
            {
                Debug.LogError($"[BaseCounter] SpawnKitchenObject: instance is null after spawn!");
            }
            return instance;
        }
        
        protected KitchenObject SpawnKitchenObject(KitchenType type)
        {
            if (!HasStateAuthority) return null;
            var prefab = PoolManager.Instance.Kitchen.GetPrefab(type);
            if (prefab == null) return null;

            var networkObject = Runner.Spawn(prefab, transform.position, Quaternion.identity);
            var instance = networkObject.GetComponent<KitchenObject>();
            if (instance != null)
            {
                instance.SetKitchenObjectParent(this);
            }
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


