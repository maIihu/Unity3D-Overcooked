using System.Collections.Generic;
using UnityEngine;

namespace Pooling
{
    public abstract class BaseTypedPoolManager<T> : MonoBehaviour 
        where T : Component 
    {
        protected Transform PoolRoot;
        protected readonly Dictionary<T, ObjectPooler<T>> Pools = new();
        protected readonly Dictionary<T, T> InstanceToPrefabMap = new();

        protected virtual void Awake()
        {
            PoolRoot = new GameObject($"[{this.GetType().Name}]").transform;
            PoolRoot.SetParent(transform);
            
            Prewarm();
        }
  
        protected abstract void Prewarm();

        protected ObjectPooler<T> GetOrCreatePool(T prefab, int defaultCapacity = 10, int maxSize = 50)
        {
            if (prefab == null) return null;
            
            if (Pools.TryGetValue(prefab, out var pool))
                return pool;

            var newPool = new ObjectPooler<T>(prefab, PoolRoot, defaultCapacity, maxSize);
            Pools.Add(prefab, newPool);
            return newPool;
        }

        public virtual TResult Get<TResult>(TResult prefab) where TResult : T
        {
            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();
            InstanceToPrefabMap[instance] = prefab;
            return (TResult)instance;
        }

        public virtual TResult Get<TResult>(TResult prefab, Vector3 position, Quaternion rotation, Transform parent = null) where TResult : T
        {
            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get(position, rotation, parent);
            InstanceToPrefabMap[instance] = prefab;
            return (TResult)instance;
        }

        public virtual void Release(T instance)
        {
            if (instance == null) return;
            
            if (InstanceToPrefabMap.TryGetValue(instance, out var prefab))
            {
                Pools[prefab].Release(instance);
            }
            else
            {
                Debug.LogWarning($"[{this.GetType().Name}] Object '{instance.name}' was not spawned from this pool. Destroying instead.");
                Destroy(instance.gameObject);
            }
        }
    }
}
