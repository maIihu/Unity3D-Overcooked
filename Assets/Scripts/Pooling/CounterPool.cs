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
    }
}
