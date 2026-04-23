using System.Collections.Generic;
using System.IO;
using Counter;
using UnityEngine;

public class LevelDesignerManager : MonoBehaviour
{
    public static LevelDesignerManager Instance { get; private set; }

    [SerializeField] private CounterTemplateListSO templateList;
    [SerializeField] private Transform counterParent;
    [SerializeField] private ObjectPlacementController placementController;
    
    private Dictionary<BaseCounter, CounterData> _placedCountersMap = new Dictionary<BaseCounter, CounterData>();

    private void Awake()
    {
        Instance = this;
        if (counterParent == null) counterParent = new GameObject("PlacedCounters").transform;
    }

    public void SpawnCounter(string templateId, Vector3 position, Vector3 rotation)
    {
        CounterTemplate template = templateList.GetTemplateById(templateId);
        if (template == null)
        {
            Debug.LogError($"Template ID {templateId} not found!");
            return;
        }

        GameObject go = Instantiate(template.prefab, position, Quaternion.Euler(rotation), counterParent);
        BaseCounter counter = go.GetComponent<BaseCounter>();
        
        if (counter != null)
        {
            // Create data immediately
            CounterData data = new CounterData
            {
                counterId = templateId,
                position = position,
                rotation = rotation
            };
            
            _placedCountersMap.Add(counter, data);
            ApplyConfiguration(counter, template);
        }
    }

    public void RemoveCounter(BaseCounter counter)
    {
        if (counter != null && _placedCountersMap.ContainsKey(counter))
        {
            _placedCountersMap.Remove(counter);
        }
    }

    private void ApplyConfiguration(BaseCounter counter, CounterTemplate template)
    {
        // Apply special settings based on type
        if (counter is ContainerCounter container)
        {
            // Use reflection or a public field if available to set the food type
            // In this project, ContainerCounter has a private serializable field containerFoodType
            // For now, we manually set it.
            var field = typeof(ContainerCounter).GetField("containerFoodType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(container, template.foodType);
        }
        else if (counter is StoveCounter stove)
        {
            var field = typeof(StoveCounter).GetField("potObjectPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(stove, template.vesselPrefab);
        }
    }

    public void SaveLevel(string levelName)
    {
        LevelData data = new LevelData();
        
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
        string path = Path.Combine(Application.dataPath, "Resources/Levels", levelName + ".json");
        
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
        Debug.Log($"Level saved to: {path}");
    }

    public void LoadLevel(string levelName)
    {
        ClearLevel();
        
        string path = Path.Combine(Application.dataPath, "Resources/Levels", levelName + ".json");
        if (!File.Exists(path))
        {
            Debug.LogError($"Level file not found at: {path}");
            return;
        }

        string json = File.ReadAllText(path);
        LevelData data = JsonUtility.FromJson<LevelData>(json);

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
