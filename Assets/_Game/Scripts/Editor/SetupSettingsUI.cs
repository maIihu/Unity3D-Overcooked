using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using _Game.Scripts.UI;

namespace GameCore.Editor
{
    public class SetupSettingsUI
    {
        [MenuItem("Overcooked/Setup Settings UI")]
        public static void DoSetup()
        {
            string prefabPath = "Assets/_Game/Prefabs/SavePrefab/UIManager.prefab";
            GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (rootPrefab == null)
            {
                Debug.LogError($"[SetupSettingsUI] UIManager prefab not found at: {prefabPath}");
                return;
            }

            // Open prefab stage or instantiate it to modify
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(rootPrefab) as GameObject;
            if (prefabInstance == null)
            {
                Debug.LogError("[SetupSettingsUI] Failed to instantiate UIManager prefab for modification.");
                return;
            }

            UIManager uiManager = prefabInstance.GetComponent<UIManager>();
            Transform popupHolder = prefabInstance.transform.Find("Canvas/PopupHolder");
            if (popupHolder == null)
            {
                Debug.LogError("[SetupSettingsUI] Canvas/PopupHolder not found in UIManager prefab!");
                Object.DestroyImmediate(prefabInstance);
                return;
            }

            // Clean up old settings popup if it exists
            Transform oldSettingsPopup = popupHolder.Find("PopupSettings");
            if (oldSettingsPopup != null)
            {
                Object.DestroyImmediate(oldSettingsPopup.gameObject);
            }

            // Create settings popup GameObject
            GameObject settingsPopupObj = new GameObject("PopupSettings", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PopupSettings));
            settingsPopupObj.transform.SetParent(popupHolder, false);
            settingsPopupObj.SetActive(false);

            RectTransform rectTrans = settingsPopupObj.GetComponent<RectTransform>();
            rectTrans.anchorMin = Vector2.zero;
            rectTrans.anchorMax = Vector2.one;
            rectTrans.sizeDelta = Vector2.zero;

            // Semi-transparent background
            Image bgImg = settingsPopupObj.GetComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.75f);

            // Create mainPopUp panel
            GameObject mainPanelObj = new GameObject("MainPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            mainPanelObj.transform.SetParent(settingsPopupObj.transform, false);
            RectTransform mainRT = mainPanelObj.GetComponent<RectTransform>();
            mainRT.anchorMin = new Vector2(0.5f, 0.5f);
            mainRT.anchorMax = new Vector2(0.5f, 0.5f);
            mainRT.pivot = new Vector2(0.5f, 0.5f);
            mainRT.sizeDelta = new Vector2(450, 400);

            // Stylize panel
            Image panelImg = mainPanelObj.GetComponent<Image>();
            panelImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            // Title "SETTINGS"
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(mainPanelObj.transform, false);
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -30);
            titleRT.sizeDelta = new Vector2(400, 60);

            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.text = "SETTINGS";
            titleText.fontSize = 40;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;

            // Template button creation helper
            Button CreateButton(string name, string labelText, float posY, Color normalColor)
            {
                GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(mainPanelObj.transform, false);
                RectTransform btnRT = btnObj.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.5f, 0.5f);
                btnRT.anchorMax = new Vector2(0.5f, 0.5f);
                btnRT.pivot = new Vector2(0.5f, 0.5f);
                btnRT.anchoredPosition = new Vector2(0, posY);
                btnRT.sizeDelta = new Vector2(300, 65);

                Image btnImg = btnObj.GetComponent<Image>();
                btnImg.color = normalColor;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(btnObj.transform, false);
                RectTransform txtRT = textObj.GetComponent<RectTransform>();
                txtRT.anchorMin = Vector2.zero;
                txtRT.anchorMax = Vector2.one;
                txtRT.sizeDelta = Vector2.zero;

                TextMeshProUGUI t = textObj.GetComponent<TextMeshProUGUI>();
                t.text = labelText;
                t.fontSize = 28;
                t.fontStyle = FontStyles.Bold;
                t.color = Color.white;
                t.alignment = TextAlignmentOptions.Center;

                Button btn = btnObj.GetComponent<Button>();
                btn.targetGraphic = btnImg;
                return btn;
            }

            // Resume Button
            Button resumeBtn = CreateButton("Button_Resume", "RESUME", 30, new Color(0.2f, 0.6f, 0.2f, 1f));

            // Main Menu Button
            Button mainMenuBtn = CreateButton("Button_MainMenu", "MAIN MENU", -60, new Color(0.7f, 0.2f, 0.2f, 1f));

            // Setup PopupSettings fields
            PopupSettings settingsPopup = settingsPopupObj.GetComponent<PopupSettings>();
            SerializedObject popupSO = new SerializedObject(settingsPopup);
            popupSO.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            popupSO.FindProperty("mainMenuButton").objectReferenceValue = mainMenuBtn;
            popupSO.FindProperty("mainPopUp").objectReferenceValue = mainRT;
            popupSO.FindProperty("animType").enumValueIndex = 2; // ScalePunch (2)
            popupSO.FindProperty("isCache").boolValue = true;
            popupSO.ApplyModifiedProperties();

            // Link in UIManager listPopup
            SerializedObject uiSO = new SerializedObject(uiManager);
            SerializedProperty listPopupProp = uiSO.FindProperty("listPopup");
            
            // Check if already in array to avoid duplicate
            bool found = false;
            for (int i = 0; i < listPopupProp.arraySize; i++)
            {
                var val = listPopupProp.GetArrayElementAtIndex(i).objectReferenceValue;
                if (val != null && val is PopupSettings)
                {
                    listPopupProp.GetArrayElementAtIndex(i).objectReferenceValue = settingsPopup;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                listPopupProp.InsertArrayElementAtIndex(listPopupProp.arraySize);
                listPopupProp.GetArrayElementAtIndex(listPopupProp.arraySize - 1).objectReferenceValue = settingsPopup;
            }
            uiSO.ApplyModifiedProperties();

            // Save changes to prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            Object.DestroyImmediate(prefabInstance);

            Debug.Log($"🎉 [SetupSettingsUI] Successfully created Settings Popup inside {prefabPath} with buttons RESUME and MAIN MENU!");
        }
    }
}
