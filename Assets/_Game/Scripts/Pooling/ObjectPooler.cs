using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Pooling
{
    public class ObjectPooler<T> where T : Component
    {
        private readonly T _prefab;
        private readonly IObjectPool<T> _pool;
        private readonly Transform _parent;

        public ObjectPooler(T prefab, Transform parent = null, int defaultCapacity = 10, int maxSize = 50)
        {
            _prefab = prefab;
            _parent = parent;

            _pool = new ObjectPool<T>(
                createFunc: CreateInstance,
                actionOnGet: OnGetFromPool,
                actionOnRelease: OnReturnToPool,
                actionOnDestroy: OnDestroyPooled,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        private T CreateInstance()
        {
            T instance = UnityEngine.Object.Instantiate(_prefab, _parent);
            if (instance.TryGetComponent(out IPoolable poolable))
            {
                //poolable.Init();
            }
            return instance;
        }

        private void OnGetFromPool(T instance)
        {
            instance.gameObject.SetActive(true);
            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnSpawn();
            }
        }

        private void OnReturnToPool(T instance)
        {
            if (instance.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnDespawn();
            }
            instance.gameObject.SetActive(false);
            if (_parent != null) instance.transform.SetParent(_parent);
        }

        private void OnDestroyPooled(T instance)
        {
            UnityEngine.Object.Destroy(instance.gameObject);
        }

        public T Get()
        {
            return _pool.Get();
        }

        public T Get(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            T instance = _pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            if (parent != null) instance.transform.SetParent(parent);
            return instance;
        }

        public void Release(T instance)
        {
            _pool.Release(instance);
        }
    }
}
