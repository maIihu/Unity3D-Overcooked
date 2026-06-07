using UnityEngine;
using UnityEditor;

public class ApplyToonShader
{
    [MenuItem("Tools/Apply Toon Shader to Player")]
    public static void ApplyShader()
    {
        // Danh sách các vật liệu của nhân vật
        string[] matPaths = new string[]
        {
            "Assets/_Assets/Materials/PlayerBody.mat",
            "Assets/_Assets/Materials/PlayerBody_Blue.mat",
            "Assets/_Assets/Materials/PlayerBody_Green.mat",
            "Assets/_Assets/Materials/PlayerBody_Red.mat",
            "Assets/_Assets/Materials/Player/PlayerUniqueColorMat.mat"
        };

        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null)
        {
            Debug.LogError("Không tìm thấy Shader 'Universal Render Pipeline/Lit'.");
            return;
        }

        foreach (var path in matPaths)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                Color oldColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                
                mat.shader = urpLitShader;
                
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", oldColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", oldColor);

                EditorUtility.SetDirty(mat);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("[Thành Công] Đã khôi phục Shader URP Lit cho toàn bộ hệ thống Player!");
    }
}
