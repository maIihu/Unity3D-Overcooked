using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelDesignerUI : MonoBehaviour
{
    [SerializeField] private CounterTemplateListSO templateList;
    [SerializeField] private Transform paletteContainer;
    [SerializeField] private GameObject paletteButtonPrefab;
    
    [SerializeField] private TMP_InputField levelNameInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button clearButton;

    private void Start()
    {
        GeneratePalette();
        
        saveButton.onClick.AddListener(() => LevelDesignerManager.Instance.SaveLevel(levelNameInput.text));
        loadButton.onClick.AddListener(() => LevelDesignerManager.Instance.LoadLevel(levelNameInput.text));
        clearButton.onClick.AddListener(() => LevelDesignerManager.Instance.ClearLevel());
    }

    private void GeneratePalette()
    {
        // Clear existing buttons
        foreach (Transform child in paletteContainer) Destroy(child.gameObject);

        foreach (var template in templateList.templates)
        {
            GameObject btnGO = Instantiate(paletteButtonPrefab, paletteContainer);
            btnGO.SetActive(true);
            Button btn = btnGO.GetComponent<Button>();
            TMP_Text txt = btnGO.GetComponentInChildren<TMP_Text>();

            if (txt != null) txt.text = template.displayName;
            
            btn.onClick.AddListener(() => {
                // Spawn at center of screen/world for now
                LevelDesignerManager.Instance.SpawnCounter(template.counterId, Vector3.zero, Vector3.zero);
            });
        }
    }
}
