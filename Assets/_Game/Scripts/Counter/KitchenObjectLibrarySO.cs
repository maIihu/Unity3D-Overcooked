using System;
using System.Collections.Generic;
using Kitchen;
using UnityEngine;

namespace Counter
{
    [CreateAssetMenu(menuName = "Game/Kitchen Object Library", fileName = "KitchenObjectLibrarySO")]
    public class KitchenObjectLibrarySO : ScriptableObject
    {
        [Serializable]
        public class KitchenEntry
        {
            public KitchenType kitchenType;
            public KitchenObject prefab;
        }

        [SerializeField] private List<KitchenEntry> kitchenEntries = new();

        private Dictionary<KitchenType, KitchenObject> _lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<KitchenType, KitchenObject>();
            foreach (var entry in kitchenEntries)
            {
                if (entry.prefab != null)
                    _lookup[entry.kitchenType] = entry.prefab;
            }
        }

        public KitchenObject GetPrefab(KitchenType kitchenType)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(kitchenType, out var prefab) ? prefab : null;
        }
    }
}
