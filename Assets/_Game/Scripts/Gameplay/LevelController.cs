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
        
        public void LoadLevel(string levelName)
        {
            ClearLevel();
            //Debug.Log("Load Level");
            TextAsset jsonAsset = Resources.Load<TextAsset>("Levels/Level_" + levelName);
            if (jsonAsset == null)
            {
                Debug.LogError($"[LevelController] Level file not found: Levels/Level_{levelName}");
                return;
            }

            LevelData data = JsonUtility.FromJson<LevelData>(jsonAsset.text);

            if (data.cameraPosition != Vector3.zero && Camera.main != null)
            {
                CameraManager.Instance.GetMainCam.transform.position = data.cameraPosition;
                CameraManager.Instance.GetMainCam.transform.eulerAngles = data.cameraEulerAngles;
            }

            foreach (var cData in data.counterList)
            {
                SpawnCounter(cData);
            }

            Debug.Log($"[LevelController] Loaded level: {levelName} ({data.counterList.Count} counters)");
        }
        
        public void ClearLevel()
        {
            foreach (var counter in _spawnedCounters)
            {
                if (counter != null) 
                    PoolManager.Instance.Counter.Release(counter);
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
            
            // if (counterParent == null)
            //     counterParent = new GameObject("LevelCounters").transform;
            
            BaseCounter counter = PoolManager.Instance.Counter.Get(counterType);
            if (counter == null) return;
            counter.transform.SetParent(counterParent);
            counter.transform.SetPositionAndRotation(cData.position, Quaternion.Euler(cData.rotation));
            ApplyConfiguration(counter, cData.counterId);
            counter.Init();
            _spawnedCounters.Add(counter);
            if (counter is PlatesCounter platesCounter)
            {
                _platesCounters.Add(platesCounter);
            }

            if (cData.kitchenObjectFoodType >= 0 && counter is ClearCounter clearCounter)
            {
                KitchenObject kitchenGO = PoolManager.Instance.Kitchen.Get((KitchenType)cData.kitchenObjectFoodType);
                if (kitchenGO != null)
                {
                    kitchenGO.transform.localPosition = Vector3.zero;
                    kitchenGO.transform.localRotation = Quaternion.identity;
                    clearCounter.SetKitchenObject(kitchenGO);
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
