#if UNITY_EDITOR
using System.Linq;
using Counter;
using Kitchen;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools > Level Editor > Build Kitchen Object Panel
/// Creates the KitchenObjectPanel UI inside the Counter Info Panel and wires up
/// all serialized references on LevelDesignerUI automatically.
/// </summary>
public static class KitchenObjectPanelBuilder
{
    [MenuItem("Tools/Level Editor/Build Kitchen Object Panel")]
    public static void Build()
    {
        // ── 1. Find LevelDesignerUI ──────────────────────────────────────────
        var ui = Object.FindObjectOfType<LevelDesignerUI>();
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Error",
                "LevelDesignerUI not found in the active scene.\n" +
                "Please open the LevelDesigner scene first.", "OK");
            return;
        }

        var so = new SerializedObject(ui);

        // ── 2. Locate / create parent panel ─────────────────────────────────
        // We'll parent KitchenObjectPanel under the Counter Info Panel if it exists,
        // otherwise directly under the LevelDesignerUI's Canvas.
        GameObject counterInfoPanel = (so.FindProperty("counterInfoPanel").objectReferenceValue as GameObject);
        Transform panelParent = counterInfoPanel != null
            ? counterInfoPanel.transform
            : ui.GetComponentInParent<Canvas>()?.transform ?? ui.transform;

        // ── 3. Destroy old KitchenObjectPanel if it already exists ───────────
        var existing = panelParent.Find("KitchenObjectPanel");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // ── 4. Create KitchenObjectPanel root ───────────────────────────────
        var panel = CreateUIObject("KitchenObjectPanel", panelParent);
        Undo.RegisterCreatedObjectUndo(panel, "Create KitchenObjectPanel");

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot    = new Vector2(0.5f, 0);
        panelRect.offsetMin = new Vector2(0, -130);
        panelRect.offsetMax = new Vector2(0, 0);

        // Background image
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        // Vertical layout
        var vLayout = panel.AddComponent<VerticalLayoutGroup>();
        vLayout.padding     = new RectOffset(8, 8, 8, 8);
        vLayout.spacing     = 6;
        vLayout.childControlWidth  = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth  = true;
        vLayout.childForceExpandHeight = false;

        var panelFitter = panel.AddComponent<ContentSizeFitter>();
        panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── 5. Label ─────────────────────────────────────────────────────────
        var label = CreateTextObject("Label_KitchenItem", panel.transform,
            "Place Item on Counter", 13, FontStyles.Bold);
        SetHeight(label, 22);

        // ── 6. Dropdown row ──────────────────────────────────────────────────
        var dropdownRow = CreateUIObject("Row_Dropdown", panel.transform);
        var rowLayout = dropdownRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6;
        rowLayout.childControlWidth  = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth  = true;
        rowLayout.childForceExpandHeight = false;
        SetHeight(dropdownRow, 32);

        var dropdownLabel = CreateTextObject("Label_FoodType", dropdownRow.transform,
            "Food Type:", 12, FontStyles.Normal);
        var dLabelFit = dropdownLabel.AddComponent<LayoutElement>();
        dLabelFit.preferredWidth = 70;
        dLabelFit.flexibleWidth  = 0;

        var dropdownGO = CreateDropdown("Dropdown_FoodType", dropdownRow.transform);
        var dropdown = dropdownGO.GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(System.Enum.GetNames(typeof(EFoodType)).ToList());

        // ── 7. Button row ────────────────────────────────────────────────────
        var btnRow = CreateUIObject("Row_Buttons", panel.transform);
        var btnLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 6;
        btnLayout.childControlWidth  = true;
        btnLayout.childControlHeight = true;
        btnLayout.childForceExpandWidth  = true;
        btnLayout.childForceExpandHeight = false;
        SetHeight(btnRow, 32);

        var setBtn   = CreateButton("Btn_SetItem",   btnRow.transform, "Set Item",   new Color(0.18f, 0.58f, 0.22f));
        var clearBtn = CreateButton("Btn_ClearItem", btnRow.transform, "Clear Item", new Color(0.70f, 0.20f, 0.20f));

        // ── 8. Wire serialized references ────────────────────────────────────
        so.FindProperty("kitchenObjectPanel").objectReferenceValue = panel;
        so.FindProperty("foodTypeDropdown").objectReferenceValue   = dropdown;
        so.FindProperty("setItemButton").objectReferenceValue      = setBtn.GetComponent<Button>();
        so.FindProperty("clearItemButton").objectReferenceValue    = clearBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        // ── 9. Hide at start (matches code logic) ────────────────────────────
        panel.SetActive(false);

        // ── 10. Save scene ───────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        Debug.Log("[KitchenObjectPanelBuilder] KitchenObjectPanel created and wired successfully!");
        EditorUtility.DisplayDialog("Done",
            "KitchenObjectPanel created and all references wired.\n" +
            "Save the scene with Ctrl+S.", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreateTextObject(string name, Transform parent,
        string text, int fontSize, FontStyles style)
    {
        var go = CreateUIObject(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return go;
    }

    private static GameObject CreateDropdown(string name, Transform parent)
    {
        // Minimal TMP_Dropdown setup
        var go = CreateUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f);

        var dd = go.AddComponent<TMP_Dropdown>();

        // Label child
        var labelGO = CreateUIObject("Label", go.transform);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8, 2);
        labelRect.offsetMax = new Vector2(-28, -2);
        var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.fontSize = 12;
        labelTMP.color    = Color.white;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        dd.captionText = labelTMP;

        // Arrow child
        var arrowGO = CreateUIObject("Arrow", go.transform);
        var arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1, 0.5f);
        arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.pivot     = new Vector2(1, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-4, 0);
        arrowRect.sizeDelta = new Vector2(16, 16);
        var arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.color = Color.white;

        // Template (required by TMP_Dropdown)
        var templateGO = CreateUIObject("Template", go.transform);
        var templateRect = templateGO.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot     = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = Vector2.zero;
        templateRect.sizeDelta = new Vector2(0, 150);
        var templateImg = templateGO.AddComponent<Image>();
        templateImg.color = new Color(0.15f, 0.15f, 0.15f);
        var templateScroll = templateGO.AddComponent<ScrollRect>();

        var viewportGO = CreateUIObject("Viewport", templateGO.transform);
        var viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportGO.AddComponent<Image>().color = Color.clear;
        viewportGO.AddComponent<Mask>().showMaskGraphic = false;
        templateScroll.viewport = viewportRect;

        var contentGO = CreateUIObject("Content", viewportGO.transform);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot     = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 28);
        templateScroll.content = contentRect;

        // Item template
        var itemGO = CreateUIObject("Item", contentGO.transform);
        var itemToggle = itemGO.AddComponent<Toggle>();
        var itemRect = itemGO.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 26);

        var itemBg = CreateUIObject("Item Background", itemGO.transform);
        var itemBgImg = itemBg.AddComponent<Image>();
        itemBgImg.color = new Color(0.22f, 0.22f, 0.22f);
        var itemBgRect = itemBg.GetComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.offsetMin = itemBgRect.offsetMax = Vector2.zero;
        itemToggle.targetGraphic = itemBgImg;

        var itemCheckmark = CreateUIObject("Item Checkmark", itemGO.transform);
        var itemCheckImg = itemCheckmark.AddComponent<Image>();
        itemCheckImg.color = new Color(0.3f, 0.8f, 0.3f);
        var itemCheckRect = itemCheckmark.GetComponent<RectTransform>();
        itemCheckRect.anchorMin = new Vector2(0, 0.5f);
        itemCheckRect.anchorMax = new Vector2(0, 0.5f);
        itemCheckRect.sizeDelta = new Vector2(16, 16);
        itemCheckRect.anchoredPosition = new Vector2(10, 0);
        itemToggle.graphic = itemCheckImg;

        var itemLabelGO = CreateUIObject("Item Label", itemGO.transform);
        var itemLabelRect = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(24, 2);
        itemLabelRect.offsetMax = new Vector2(-4, -2);
        var itemLabelTMP = itemLabelGO.AddComponent<TextMeshProUGUI>();
        itemLabelTMP.fontSize  = 12;
        itemLabelTMP.color     = Color.white;
        itemLabelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        dd.itemText = itemLabelTMP;

        dd.template = templateRect;
        templateGO.SetActive(false);

        return go;
    }

    private static GameObject CreateButton(string name, Transform parent,
        string label, Color bgColor)
    {
        var go = CreateUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor     = bgColor * 0.8f;
        btn.colors = colors;

        var textGO  = CreateUIObject("Text", go.transform);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 12;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private static void SetHeight(GameObject go, float height)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight       = height;
    }
}
#endif
