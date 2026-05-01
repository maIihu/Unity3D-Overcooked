using UnityEngine;
using Counter;
using Kitchen;
using Pooling;

namespace GameCore
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private TextAsset levelJson;
        [SerializeField] private CounterTemplateListSO templateList;
        [SerializeField] private Transform counterParent;

        private void Start()
        {
            if (levelJson != null)
            {
                LoadLevel(levelJson);
            }
            else
            {
                Debug.LogWarning("LevelController: No level JSON assigned.");
            }
        }

        private void LoadLevel(TextAsset jsonAsset)
        {
            if (jsonAsset == null) return;
            
            LevelData data = JsonUtility.FromJson<LevelData>(jsonAsset.text);
            if (data == null || data.counterList == null || data.counterList.Count == 0)
            {
                Debug.LogError("Failed to parse LevelData from JSON or level is empty.");
                return;
            }

            foreach (var cData in data.counterList)
            {
                SpawnCounter(cData.counterId, cData.position, cData.rotation);
            }
            if (Camera.main != null)
            {
                Camera.main.transform.position = data.cameraPosition;
                Camera.main.transform.eulerAngles = data.cameraEulerAngles;
            }
        }

        private void SpawnCounter(int counterId, Vector3 position, Vector3 rotation)
        {
            CounterType counterType = CounterIdConverter.GetCounterType(counterId);

            //GameObject go = Instantiate(template.prefab, position, Quaternion.Euler(rotation), counterParent);
            var counter = PoolManager.Instance.Counter.Get(counterType);
            counter.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
            counter.transform.SetParent(counterParent);

            if (counter != null)
            {
                ApplyConfiguration(counter, counterId);
            }
        }

        private void ApplyConfiguration(BaseCounter counter, int counterId)
        {
            CounterType counterType = CounterIdConverter.GetCounterType(counterId);

            switch (counterType)
            {
                case CounterType.ContainerCounter:
                    if (counter is ContainerCounter container)
                    {
                        container.SetContainer(CounterIdConverter.GetFoodType(counterId));
                    }
                    break;

                case CounterType.StoveCounter:
                    if (counter is StoveCounter stove)
                    {
                        stove.SetStoveData(CounterIdConverter.GetKitchenType(counterId));
                    }
                    break;
            }
            counter.Init();

        }
    }
}
