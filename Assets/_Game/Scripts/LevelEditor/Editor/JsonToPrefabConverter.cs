#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Counter;
using Kitchen;

/// <summary>
/// Editor tool to convert existing JSON level files into Prefab format.
/// Menu: Tools > Level Designer > Convert JSON to Prefab
/// </summary>
public class JsonToPrefabConverter : EditorWindow
{
    // Legacy data structures for parsing old JSON files
    [System.Serializable]
    public class OldCounterData
    {
        public int counterId;
        public Vector3 position;
        public Vector3 rotation;
        public int kitchenObjectFoodType = -1;
    }

    [System.Serializable]
    public class OldLevelData
    {
        public List<OldCounterData> counterList = new List<OldCounterData>();
        public Vector3 cameraPosition;
        public Vector3 cameraEulerAngles;
    }

    private static class OldCounterIdConverter
    {
        private const int Multiplier = 100;
        public static CounterType GetCounterType(int counterId) => (CounterType)(counterId / Multiplier);
        public static int GetSubType(int counterId) => counterId % Multiplier;
        public static EFoodType GetFoodType(int counterId) => (EFoodType)GetSubType(counterId);
        public static KitchenType GetKitchenType(int counterId) => (KitchenType)GetSubType(counterId);
    }

    private CounterTemplateListSO templateList;
    private KitchenObjectLibrarySO kitchenObjectLibrary;
    private Vector2 scrollPos;
    private string[] jsonFiles;

    [MenuItem("Tools/Level Designer/Convert JSON to Prefab")]
    public static void ShowWindow()
    {
        var window = GetWindow<JsonToPrefabConverter>("JSON → Prefab Converter");
        window.minSize = new Vector2(400, 300);
        window.RefreshJsonList();
    }

    [MenuItem("Tools/Level Designer/Upgrade All Levels")]
    public static void UpgradeAllCommandLine()
    {
        var converter = CreateInstance<JsonToPrefabConverter>();
        converter.templateList = AssetDatabase.LoadAssetAtPath<CounterTemplateListSO>("Assets/Resources/LevelDesigner/CounterTemplateList.asset");
        converter.kitchenObjectLibrary = AssetDatabase.LoadAssetAtPath<KitchenObjectLibrarySO>("Assets/Resources/LevelDesigner/KitchenObjectLibrarySO.asset");
        converter.RefreshJsonList();
        
        if (converter.jsonFiles == null || converter.jsonFiles.Length == 0)
        {
            Debug.LogWarning("[Converter] No JSON files found to upgrade.");
            return;
        }

        int successCount = 0;
        foreach (string filePath in converter.jsonFiles)
        {
            if (converter.ConvertOrUpgradeSingle(filePath))
                successCount++;
        }

        Debug.Log($"[Converter] Batch Processed {successCount}/{converter.jsonFiles.Length} levels.");
    }

    private void OnEnable()
    {
        RefreshJsonList();
    }

