using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CounterData
{
    public string counterId;
    public Vector3 position;
    public Vector3 rotation;
}

[Serializable]
public class LevelData
{
    public List<CounterData> counterList = new List<CounterData>();
}
