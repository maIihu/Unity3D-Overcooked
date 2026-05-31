using System.Collections.Generic;
using Counter;
using Kitchen;
using Pooling;
using UnityEngine;

namespace GameCore
{
    public class LevelController : MonoBehaviour
    {
        [Header("Level Config")]
        [SerializeField] private Transform counterParent;

        private readonly List<BaseCounter> _spawnedCounters = new List<BaseCounter>();
        private readonly List<PlatesCounter> _platesCounters = new List<PlatesCounter>();
        private GameObject _currentLevelInstance;
        
        public void LoadLevel(string levelName)
        {
            ClearLevel();

            GameObject prefab = Resources.Load<GameObject>("Levels/Level_" + levelName);
            if (prefab == null)
            {
                Debug.LogError($"[LevelController] Level prefab not found: Levels/Level_{levelName}");
                return;
            }

            // Instantiate prefab to retain environment props
            _currentLevelInstance = Instantiate(prefab, counterParent);
            _currentLevelInstance.name = "Level_" + levelName;

            LevelPrefabData prefabData = _currentLevelInstance.GetComponent<LevelPrefabData>();
            if (prefabData == null)
            {
                Debug.LogError($"[LevelController] LevelPrefabData missing on prefab: Level_{levelName}");
                return;
            }

            if (prefabData.cameraPosition != Vector3.zero && Camera.main != null)
            {
                CameraManager.Instance.GetMainCam.transform.position = prefabData.cameraPosition;
                CameraManager.Instance.GetMainCam.transform.eulerAngles = prefabData.cameraEulerAngles;
            }

            foreach (var templateCounter in prefabData.baseCounters)
            {
                if (templateCounter == null) continue;
                SpawnCounter(templateCounter);
                Destroy(templateCounter.gameObject); // Remove non-networked template counter from environment
            }

            Debug.Log($"[LevelController] Loaded level environment & counters: {levelName} ({prefabData.baseCounters.Count} counters)");
        }
        
        public void ClearLevel()
        {
            if (GameCore.Network.FusionNetworkRunner.Instance != null && 
                GameCore.Network.FusionNetworkRunner.Instance.Runner != null &&
                GameCore.Network.FusionNetworkRunner.Instance.Runner.IsServer)
            {
                foreach (var counter in _spawnedCounters)
                {
                    if (counter != null && counter.Object != null) 
                        GameCore.Network.FusionNetworkRunner.Instance.Runner.Despawn(counter.Object);
                }
            }
            _spawnedCounters.Clear();
            _platesCounters.Clear();

            if (_currentLevelInstance != null)
            {
                Destroy(_currentLevelInstance);
                _currentLevelInstance = null;
            }
        }

        public PlatesCounter GetEmptyPlatesCounter()
        {
            foreach (var counter in _platesCounters)
            {
                if (!counter.HasKitchenObject()) return counter;
            }
            return null;
        }

        public DeliveryCounter GetDeliveryCounter()
        {
            foreach (var counter in _spawnedCounters)
            {
                if (counter is DeliveryCounter dc) return dc;
            }
            return null;
        }

        public void RegisterSpawnedCounter(BaseCounter counter)
        {
            if (!_spawnedCounters.Contains(counter))
            {
                _spawnedCounters.Add(counter);
            }
            if (counter is PlatesCounter platesCounter && !_platesCounters.Contains(platesCounter))
            {
                _platesCounters.Add(platesCounter);
            }
            if (counterParent != null)
            {
                counter.transform.SetParent(counterParent);
            }
        }

        // ── Private ────────────────────────────────────────────

        private void SpawnCounter(BaseCounter templateCounter)
        {
            if (GameCore.Network.FusionNetworkRunner.Instance == null || 
                GameCore.Network.FusionNetworkRunner.Instance.Runner == null ||
                !GameCore.Network.FusionNetworkRunner.Instance.Runner.IsServer)
            {
                return; // Only Host spawns networked counters
            }

            CounterType counterType = LevelDesignerManager.GetCounterType(templateCounter);
            
            BaseCounter prefab = PoolManager.Instance.Counter.GetPrefab(counterType);
            if (prefab == null) return;

            var netObj = GameCore.Network.FusionNetworkRunner.Instance.Runner.Spawn(
                prefab, 
                templateCounter.transform.position, 
                templateCounter.transform.rotation
            );
            BaseCounter counter = netObj.GetComponent<BaseCounter>();

            // Copy configuration
            if (counter is ContainerCounter container && templateCounter is ContainerCounter templateContainer)
            {
                container.SetContainer(templateContainer.ContainerEFoodType);
            }
            else if (counter is StoveCounter stove && templateCounter is StoveCounter templateStove)
            {
                stove.SetStoveData(templateStove.KitchenType);
            }

            // Spawn pre-placed KitchenObject if any
            KitchenObject templateKitchenObj = templateCounter.GetComponentInChildren<KitchenObject>(true);
            if (templateKitchenObj != null && counter is ClearCounter clearCounter)
            {
                KitchenObject kPrefab = null;
                if (templateKitchenObj is PlateObject)
                    kPrefab = PoolManager.Instance.Kitchen.GetPrefab(KitchenType.Plate);
                else if (templateKitchenObj is PotObject)
                    kPrefab = PoolManager.Instance.Kitchen.GetPrefab(KitchenType.Pot);
                else if (templateKitchenObj is PanObject)
                    kPrefab = PoolManager.Instance.Kitchen.GetPrefab(KitchenType.Pan);

                if (kPrefab != null)
                {
                    var kNetObj = GameCore.Network.FusionNetworkRunner.Instance.Runner.Spawn(kPrefab, clearCounter.GetKitchenObjectToTransform().position, Quaternion.identity);
                    KitchenObject kitchenGO = kNetObj.GetComponent<KitchenObject>();
                    clearCounter.SetKitchenObject(kitchenGO);
                }
            }
        }
    }
}
