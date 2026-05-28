using System.Collections.Generic;
using Counter;
using UnityEngine;

namespace Pooling
{
    public class CounterPool : BaseTypedPoolManager<BaseCounter>
    {
        [System.Serializable]
        public class CounterPoolConfig
        {
            public CounterType type;
            public BaseCounter prefab;
            public int defaultCapacity = 5;
            public int maxSize = 20;
        }

        [SerializeField] private List<CounterPoolConfig> counterPrewarmPools;

        protected override void Prewarm()
        {
            if (counterPrewarmPools == null) return;

            foreach (var config in counterPrewarmPools)
            {
                if (config.prefab != null)
                    GetOrCreatePool(config.prefab, config.defaultCapacity, config.maxSize);
            }
        }
        
        public BaseCounter Get(CounterType type)
        {
            var prefab = GetPrefab(type);
            if (prefab != null) return Get(prefab);
            
            Debug.LogError($"[KitchenObjectPool] No prefab found for KitchenType: {type}");
            return null;
        }

        public BaseCounter GetPrefab(CounterType type)
        {
            foreach (var counter in counterPrewarmPools)
            {
                if (counter.type == type)
                {
                    return counter.prefab;
                }
            }
            return null;
        }
    }
}
