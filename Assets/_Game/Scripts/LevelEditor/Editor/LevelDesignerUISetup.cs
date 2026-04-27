using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelDesignerUISetup : EditorWindow
{
    [MenuItem("Tools/Overcooked/Auto-Setup Level Designer UI")]
    public static void SetupUI()
    {
        LevelDesignerUI uiScript = FindObjectOfType<LevelDesignerUI>();

        if (uiScript == null)
        {
            Debug.LogError("Could not find LevelDesignerUI in the scene. Please open the LevelDesigner scene first.");
            return;
        }

        // 1. Create a Panel (Container)
        EditorApplication.ExecuteMenuItem("GameObject/UI/Panel");
        GameObject containerGO = Selection.activeGameObject;
        if (containerGO == null) return;
        
        containerGO.name = "SubTypeDropdownContainer";
        containerGO.transform.SetParent(uiScript.transform, false);
        
        RectTransform rect = containerGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(300, 150);

        // 2. Create TMP Dropdown
        EditorApplication.ExecuteMenuItem("GameObject/UI/Dropdown - TextMeshPro");
        GameObject dropdownGO = Selection.activeGameObject;
        dropdownGO.name = "SubTypeDropdown";
        dropdownGO.transform.SetParent(containerGO.transform, false);
        
        RectTransform dropRect = dropdownGO.GetComponent<RectTransform>();
        dropRect.anchoredPosition = new Vector2(0, 20);
        dropRect.sizeDelta = new Vector2(200, 40);
        
        TMP_Dropdown dropdown = dropdownGO.GetComponent<TMP_Dropdown>();

        // 3. Create TMP Button
        EditorApplication.ExecuteMenuItem("GameObject/UI/Button - TextMeshPro");
        GameObject buttonGO = Selection.activeGameObject;
        buttonGO.name = "SpawnSubTypeButton";
        buttonGO.transform.SetParent(containerGO.transform, false);
        
        RectTransform btnRect = buttonGO.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -30);
        btnRect.sizeDelta = new Vector2(100, 40);

        TMP_Text btnText = buttonGO.GetComponentInChildren<TMP_Text>();
        if (btnText != null) btnText.text = "Spawn";

        Button spawnBtn = buttonGO.GetComponent<Button>();

        // 4. Assign fields
        SerializedObject so = new SerializedObject(uiScript);
        so.FindProperty("dropdownContainer").objectReferenceValue = containerGO;
        so.FindProperty("subTypeDropdown").objectReferenceValue = dropdown;
        so.FindProperty("spawnSubTypeButton").objectReferenceValue = spawnBtn;
        so.ApplyModifiedProperties();

        // 5. Hide container initially
        containerGO.SetActive(false);

        Debug.Log("Successfully created and assigned SubType Dropdown UI!");
    }
}