    private void RefreshJsonList()
    {
        string folder = Path.Combine(Application.dataPath, "Resources", "Levels");
        if (Directory.Exists(folder))
            jsonFiles = Directory.GetFiles(folder, "*.json");
        else
            jsonFiles = new string[0];
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("JSON → Prefab Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        templateList = (CounterTemplateListSO)EditorGUILayout.ObjectField(
            "Counter Template List", templateList, typeof(CounterTemplateListSO), false);

        kitchenObjectLibrary = (KitchenObjectLibrarySO)EditorGUILayout.ObjectField(
            "Kitchen Object Library", kitchenObjectLibrary, typeof(KitchenObjectLibrarySO), false);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Refresh File List"))
            RefreshJsonList();

        EditorGUILayout.Space(5);

        if (jsonFiles == null || jsonFiles.Length == 0)
        {
            EditorGUILayout.HelpBox("No JSON files found in Assets/Resources/Levels/", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Found {jsonFiles.Length} JSON file(s):", EditorStyles.miniLabel);
        EditorGUILayout.Space(3);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (string filePath in jsonFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fileName, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Convert/Upgrade", GUILayout.Width(120)))
            {
                ConvertOrUpgradeSingle(filePath);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Convert & Upgrade All", GUILayout.Height(30)))
        {
            ConvertAll();
        }
        GUI.backgroundColor = Color.white;
    }

    private void ConvertAll()
    {
        if (!ValidateReferences()) return;

        int successCount = 0;
        foreach (string filePath in jsonFiles)
        {
            if (ConvertOrUpgradeSingle(filePath))
                successCount++;
        }

        EditorUtility.DisplayDialog("Done",
            $"Processed {successCount}/{jsonFiles.Length} levels.", "OK");

        RefreshJsonList();
    }

    private bool ConvertOrUpgradeSingle(string jsonFilePath)
    {
        if (!ValidateReferences()) return false;

        string json = File.ReadAllText(jsonFilePath);
        OldLevelData data = JsonUtility.FromJson<OldLevelData>(json);

        if (data == null)
        {
            Debug.LogError($"[Converter] Failed to parse JSON: {jsonFilePath}");
            return false;
        }

        string fileName = Path.GetFileNameWithoutExtension(jsonFilePath);
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogWarning($"[Converter] Skipping invalid file path: {jsonFilePath}");
            return false;
        }
        string prefabFolder = "Assets/Resources/Levels";
        string prefabPath = $"{prefabFolder}/{fileName}.prefab";

        GameObject root = null;
        bool isUpgrade = File.Exists(prefabPath);

        if (isUpgrade)
        {
            // Load existing prefab to upgrade its values rather than destroying it
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(existingPrefab);
                Debug.Log($"[Converter] Upgrading existing prefab: {prefabPath}");
            }
        }

        if (root == null)
        {
            // Create brand new root GO
            root = new GameObject(fileName);
            isUpgrade = false;
        }

        // Configure camera
        LevelPrefabData prefabData = root.GetComponent<LevelPrefabData>();
        if (prefabData == null) prefabData = root.AddComponent<LevelPrefabData>();
        prefabData.cameraPosition = data.cameraPosition;
        prefabData.cameraEulerAngles = data.cameraEulerAngles;

        // If not upgrading, spawn new counters as children
        if (!isUpgrade)
        {
            foreach (var cData in data.counterList)
            {
                CounterType counterType = OldCounterIdConverter.GetCounterType(cData.counterId);
                CounterTemplate template = templateList.GetTemplateByType(counterType);

                if (template == null || template.prefab == null) continue;

                GameObject counterGO = (GameObject)PrefabUtility.InstantiatePrefab(template.prefab, root.transform);
                counterGO.transform.position = cData.position;
                counterGO.transform.eulerAngles = cData.rotation;

                BaseCounter counter = counterGO.GetComponent<BaseCounter>();
                if (counter != null)
                {
                    if (counter is ContainerCounter container)
                        container.SetContainer(OldCounterIdConverter.GetFoodType(cData.counterId));
                    else if (counter is StoveCounter stove)
                        stove.SetStoveData(OldCounterIdConverter.GetKitchenType(cData.counterId));
                }

                // Restore kitchen object
                if (cData.kitchenObjectFoodType >= 0 && counter is ClearCounter clearCounter && kitchenObjectLibrary != null)
                {
                    KitchenObject koPrefab = kitchenObjectLibrary.GetPrefab((KitchenType)cData.kitchenObjectFoodType);
                    if (koPrefab != null)
                    {
                        KitchenObject koInstance = (KitchenObject)PrefabUtility.InstantiatePrefab(koPrefab, clearCounter.GetKitchenObjectToTransform());
                        koInstance.transform.localPosition = Vector3.zero;
                        koInstance.transform.localRotation = Quaternion.identity;
                        clearCounter.SetKitchenObject(koInstance);
                    }
                }
            }
        }
        else
        {
            // Upgrading: Find existing children and match them to the old JSON configurations to configure them
            BaseCounter[] childCounters = root.GetComponentsInChildren<BaseCounter>(true);
            foreach (var child in childCounters)
            {
                // Find matching JSON record based on position (approximate match)
                OldCounterData match = data.counterList.Find(c => Vector3.Distance(c.position, child.transform.position) < 0.1f);
                if (match != null)
                {
                    if (child is ContainerCounter container)
                        container.SetContainer(OldCounterIdConverter.GetFoodType(match.counterId));
                    else if (child is StoveCounter stove)
                        stove.SetStoveData(OldCounterIdConverter.GetKitchenType(match.counterId));

                    // Check kitchen object
                    if (match.kitchenObjectFoodType >= 0 && child is ClearCounter clearCounter && kitchenObjectLibrary != null)
                    {
                        // Only instantiate if it does not already have a kitchen object child
                        if (!clearCounter.HasKitchenObject() && clearCounter.GetComponentInChildren<KitchenObject>(true) == null)
                        {
                            KitchenObject koPrefab = kitchenObjectLibrary.GetPrefab((KitchenType)match.kitchenObjectFoodType);
                            if (koPrefab != null)
                            {
                                KitchenObject koInstance = (KitchenObject)PrefabUtility.InstantiatePrefab(koPrefab, clearCounter.GetKitchenObjectToTransform());
                                koInstance.transform.localPosition = Vector3.zero;
                                koInstance.transform.localRotation = Quaternion.identity;
                                clearCounter.SetKitchenObject(koInstance);
                            }
                        }
                    }
                }
            }
        }

        // Populate lists on the LevelPrefabData root
        prefabData.baseCounters.Clear();
        prefabData.baseCounters.AddRange(root.GetComponentsInChildren<BaseCounter>(true));

        prefabData.kitchenObjects.Clear();
        prefabData.kitchenObjects.AddRange(root.GetComponentsInChildren<KitchenObject>(true));

        // Save prefab
        if (!Directory.Exists(prefabFolder))
            Directory.CreateDirectory(prefabFolder);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
        DestroyImmediate(root);

        if (success)
        {
            Debug.Log($"[Converter] ✓ {fileName} saved & updated → {prefabPath}");
            return true;
        }
        else
        {
            Debug.LogError($"[Converter] ✗ Failed to save prefab: {prefabPath}");
            return false;
        }
    }

    private bool ValidateReferences()
    {
        if (templateList == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign Counter Template List!", "OK");
            return false;
        }
        return true;
    }
}
#endif
