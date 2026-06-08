using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
public class ImageCropMiddleTool : EditorWindow
{
    private List<Texture2D> sourceTextures = new List<Texture2D>();
    static int remain=2;

    [MenuItem("Tools/Image Crop Middle Tool (Batch Raw Data -> Sprite)")]
    private static void OpenWindow()
    {
        GetWindow<ImageCropMiddleTool>("Image Crop Middle Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Crop Middle + Adjust Sprite Border (Drag & Drop, Raw Data -> Sprite)", EditorStyles.boldLabel);

        // Drag & Drop zone
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 100.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag & Drop Textures Here", EditorStyles.helpBox);

        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object dragged in DragAndDrop.objectReferences)
                    {
                        Texture2D tex = dragged as Texture2D;
                        if (tex != null && !sourceTextures.Contains(tex))
                            sourceTextures.Add(tex);
                    }
                }
                Event.current.Use();
            }
        }

        if (sourceTextures.Count > 0)
        {
            GUILayout.Label("Queued Textures:", EditorStyles.boldLabel);
            foreach (var tex in sourceTextures)
                GUILayout.Label(tex.name, EditorStyles.miniLabel);

            if (GUILayout.Button("Crop Both"))
                ProcessTextures(true,true);
            if (GUILayout.Button("Crop Width"))
                ProcessTextures(false,true);
            if (GUILayout.Button("Crop Height"))
                ProcessTextures(true,false);
            if (GUILayout.Button("Fix Size"))
                FixSizeTextures();
        }
    }

    private void ProcessTextures(bool isCropHeight = true,bool isCropWidth = true)
    {
        int index = 0;
        foreach (var tex in sourceTextures)
        {
            index++;
            string path = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Could not find asset path of texture: " + tex.name);
                continue;
            }

            Texture2D rawTex = LoadRawTexture(path);
            if (rawTex == null)
            {
                Debug.LogError("Failed to load raw texture: " + tex.name);
                continue;
            }
            Texture2D cropped = null;
            int rowStart = 0; int rowCount = 0; int colStart = 0; int colCount = 0;
            (cropped, rowStart, rowCount, colStart, colCount) = CropTexture(rawTex, isCropHeight, isCropWidth);
            if (cropped != null)
            {
                cropped = PadToMultipleOf4Raw(cropped);
                byte[] pngData = cropped.EncodeToPNG();
                File.WriteAllBytes(path, pngData);

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                // ép về Sprite type
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;

                    // adjust border
                    AdjustSpriteBorder(importer, rowStart, rowCount, colStart, colCount,isCropHeight,isCropWidth, cropped);

                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }

                Debug.Log($"[{index}/{sourceTextures.Count}] Cropped + converted to Sprite: {tex.name}");
            }
        }

        sourceTextures.Clear(); // reset list
        EditorUtility.ClearProgressBar();
    }
    private void FixSizeTextures()
    {
        int index = 0;
        foreach (var tex in sourceTextures)
        {
            index++;
            string path = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Could not find asset path of texture: " + tex.name);
                continue;
            }

            Texture2D rawTex = LoadRawTexture(path);
            if (rawTex == null)
            {
                Debug.LogError("Failed to load raw texture: " + tex.name);
                continue;
            }
            var cropped = rawTex;
            if (cropped != null)
            {
                cropped = PadToMultipleOf4Raw(cropped);
                byte[] pngData = cropped.EncodeToPNG();
                File.WriteAllBytes(path, pngData);

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[Fix Size x4 + converted to Sprite: {tex.name}");
            }
        }

        sourceTextures.Clear(); // reset list
        EditorUtility.ClearProgressBar();
    }
    private Texture2D LoadRawTexture(string path)
    {
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        if (ImageConversion.LoadImage(tex, fileData, false))
            return tex;
        return null;
    }

    private (Texture2D, int, int, int, int) CropTexture(Texture2D tex,bool cropHeight,bool cropWidth)
    {
        Color32[] pixels = tex.GetPixels32();
        int width = tex.width;
        int height = tex.height;
        int rowStart=0;
        int rowCount=0;
        int colStart=0;
        int colCount=0;
        if (cropHeight)
        {
            (rowStart, rowCount) = FindMaxRepeatedRows(pixels, width, height);
            if (rowCount>= remain) rowCount -= remain;
        }
        if (cropWidth)
        {
            (colStart, colCount) = FindMaxRepeatedCols(pixels, width, height);
            if (colCount >= remain) colCount -= remain;
        }
        int newWidth = width - colCount;
        int newHeight = height - rowCount;

        if (newWidth <= 0 || newHeight <= 0)
        {
            Debug.LogError("Invalid crop result on: " + tex.name);
            return (null, 0, 0, 0, 0);
        }

        Color32[] newPixels = new Color32[newWidth * newHeight];

        for (int y = 0, newY = 0; y < height; y++)
        {
            if (y >= rowStart && y < rowStart + rowCount) continue;

            int newX = 0;
            for (int x = 0; x < width; x++)
            {
                if (x >= colStart && x < colStart + colCount) continue;

                newPixels[newY * newWidth + newX] = pixels[y * width + x];
                newX++;
            }
            newY++;
        }

        Texture2D newTex = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false, true);
        newTex.SetPixels32(newPixels);
        newTex.Apply();
        return (newTex, rowStart, rowCount, colStart, colCount);
    }

    private void AdjustSpriteBorder(TextureImporter importer, int rowStart, int rowCount, int colStart, int colCount,bool isCropHeight,bool isCropWidth, Texture2D texture)
    {
        Vector4 border = importer.spriteBorder;
        if (rowStart == 0)
        {
            rowStart = texture.height / 2;
        }
        if(colStart == 0)
        {
            colStart = texture.height / 2;
        }

        if (isCropWidth)
        {
            border.x = Mathf.Max(colStart , 0);
            border.z = Mathf.Min(texture.width - colStart - remain, texture.width);
        }
        else
        {
            border.x = 0;
            border.z = texture.width;
        }
        if (isCropHeight)
        {
            border.y = Mathf.Max(rowStart, 0);
            border.w = Mathf.Min(texture.height - rowStart - remain, texture.height);
        }
        else
        {
            border.y = 0;
            border.w= texture.height;
        }
        importer.spriteBorder = border;
    }

    private (int, int) FindMaxRepeatedRows(Color32[] pixels, int width, int height)
    {
        int bestCount = 0, bestStart = 0, currentCount = 1;

        for (int y = 1; y < height; y++)
        {
            // Bỏ qua nếu hàng này hoặc hàng trước toàn trong suốt
            if (IsRowTransparent(pixels, width, y) || IsRowTransparent(pixels, width, y - 1))
            {
                currentCount = 1;
                continue;
            }

            if (CompareRow(pixels, width, y, y - 1))
            {
                currentCount++;
                if (currentCount > bestCount)
                {
                    bestCount = currentCount;
                    bestStart = y - currentCount + 1;
                }
            }
            else currentCount = 1;
        }
        return (bestStart, bestCount);
    }

    private (int, int) FindMaxRepeatedCols(Color32[] pixels, int width, int height)
    {
        int bestCount = 0, bestStart = 0, currentCount = 1;

        for (int x = 1; x < width; x++)
        {
            // Bỏ qua nếu cột này hoặc cột trước toàn trong suốt
            if (IsColTransparent(pixels, width, height, x) || IsColTransparent(pixels, width, height, x - 1))
            {
                currentCount = 1;
                continue;
            }

            if (CompareCol(pixels, width, height, x, x - 1))
            {
                currentCount++;
                if (currentCount > bestCount)
                {
                    bestCount = currentCount;
                    bestStart = x - currentCount + 1;
                }
            }
            else currentCount = 1;
        }
        return (bestStart, bestCount);
    }

    /// <summary>
    /// Check if a row is fully transparent (all pixels alpha = 0)
    /// </summary>
    private bool IsRowTransparent(Color32[] pixels, int width, int y)
    {
        int offset = y * width;
        for (int x = 0; x < width; x++)
        {
            if (pixels[offset + x].a > 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Check if a column is fully transparent (all pixels alpha = 0)
    /// </summary>
    private bool IsColTransparent(Color32[] pixels, int width, int height, int x)
    {
        for (int y = 0; y < height; y++)
        {
            if (pixels[y * width + x].a > 0)
                return false;
        }
        return true;
    }

    private bool CompareRow(Color32[] pixels, int width, int y1, int y2)
    {
        for (int x = 0; x < width; x++)
            if (!pixels[y1 * width + x].Equals(pixels[y2 * width + x])) return false;
        return true;
    }

    private bool CompareCol(Color32[] pixels, int width, int height, int x1, int x2)
    {
        for (int y = 0; y < height; y++)
            if (!pixels[y * width + x1].Equals(pixels[y * width + x2])) return false;
        return true;
    }

    public static Texture2D PadToMultipleOf4Raw(Texture2D source)
    {
        int width = source.width;
        int height = source.height;

        int finalWidth = Mathf.CeilToInt(width / 4f) * 4;
        int finalHeight = Mathf.CeilToInt(height / 4f) * 4;

        // Already divisible by 4
        if (finalWidth == width && finalHeight == height)
            return source;

        int padX = finalWidth - width;
        int padY = finalHeight - height;

        int padLeft = padX / 2;
        int padRight = padX - padLeft;   // nếu lẻ thì dư sang bên phải
        int padBottom = padY - padY / 2; // nếu lẻ thì dư sang dưới
        int padTop = padY / 2;

        // Create output texture
        Texture2D result = new Texture2D(finalWidth, finalHeight, TextureFormat.RGBA32, false, true);

        // Prepare output buffer filled with transparent
        Color32[] outPixels = new Color32[finalWidth * finalHeight];
        for (int i = 0; i < outPixels.Length; i++) outPixels[i] = new Color32(0, 0, 0, 0);

        // Copy source pixels into correct offset
        Color32[] srcPixels = source.GetPixels32();
        for (int y = 0; y < height; y++)
        {
            int destY = y + padBottom;
            int srcRow = y * width;
            int destRow = destY * finalWidth + padLeft;

            for (int x = 0; x < width; x++)
            {
                outPixels[destRow + x] = srcPixels[srcRow + x];
            }
        }

        result.SetPixels32(outPixels);
        result.Apply();
        return result;
    }
}
#endif