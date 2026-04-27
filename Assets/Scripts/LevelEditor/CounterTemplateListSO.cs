using System;
using System.Collections.Generic;
using UnityEngine;
using Counter;

[Serializable]
public class CounterTemplate
{
    public CounterType counterType;
    public GameObject prefab;
}

[CreateAssetMenu(fileName = "CounterTemplateList", menuName = "LevelDesigner/TemplateList")]
public class CounterTemplateListSO : ScriptableObject
{
    public List<CounterTemplate> templates = new List<CounterTemplate>();

    public CounterTemplate GetTemplateByType(CounterType type)
    {
        foreach (var t in templates)
        {
            if (t.counterType == type) return t;
        }
        return null;
    }
}
