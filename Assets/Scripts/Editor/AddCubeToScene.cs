using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public class AddCubeToScene
{
    [MenuItem("Tools/Add Cube to GameScene")]
    public static void CreateCube()
    {
        // Kiểm tra xem có đang mở GameScene không
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "GameScene")
        {
            Debug.LogWarning("Vui lòng mở GameScene trước khi tạo Cube!");
            return;
        }

        // Tạo một Cube
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "MyNewCube";
        cube.transform.position = Vector3.zero;

        // Đánh dấu Scene đã thay đổi để Unity cho phép lưu (Ctrl + S)
        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("Đã tạo Cube thành công vào GameScene!");
    }
}
