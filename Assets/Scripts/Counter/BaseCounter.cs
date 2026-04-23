using Kitchen;
using Pooling;
using UnityEngine;

namespace Counter
{
    /// <summary>
    /// Base class for all counters in the kitchen.
    /// Handles IKitchenObjectParent logic and integrates with the Pooling system.
    /// </summary>
    public class BaseCounter : MonoBehaviour, IKitchenObjectParent, IPoolable
    {
        [SerializeField] private Transform counterTopPoint;
        [SerializeField] private GameObject selectedCounter;

        private KitchenObject _kitchenObject;

        #region Unity Methods

        protected virtual void Awake() { }

        protected virtual void Start()
        {
            Hide();
        }
        #endregion

        #region Public Methods

        public virtual void Init() { }

        public virtual void OnSpawn() { }

        public virtual void OnDespawn()
        {
            // When the counter is released to the pool, make sure it doesn't leave a "ghost" kitchen object
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
        public virtual void Interact(Player.Player player) { }

        public virtual void InteractAlternate(Player.Player player) { }
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

        /// <summary>
        /// Spawns a KitchenObject from the pool and places it on this counter.
        /// </summary>
        protected T SpawnKitchenObject<T>(T prefab) where T : KitchenObject
        {
            T instance = PoolManager.Instance.Kitchen.Get(prefab);
            instance.SetKitchenObjectParent(this);
            return instance;
        }

        /// <summary>
        /// Spawns a KitchenObject by FoodType and places it on this counter.
        /// </summary>
        protected KitchenObject SpawnKitchenObject(FoodType type)
        {
            KitchenObject instance = PoolManager.Instance.Kitchen.Get(type);
            instance.SetKitchenObjectParent(this);
            return instance;
        }

        /// <summary>
        /// Spawns a KitchenObject by KitchenType and places it on this counter.
        /// </summary>
        protected KitchenObject SpawnKitchenObject(KitchenType type)
        {
            KitchenObject instance = PoolManager.Instance.Kitchen.Get(type);
            instance.SetKitchenObjectParent(this);
            return instance;
        }

        #endregion
    }
}


