using System.Collections.Generic;
using Counter;
using UnityEngine;
using UnityEngine.Pool;
using DesignPattern;
using Kitchen;

namespace Pooling
{
    /// <summary>
    /// Centralized Hub for all object pooling.
    /// Manages specialized pools (KitchenObjectPool, CounterPool) and handles generic GameObjects.
    /// </summary>
    public class PoolManager : Singleton<PoolManager>
    {
        [Header("Specialized Pools")]
        [SerializeField] private KitchenObjectPool kitchen;
        [SerializeField] private CounterPool counter;

        public KitchenObjectPool Kitchen => kitchen;
        public CounterPool Counter => counter;

        [Header("Generic Pools")]
        [SerializeField] private List<PoolConfig> genericPrewarmPools;
        
        private Dictionary<GameObject, IObjectPool<GameObject>> _genericPools = new();
        private Dictionary<GameObject, IObjectPool<GameObject>> _instanceToPoolMap = new();

        [System.Serializable]
        public class PoolConfig
        {
            public GameObject prefab;
            public int defaultCapacity = 10;
            public int maxSize = 50;
        }

        protected void Awake()
        {
            Initialize(this);
            
            // Prewarm generic pools
            if (genericPrewarmPools != null)
            {
                foreach (var config in genericPrewarmPools)
                {
                    GetPool(config.prefab, config.defaultCapacity, config.maxSize);
                }
            }
        }

        #region Generic Pooling (Fallback for non-typed objects)
        public IObjectPool<GameObject> GetPool(GameObject prefab, int defaultCapacity = 10, int maxSize = 50)
        {
            if (_genericPools.TryGetValue(prefab, out var pool)) return pool;

            var newPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: (obj) => {
                    obj.SetActive(true);
                    foreach (var p in obj.GetComponentsInChildren<IPoolable>()) p.OnSpawn();
                },
                actionOnRelease: (obj) => {
                    foreach (var p in obj.GetComponentsInChildren<IPoolable>()) p.OnDespawn();
                    obj.SetActive(false);
                },
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            _genericPools.Add(prefab, newPool);
            return newPool;
        }

        public GameObject Get(GameObject prefab)
        {
            var pool = GetPool(prefab);
            var instance = pool.Get();
            _instanceToPoolMap[instance] = pool;
            return instance;
        }

        public T Get<T>(T prefab) where T : Component 
        {
            var go = Get(prefab.gameObject);
            return go.GetComponent<T>();
        }
        #endregion

        #region Unified Release
        /// <summary>
        /// Centralized release method. Routes the object back to its correct pool.
        /// </summary>
        public void Release(GameObject instance)
        {
            if (instance == null) return;

            // 1. Try specialized pools first (more efficient)
            if (instance.TryGetComponent<KitchenObject>(out var k))
            {
                if (kitchen != null) kitchen.Release(k);
                else Destroy(instance);
                return;
            }
            
            if (instance.TryGetComponent<BaseCounter>(out var c))
            {
                if (counter != null) counter.Release(c);
                else Destroy(instance);
                return;
            }

            // 2. Fallback to generic pools
            if (_instanceToPoolMap.TryGetValue(instance, out var pool))
            {
                pool.Release(instance);
            }
            else
            {
                Debug.LogWarning($"[PoolManager] No pool found for {instance.name}. Destroying.");
                Destroy(instance);
            }
        }

        public void Release<T>(T component) where T : Component => Release(component.gameObject);
        #endregion
    }
}
