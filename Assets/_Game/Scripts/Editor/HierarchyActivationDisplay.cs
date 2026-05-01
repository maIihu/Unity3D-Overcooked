using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class HierarchyActivationDisplay
{
    static HierarchyActivationDisplay()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
    }

    private static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
    {
        GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go)
        {
            Rect toggleRect = new Rect(selectionRect);
            toggleRect.x -= 27f;
            toggleRect.width = 13f;
            bool active = EditorGUI.Toggle(toggleRect, go.activeSelf, EditorStyles.toggle);
            if (active != go.activeSelf)
            {
                Undo.RecordObject(go, "Toggle Active State");
                go.SetActive(active);
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(go.scene);
                }
            }
        }
    }
}
