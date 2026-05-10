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
    [SerializeField] private LevelDesignerUI levelDesignerUI;
    [SerializeField] private KitchenObjectLibrarySO kitchenObjectLibrary;
    [SerializeField] private Camera levelPreviewCamera;

    public KitchenObjectLibrarySO GetKitchenObjectLibrary() => kitchenObjectLibrary;

    private Dictionary<BaseCounter, CounterData> _placedCountersMap = new Dictionary<BaseCounter, CounterData>();

    private void Awake()
    {
        Instance = this;
        if (counterParent == null) counterParent = new GameObject("PlacedCounters").transform;
    }
    

    public void SpawnCounter(int counterId, Vector3 position, Vector3 rotation, int kitchenObjectFoodType = -1)
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
                rotation = rotation,
                kitchenObjectFoodType = kitchenObjectFoodType
            };

            _placedCountersMap.Add(counter, data);
            ApplyConfiguration(counter, counterId);

            // Restore pre-placed KitchenObject
            if (kitchenObjectFoodType >= 0 && counter is ClearCounter clearCounter)
            {
                if (kitchenObjectLibrary != null)
                {
                    KitchenObject prefab = kitchenObjectLibrary.GetPrefab((KitchenType)kitchenObjectFoodType);
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
    }

    public void RemoveCounter(BaseCounter counter)
    {
        if (counter != null && _placedCountersMap.ContainsKey(counter))
        {
            _placedCountersMap.Remove(counter);
        }
    }

    /// <summary>
    /// Update the KitchenObject pre-placed on a ClearCounter and persist the change in CounterData.
    /// Pass foodType = -1 to clear the item.
    /// </summary>
    public void SetKitchenObjectOnCounter(BaseCounter counter, int foodType)
    {
        if (!_placedCountersMap.TryGetValue(counter, out CounterData data)) return;

        data.kitchenObjectFoodType = foodType;

        if (counter is ClearCounter clearCounter)
        {
            if (foodType >= 0)
            {
                // Logic Spawn trực tiếp cho Editor (không pool)
                if (kitchenObjectLibrary == null)
                {
                    Debug.LogWarning("[LevelDesignerManager] KitchenObjectLibrarySO is not assigned!");
                    return;
                }

                KitchenObject prefab = kitchenObjectLibrary.GetPrefab((KitchenType)foodType);
                if (prefab != null)
                {
                    // Clear cũ nếu có
                    if (clearCounter.HasKitchenObject())
                    {
                        Destroy(clearCounter.GetKitchenObject().gameObject);
                        clearCounter.ClearKitchenObject();
                    }

                    KitchenObject instance = Instantiate(prefab, clearCounter.GetKitchenObjectToTransform());
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    clearCounter.SetKitchenObject(instance);
                }
            }
            else
            {
                // Clear Item
                if (clearCounter.HasKitchenObject())
                {
                    Destroy(clearCounter.GetKitchenObject().gameObject);
                    clearCounter.ClearKitchenObject();
                }
            }
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
            SpawnCounter(cData.counterId, cData.position, cData.rotation, cData.kitchenObjectFoodType);
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
