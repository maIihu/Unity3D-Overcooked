using System.Collections.Generic;
using Counter;
using Kitchen;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelDesignerUI : MonoBehaviour
{
    [SerializeField] private CounterTemplateListSO templateList;
    [SerializeField] private Transform paletteContainer;
    [SerializeField] private GameObject paletteButtonPrefab;

    [Header("Sub-Type Dropdown")]
    [SerializeField] private TMP_Dropdown subTypeDropdown;
    [SerializeField] private GameObject dropdownContainer;
    [SerializeField] private Button spawnSubTypeButton;
    [SerializeField] private Button closeButton;

    [Header("Level IO")]
    [SerializeField] private TMP_InputField levelNameInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button clearButton;

    private CounterTemplate _selectedTemplate;

    private void Start()
    {
        GeneratePalette();
        HideSubTypeDropdown();

        saveButton.onClick.AddListener(() => LevelDesignerManager.Instance.SaveLevel(levelNameInput.text));
        loadButton.onClick.AddListener(() => LevelDesignerManager.Instance.LoadLevel(levelNameInput.text));
        clearButton.onClick.AddListener(() => LevelDesignerManager.Instance.ClearLevel());
        closeButton.onClick.AddListener(HideSubTypeDropdown);
        
        if (spawnSubTypeButton != null)
        {
            spawnSubTypeButton.onClick.AddListener(OnSpawnSubTypeClicked);
        }
    }

    private void GeneratePalette()
    {
        foreach (Transform child in paletteContainer) Destroy(child.gameObject);

        foreach (var template in templateList.templates)
        {
            GameObject btnGO = Instantiate(paletteButtonPrefab, paletteContainer);
            btnGO.SetActive(true);
            Button btn = btnGO.GetComponent<Button>();
            TMP_Text txt = btnGO.GetComponentInChildren<TMP_Text>();

            if (txt != null) txt.text = template.counterType.ToString();

            btn.onClick.AddListener(() => OnPaletteButtonClicked(template));
        }
    }

    private void OnPaletteButtonClicked(CounterTemplate template)
    {
        _selectedTemplate = template;

        switch (template.counterType)
        {
            case CounterType.ContainerCounter:
                ShowSubTypeDropdown(typeof(FoodType));
                break;

            case CounterType.StoveCounter:
                ShowSubTypeDropdown(typeof(KitchenType));
                break;

            default:
                // No sub-type needed, spawn immediately
                HideSubTypeDropdown();
                int counterId = CounterIdConverter.ToId(template.counterType);
                LevelDesignerManager.Instance.SpawnCounter(counterId, Vector3.zero, Vector3.zero);
                break;
        }
    }

    private void ShowSubTypeDropdown(System.Type enumType)
    {
        if (dropdownContainer != null) dropdownContainer.SetActive(true);

        if (subTypeDropdown != null)
        {
            subTypeDropdown.ClearOptions();
            var names = System.Enum.GetNames(enumType);
            subTypeDropdown.AddOptions(new List<string>(names));
            subTypeDropdown.value = 0;
        }
    }

    private void HideSubTypeDropdown()
    {
        if (dropdownContainer != null) dropdownContainer.SetActive(false);
    }

    private void OnSpawnSubTypeClicked()
    {
        if (_selectedTemplate == null || subTypeDropdown == null) return;

        int index = subTypeDropdown.value;
        int counterId = CounterIdConverter.ToId(_selectedTemplate.counterType, index);
        LevelDesignerManager.Instance.SpawnCounter(counterId, Vector3.zero, Vector3.zero);
    }
}
