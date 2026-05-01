using System.Collections.Generic;
using System.IO;
using Counter;
using Kitchen;
using UnityEngine;

public class LevelDesignerManager : MonoBehaviour
{
    public static LevelDesignerManager Instance { get; private set; }

    [SerializeField] private CounterTemplateListSO templateList;
    [SerializeField] private Transform counterParent;
    [SerializeField] private ObjectPlacementController placementController;
    [SerializeField] private Camera levelPreviewCamera;

    private Dictionary<BaseCounter, CounterData> _placedCountersMap = new Dictionary<BaseCounter, CounterData>();

    private void Awake()
    {
        Instance = this;
        if (counterParent == null) counterParent = new GameObject("PlacedCounters").transform;
    }
    
    public void SpawnCounter(int counterId, Vector3 position, Vector3 rotation)
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
            CounterData data = new CounterData
            {
                counterId = counterId,
                position = position,
                rotation = rotation
            };

            _placedCountersMap.Add(counter, data);
            ApplyConfiguration(counter, counterId);
        }
    }

    public void RemoveCounter(BaseCounter counter)
    {
        if (counter != null && _placedCountersMap.ContainsKey(counter))
        {
            _placedCountersMap.Remove(counter);
        }
    }

    public bool TryGetCounterData(BaseCounter counter, out CounterData data)
    {
        if (counter != null && _placedCountersMap.TryGetValue(counter, out data))
        {
            return true;
        }
        
        data = default;
        return false;
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
    }

    public void SaveLevel(string levelName)
    {
        LevelData data = new LevelData();

        if (levelPreviewCamera != null)
        {
            data.cameraPosition = levelPreviewCamera.transform.position;
            data.cameraEulerAngles = levelPreviewCamera.transform.eulerAngles;
        }

        foreach (var pair in _placedCountersMap)
        {
            BaseCounter counter = pair.Key;
            CounterData cData = pair.Value;

            if (counter == null) continue;

            // Sync current transform to data
            cData.position = counter.transform.position;
            cData.rotation = counter.transform.eulerAngles;

            data.counterList.Add(cData);
        }
        
        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(Application.dataPath, "Resources/Levels", "Level_" + levelName + ".json");

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
        Debug.Log($"Level saved to: {path}");
    }

    public void LoadLevel(string levelName)
    {
        ClearLevel();

        string path = Path.Combine(Application.dataPath, "Resources/Levels", "Level_"+ levelName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogError($"Level file not found at: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        LevelData data = JsonUtility.FromJson<LevelData>(json);

        if (levelPreviewCamera != null && data.cameraPosition != Vector3.zero)
        {
            levelPreviewCamera.transform.position = data.cameraPosition;
            levelPreviewCamera.transform.eulerAngles = data.cameraEulerAngles;
        }

        foreach (var cData in data.counterList)
        {
            SpawnCounter(cData.counterId, cData.position, cData.rotation);
        }
    }

    public void ClearLevel()
    {
        foreach (var counter in _placedCountersMap.Keys)
        {
            if (counter != null) Destroy(counter.gameObject);
        }
        _placedCountersMap.Clear();
    }
}
