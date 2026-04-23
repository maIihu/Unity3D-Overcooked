using System;
using System.Collections.Generic;
using Kitchen;
using UnityEngine;

[Serializable]
public class CounterTemplate
{
    public string counterId;
    public string displayName;
    public GameObject prefab;
    
    [Header("Special Configurations")]
    public FoodType foodType; // For ContainerCounter
    public PotObject vesselPrefab; // For StoveCounter
}

[CreateAssetMenu(fileName = "CounterTemplateList", menuName = "LevelDesigner/TemplateList")]
public class CounterTemplateListSO : ScriptableObject
{
    public List<CounterTemplate> templates = new List<CounterTemplate>();

    public CounterTemplate GetTemplateById(string id)
    {
        foreach (var t in templates)
        {
            if (t.counterId == id) return t;
        }
        return null;
    }
}
