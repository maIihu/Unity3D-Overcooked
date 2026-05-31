using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ScreenshotToolWindow : EditorWindow
{
    private int width = 1920;
    private int height = 1080;
    private string folderName = "Screenshots";

    [MenuItem("Tools/Screenshot Tool")]
    public static void ShowWindow()
    {
        GetWindow<ScreenshotToolWindow>("Screenshot Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Screenshot Settings", EditorStyles.boldLabel);

        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        folderName = EditorGUILayout.TextField("Folder", folderName);

        GUILayout.Space(10);

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("Capture Screenshot", GUILayout.Height(40)))
        {
            Capture();
        }

        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode before capturing.",
                MessageType.Info);
        }
    }

    private void Capture()
    {
        string folderPath = Path.Combine(
            Directory.GetParent(Application.dataPath)!.FullName,
            folderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName =
            $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        string fullPath = Path.Combine(folderPath, fileName);

        ScreenCapture.CaptureScreenshot(fullPath);

        Debug.Log($"Saved Screenshot: {fullPath}");
        AssetDatabase.Refresh();
    }
}