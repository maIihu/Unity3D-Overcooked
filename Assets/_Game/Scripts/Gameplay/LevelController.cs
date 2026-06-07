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

        // Cached DeliveryController instance — set khi offline spawn
        private DeliveryController _deliveryControllerInstance;
        
        public async System.Threading.Tasks.Task LoadLevelAsync(string levelName)
        {
            ClearLevel();

            ResourceRequest request = Resources.LoadAsync<GameObject>("Levels/Level_" + levelName);
            while (!request.isDone)
            {
                await System.Threading.Tasks.Task.Yield();
            }

            GameObject prefab = request.asset as GameObject;
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
                _Game.Scripts.DesignPattern.Observer.MessageManager.Instance.SendMessage(
                    new _Game.Scripts.DesignPattern.Observer.Message(_Game.Scripts.DesignPattern.Observer.ProjectMessageType.OnSetupCamera, new object[] { prefabData.cameraPosition, prefabData.cameraEulerAngles })
                );
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
            if (GameManager.Instance != null && GameManager.Instance.IsOffline)
            {
                // ── Offline: Destroy bình thường ──
                foreach (var counter in _spawnedCounters)
                {
                    if (counter != null) Destroy(counter.gameObject);
                }
            }
            else
            {
                // ── Online: Despawn qua Fusion Runner ──
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
            }

            _spawnedCounters.Clear();
            _platesCounters.Clear();
            _deliveryControllerInstance = null;

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

        /// <summary>
        /// Trả về DeliveryController instance đã spawn (dùng cho offline mode).
        /// </summary>
        public DeliveryController GetDeliveryControllerInstance() => _deliveryControllerInstance;

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
            bool isOffline = GameManager.Instance != null && GameManager.Instance.IsOffline;

            CounterType counterType = LevelDesignerManager.GetCounterType(templateCounter);
            BaseCounter prefab = PoolManager.Instance.Counter.GetPrefab(counterType);
            if (prefab == null) return;

            BaseCounter counter;

            if (isOffline)
            {
                // ── Offline: Instantiate trực tiếp — ZERO Fusion overhead ──
                var go = Instantiate(prefab, templateCounter.transform.position, templateCounter.transform.rotation);
                counter = go.GetComponent<BaseCounter>();
                counter.Init(); // Gọi Init() vì Spawned() sẽ không được gọi khi không có Fusion
                RegisterSpawnedCounter(counter);
            }
            else
            {
                // ── Online: Spawn qua Fusion Runner ──
                if (GameCore.Network.FusionNetworkRunner.Instance == null || 
                    GameCore.Network.FusionNetworkRunner.Instance.Runner == null ||
                    !GameCore.Network.FusionNetworkRunner.Instance.Runner.IsServer)
                {
                    return; // Only Host spawns networked counters
                }

                var netObj = GameCore.Network.FusionNetworkRunner.Instance.Runner.Spawn(
                    prefab, 
                    templateCounter.transform.position, 
                    templateCounter.transform.rotation
                );
                counter = netObj.GetComponent<BaseCounter>();
            }

            // Copy configuration
            if (counter is ContainerCounter container && templateCounter is ContainerCounter templateContainer)
            {
                container.SetContainer(templateContainer.ContainerEFoodType);
            }
            else if (counter is StoveCounter stove && templateCounter is StoveCounter templateStove)
            {
                stove.SetStoveData(templateStove.KitchenType);
            }

            // Track DeliveryController instance cho offline resolve
            if (counter is DeliveryCounter dc)
            {
                _deliveryControllerInstance = dc.GetComponent<DeliveryController>();
            }

            // Spawn pre-placed KitchenObject if any
            SpawnPreplacedKitchenObject(templateCounter, counter, isOffline);
        }

        private void SpawnPreplacedKitchenObject(BaseCounter templateCounter, BaseCounter counter, bool isOffline)
        {
            KitchenObject templateKitchenObj = templateCounter.GetComponentInChildren<KitchenObject>(true);
            if (templateKitchenObj == null || counter is not ClearCounter clearCounter) return;

            KitchenObject kPrefab = null;
            if (templateKitchenObj is PlateObject)
                kPrefab = PoolManager.Instance.Kitchen.GetPrefab(KitchenType.Plate);
            else if (templateKitchenObj is PotObject)
                kPrefab = PoolManager.Instance.Kitchen.GetPrefab(KitchenType.Pot);
            else if (templateKitchenObj is PanObject)
                kPrefab = PoolManager.Instance.Kitchen.GetPrefab(KitchenType.Pan);

            if (kPrefab == null) return;

            if (isOffline)
            {
                // ── Offline: Instantiate KitchenObject ──
                var kitchenGO = Instantiate(kPrefab, clearCounter.GetKitchenObjectToTransform().position, Quaternion.identity);
                var ko = kitchenGO.GetComponent<KitchenObject>();
                if (ko != null)
                {
                    ko.SetKitchenObjectParent(clearCounter);
                }
            }
            else
            {
                // ── Online: Spawn qua Fusion ──
                var kNetObj = GameCore.Network.FusionNetworkRunner.Instance.Runner.Spawn(
                    kPrefab, clearCounter.GetKitchenObjectToTransform().position, Quaternion.identity);
                KitchenObject kitchenGO = kNetObj.GetComponent<KitchenObject>();
                clearCounter.SetKitchenObject(kitchenGO);
            }
        }
    }
}
