using System.Collections.Generic;
using Kitchen;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pooling
{
    public class KitchenObjectPool : BaseTypedPoolManager<KitchenObject>
    {
        [System.Serializable]
        public class KitchenPoolConfig
        {
            public KitchenObject prefab;
            public KitchenType kitchenType;
            public int defaultCapacity = 10;
            public int maxSize = 50;
        }

        [System.Serializable]
        public class FoodPoolConfig
        {
            public KitchenObject prefab;
            [FormerlySerializedAs("foodType")] public EFoodType eFoodType;
            public int defaultCapacity = 10;
            public int maxSize = 50;
        }

        [SerializeField] private List<KitchenPoolConfig> kitchenPrewarmPools;
        [SerializeField] private List<FoodPoolConfig> foodPrewarmPools;

        private readonly Dictionary<KitchenType, KitchenObject> _kitchenTypeToPrefab = new();
        private readonly Dictionary<EFoodType, KitchenObject> _foodTypeToPrefab = new();

        protected override void Prewarm()
        {
            if (kitchenPrewarmPools == null) return;

            foreach (var config in kitchenPrewarmPools)
            {
                if (config.prefab == null) continue;

                GetOrCreatePool(config.prefab, config.defaultCapacity, config.maxSize);
                _kitchenTypeToPrefab[config.kitchenType] = config.prefab;
            }

            if (foodPrewarmPools == null) return;

            foreach (var config in foodPrewarmPools)
            {
                if (config.prefab == null) continue;

                GetOrCreatePool(config.prefab, config.defaultCapacity, config.maxSize);
                _foodTypeToPrefab[config.eFoodType] = config.prefab;
            }
        }

        public KitchenObject Get(KitchenType type)
        {
            if (_kitchenTypeToPrefab.TryGetValue(type, out var prefab))
            {
                return Get(prefab);
            }
            Debug.LogError($"[KitchenObjectPool] No prefab found for KitchenType: {type}");
            return null;
        }

        public KitchenObject Get(EFoodType type)
        {
            if (_foodTypeToPrefab.TryGetValue(type, out var prefab))
            {
                return Get(prefab);
            }
            Debug.LogError($"[KitchenObjectPool] No prefab found for FoodType: {type}");
            return null;
        }

        public T Get<T>(KitchenType type) where T : KitchenObject => Get(type) as T;
        public T Get<T>(EFoodType type) where T : KitchenObject => Get(type) as T;

        public KitchenObject Get(KitchenType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (_kitchenTypeToPrefab.TryGetValue(type, out var prefab))
            {
                return Get(prefab, position, rotation, parent);
            }
            return null;
        }

        public KitchenObject Get(EFoodType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (_foodTypeToPrefab.TryGetValue(type, out var prefab))
            {
                return Get(prefab, position, rotation, parent);
            }
            return null;
        }

        public T Get<T>(KitchenType type, Vector3 position, Quaternion rotation, Transform parent = null) where T : KitchenObject
            => Get(type, position, rotation, parent) as T;
        public T Get<T>(EFoodType type, Vector3 position, Quaternion rotation, Transform parent = null) where T : KitchenObject
            => Get(type, position, rotation, parent) as T;
    }
}
