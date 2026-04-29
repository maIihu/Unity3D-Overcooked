using UnityEngine;
using Counter;
using Kitchen;

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

            Vector3 minBounds = new Vector3(float.MaxValue, 0, float.MaxValue);
            Vector3 maxBounds = new Vector3(float.MinValue, 0, float.MinValue);

            foreach (var cData in data.counterList)
            {
                SpawnCounter(cData.counterId, cData.position, cData.rotation);

                if (cData.position.x < minBounds.x) minBounds.x = cData.position.x;
                if (cData.position.x > maxBounds.x) maxBounds.x = cData.position.x;
                if (cData.position.z < minBounds.z) minBounds.z = cData.position.z;
                if (cData.position.z > maxBounds.z) maxBounds.z = cData.position.z;
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
            CounterTemplate template = templateList.GetTemplateByType(counterType);

            if (template == null)
            {
                Debug.LogError($"No template found for CounterType {counterType} (id={counterId})!");
                return;
            }

            GameObject go = Instantiate(template.prefab, position, Quaternion.Euler(rotation), counterParent);
            BaseCounter counter = go.GetComponent<BaseCounter>();

            if (counter != null)
            {
                counter.Init();
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
                        stove.SetStoveData(CounterIdConverter.GetStoveKitchenType(counterId));
                    }
                    break;
            }
        }
    }
}
