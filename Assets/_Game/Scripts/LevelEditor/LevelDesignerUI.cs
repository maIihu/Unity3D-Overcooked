using System.Collections.Generic;
using System.Linq;
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

    [Header("Counter Info Panel")]
    [SerializeField] private GameObject counterInfoPanel;
    [SerializeField] private TMP_Text counterInfoText;
    [SerializeField] private Button deleteCounterButton;

    private CounterTemplate _selectedTemplate;
    private ObjectPlacementController _placementController;

    private void Start()
    {
        GeneratePalette();
        HideSubTypeDropdown();
        if (counterInfoPanel != null) counterInfoPanel.SetActive(false);

        _placementController = FindObjectOfType<ObjectPlacementController>();
        if (_placementController != null)
        {
            _placementController.OnCounterSelected += HandleCounterSelected;
            _placementController.OnCounterDeselected += HandleCounterDeselected;
        }

        saveButton.onClick.AddListener(() => LevelDesignerManager.Instance.SaveLevel(levelNameInput.text));
        loadButton.onClick.AddListener(() => LevelDesignerManager.Instance.LoadLevel(levelNameInput.text));
        clearButton.onClick.AddListener(() => LevelDesignerManager.Instance.ClearLevel());
        closeButton.onClick.AddListener(HideSubTypeDropdown);
        
        if (spawnSubTypeButton != null)
        {
            spawnSubTypeButton.onClick.AddListener(OnSpawnSubTypeClicked);
        }

        if (deleteCounterButton != null)
        {
            deleteCounterButton.onClick.AddListener(() => {
                if (_placementController != null) _placementController.DeleteCurrentSelection();
            });
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

            btn.onClick.AddListener(() =>
                {
                    OnPaletteButtonClicked(template);
                });
        }
    }

    private void OnPaletteButtonClicked(CounterTemplate template)
    {
        _selectedTemplate = template;

        switch (template.counterType)
        {
            case CounterType.ContainerCounter:
                ShowSubTypeDropdown(typeof(EFoodType));
                break;
            case CounterType.StoveCounter:
                ShowSubTypeDropdown(new KitchenType[]
                {
                    KitchenType.Pot,
                    KitchenType.Pan
                });
                break;

            default:
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

            var names = System.Enum.GetNames(enumType).ToList();

            subTypeDropdown.AddOptions(names);
            subTypeDropdown.value = 0;
        }
    }

    private void ShowSubTypeDropdown<T>(IEnumerable<T> values) where T : System.Enum
    {
        if (dropdownContainer != null) dropdownContainer.SetActive(true);

        if (subTypeDropdown != null)
        {
            subTypeDropdown.ClearOptions();

            var names = values.Select(v => v.ToString()).ToList();

            subTypeDropdown.AddOptions(names);
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

        string selectedName = subTypeDropdown.options[subTypeDropdown.value].text;
        int subTypeInt = 0;

        switch (_selectedTemplate.counterType)
        {
            case CounterType.ContainerCounter:
                if (System.Enum.TryParse(typeof(EFoodType), selectedName, out object foodResult))
                {
                    subTypeInt = (int)foodResult;
                }
                break;
            case CounterType.StoveCounter:
                if (System.Enum.TryParse(typeof(KitchenType), selectedName, out object kitchenResult))
                {
                    subTypeInt = (int)kitchenResult;
                }
                break;
        }

        int counterId = CounterIdConverter.ToId(_selectedTemplate.counterType, subTypeInt);
        LevelDesignerManager.Instance.SpawnCounter(counterId, Vector3.zero, Vector3.zero);
        HideSubTypeDropdown();
    }

    private void HandleCounterSelected(BaseCounter counter)
    {
        if (counterInfoPanel == null || counterInfoText == null) return;
        
        if (LevelDesignerManager.Instance.TryGetCounterData(counter, out CounterData data))
        {
            CounterType type = CounterIdConverter.GetCounterType(data.counterId);
            string subTypeStr = "";
            
            if (type == CounterType.ContainerCounter)
            {
                subTypeStr = $"\n<b>Sub-Type:</b> {CounterIdConverter.GetFoodType(data.counterId)}";
            }
            else if (type == CounterType.StoveCounter)
            {
                subTypeStr = $"\n<b>Sub-Type:</b> {CounterIdConverter.GetKitchenType(data.counterId)}";
            }
            
            string cName = counter.gameObject.name.Replace("(Clone)", "");
            counterInfoText.text = $"<b>Name:</b> {cName}\n<b>ID:</b> {data.counterId}\n<b>Pos:</b> {data.position}{subTypeStr}";
            counterInfoPanel.SetActive(true);
        }
    }

    private void HandleCounterDeselected()
    {
        if (counterInfoPanel != null) counterInfoPanel.SetActive(false);
    }
}
