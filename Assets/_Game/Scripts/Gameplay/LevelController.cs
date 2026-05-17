using System.Collections.Generic;
using Counter;
using Kitchen;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Loads and spawns level data at runtime.
    /// NOT a Singleton — accessed via GameManager.Instance.LevelController.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [Header("Level Config")]
        [SerializeField] private CounterTemplateListSO templateList;
        [SerializeField] private KitchenObjectLibrarySO kitchenObjectLibrary;
        [SerializeField] private Transform counterParent;

        private readonly List<BaseCounter> _spawnedCounters = new List<BaseCounter>();
        private readonly List<PlatesCounter> _platesCounters = new List<PlatesCounter>();

        // ── Public API ─────────────────────────────────────────

        /// <summary>
        /// Load a level from Resources/Levels by name and spawn all counters.
        /// </summary>
        public void LoadLevel(string levelName)
        {
            ClearLevel();

            TextAsset jsonAsset = Resources.Load<TextAsset>("Levels/Level_" + levelName);
            if (jsonAsset == null)
            {
                Debug.LogError($"[LevelController] Level file not found: Levels/Level_{levelName}");
                return;
            }

            LevelData data = JsonUtility.FromJson<LevelData>(jsonAsset.text);

            // Apply camera
            if (data.cameraPosition != Vector3.zero && Camera.main != null)
            {
                Camera.main.transform.position = data.cameraPosition;
                Camera.main.transform.eulerAngles = data.cameraEulerAngles;
            }

            // Spawn counters
            foreach (var cData in data.counterList)
            {
                SpawnCounter(cData);
            }

            Debug.Log($"[LevelController] Loaded level: {levelName} ({data.counterList.Count} counters)");
        }

        /// <summary>
        /// Destroy all spawned counters.
        /// </summary>
        public void ClearLevel()
        {
            foreach (var counter in _spawnedCounters)
            {
                if (counter != null) Destroy(counter.gameObject);
            }
            _spawnedCounters.Clear();
            _platesCounters.Clear();
        }

        public PlatesCounter GetEmptyPlatesCounter()
        {
            foreach (var counter in _platesCounters)
            {
                if (!counter.HasKitchenObject()) return counter;
            }
            return null;
        }

        // ── Private ────────────────────────────────────────────

        private void SpawnCounter(CounterData cData)
        {
            CounterType counterType = CounterIdConverter.GetCounterType(cData.counterId);
            CounterTemplate template = templateList.GetTemplateByType(counterType);

            if (template == null)
            {
                Debug.LogError($"[LevelController] No template for CounterType {counterType} (id={cData.counterId})");
                return;
            }

            if (counterParent == null)
                counterParent = new GameObject("LevelCounters").transform;

            GameObject go = Instantiate(template.prefab, cData.position, Quaternion.Euler(cData.rotation), counterParent);
            BaseCounter counter = go.GetComponent<BaseCounter>();

            if (counter == null) return;
            counter.Init();
            _spawnedCounters.Add(counter);
            if (counter is PlatesCounter platesCounter)
            {
                _platesCounters.Add(platesCounter);
            }
            
            ApplyConfiguration(counter, cData.counterId);

            // Restore pre-placed KitchenObject
            if (cData.kitchenObjectFoodType >= 0 && counter is ClearCounter clearCounter)
            {
                if (kitchenObjectLibrary != null)
                {
                    KitchenObject prefab = kitchenObjectLibrary.GetPrefab((KitchenType)cData.kitchenObjectFoodType);
                    if (prefab != null)
                    {
                        KitchenObject instance = Instantiate(prefab, clearCounter.GetKitchenObjectToTransform());
                        instance.transform.localPosition = Vector3.zero;
                        instance.transform.localRotation = Quaternion.identity;
                        clearCounter.SetKitchenObject(instance);
                    }
                }
            }
        }

        private void ApplyConfiguration(BaseCounter counter, int counterId)
        {
            CounterType counterType = CounterIdConverter.GetCounterType(counterId);

            switch (counterType)
            {
                case CounterType.ContainerCounter:
                    if (counter is ContainerCounter container)
                        container.SetContainer(CounterIdConverter.GetFoodType(counterId));
                    break;

                case CounterType.StoveCounter:
                    if (counter is StoveCounter stove)
                        stove.SetStoveData(CounterIdConverter.GetKitchenType(counterId));
                    break;
            }
        }
    }
}
