using System.Collections.Generic;
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

    private void Awake()
    {
        Instance = this;
        if (counterParent == null) counterParent = new GameObject("PlacedCounters").transform;
    }

    public static CounterType GetCounterType(BaseCounter counter)
    {
        if (counter is ContainerCounter) return CounterType.ContainerCounter;
        if (counter is StoveCounter) return CounterType.StoveCounter;
        if (counter is CuttingCounter) return CounterType.CuttingCounter;
        if (counter is TrashCounter) return CounterType.TrashCounter;
        if (counter is PlatesCounter) return CounterType.PlatesCounter;
        if (counter is DeliveryCounter) return CounterType.DeliveryCounter;
        if (counter is ClearCounter) return CounterType.ClearCounter;
        return CounterType.ClearCounter;
    }

    public void SpawnCounter(CounterType counterType, Vector3 position, Vector3 rotation, int subType = 0)
    {
        CounterTemplate template = templateList.GetTemplateByType(counterType);

        if (template == null)
        {
            Debug.LogError($"No template found for CounterType {counterType}!");
            return;
        }

        GameObject go = Instantiate(template.prefab, position, Quaternion.Euler(rotation), counterParent);
        BaseCounter counter = go.GetComponent<BaseCounter>();

        if (counter != null)
        {
            if (counter is ContainerCounter container)
            {
                container.SetContainer((EFoodType)subType);
            }
            else if (counter is StoveCounter stove)
            {
                stove.SetStoveData((KitchenType)subType);
            }
        }
    }

    public void RemoveCounter(BaseCounter counter)
    {
        if (counter != null)
        {
            DestroyImmediate(counter.gameObject);
        }
    }

    public void SetKitchenObjectOnCounter(BaseCounter counter, int foodType)
    {
        if (counter is ClearCounter clearCounter)
        {
            // Clear existing if any
            if (clearCounter.HasKitchenObject())
            {
                DestroyImmediate(clearCounter.GetKitchenObject().gameObject);
                clearCounter.ClearKitchenObject();
            }

            if (foodType >= 0)
            {
                if (kitchenObjectLibrary == null)
                {
                    Debug.LogWarning("[LevelDesignerManager] KitchenObjectLibrarySO is not assigned!");
                    return;
                }

                KitchenObject prefab = kitchenObjectLibrary.GetPrefab((KitchenType)foodType);
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

    public void SaveLevel(string levelName)
    {
#if UNITY_EDITOR
        // Attach/Get LevelPrefabData component to root
        LevelPrefabData prefabData = counterParent.GetComponent<LevelPrefabData>();
        if (prefabData == null) prefabData = counterParent.gameObject.AddComponent<LevelPrefabData>();

        // Set camera
        if (levelPreviewCamera != null)
        {
            prefabData.cameraPosition = levelPreviewCamera.transform.position;
            prefabData.cameraEulerAngles = levelPreviewCamera.transform.eulerAngles;
        }

        // Fill lists directly from active child GameObjects
        prefabData.baseCounters.Clear();
        prefabData.baseCounters.AddRange(counterParent.GetComponentsInChildren<BaseCounter>(true));

        prefabData.kitchenObjects.Clear();
        prefabData.kitchenObjects.AddRange(counterParent.GetComponentsInChildren<KitchenObject>(true));

        // Rename root
        counterParent.name = "Level_" + levelName;

        // Save as Prefab
        string folderPath = "Assets/Resources/Levels";
        if (!System.IO.Directory.Exists(folderPath))
            System.IO.Directory.CreateDirectory(folderPath);

        string prefabPath = folderPath + "/Level_" + levelName + ".prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(
            counterParent.gameObject, prefabPath,
            UnityEditor.InteractionMode.UserAction, out bool success);

        if (success)
            Debug.Log($"[LevelDesigner] Prefab saved: {prefabPath}");
        else
            Debug.LogError($"[LevelDesigner] Failed to save prefab: {prefabPath}");

        UnityEditor.AssetDatabase.Refresh();
#else
        Debug.LogWarning("[LevelDesigner] SaveLevel is only available in the Unity Editor.");
#endif
    }

    public void LoadLevel(string levelName)
    {
#if UNITY_EDITOR
        ClearLevel();

        string prefabPath = "Assets/Resources/Levels/Level_" + levelName + ".prefab";
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[LevelDesigner] Prefab not found: {prefabPath}");
            return;
        }

        // Instantiate prefab
        GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        counterParent = instance.transform;

        // Read camera settings from LevelPrefabData
        LevelPrefabData prefabData = instance.GetComponent<LevelPrefabData>();
        if (prefabData != null)
        {
            if (levelPreviewCamera != null && prefabData.cameraPosition != Vector3.zero)
            {
                levelPreviewCamera.transform.position = prefabData.cameraPosition;
                levelPreviewCamera.transform.eulerAngles = prefabData.cameraEulerAngles;
            }
        }

        Debug.Log($"[LevelDesigner] Loaded prefab: Level_{levelName}");
#else
        Debug.LogWarning("[LevelDesigner] LoadLevel is only available in the Unity Editor.");
#endif
    }

    public void ClearLevel()
    {
        if (counterParent != null)
        {
            DestroyImmediate(counterParent.gameObject);
        }
        counterParent = new GameObject("PlacedCounters").transform;
    }
}
