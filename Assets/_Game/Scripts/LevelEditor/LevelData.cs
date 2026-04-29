using System;
using System.Collections.Generic;
using Counter;
using Kitchen;
using UnityEngine;

[Serializable]
public class CounterData
{
    public int counterId;
    public Vector3 position;
    public Vector3 rotation;
}

[Serializable]
public class LevelData
{
    public List<CounterData> counterList = new List<CounterData>();
    public Vector3 cameraPosition;
    public Vector3 cameraEulerAngles;
}

public static class CounterIdConverter
{
    private const int Multiplier = 100;
    
    public static int ToId(CounterType type, int subType = 0)
    {
        return (int)type * Multiplier + subType;
    }
    
    public static CounterType GetCounterType(int counterId)
    {
        return (CounterType)(counterId / Multiplier);
    }
    
    public static int GetSubType(int counterId)
    {
        return counterId % Multiplier;
    }
    
    public static EFoodType GetFoodType(int counterId)
    {
        return (EFoodType)GetSubType(counterId);
    }
    
    public static KitchenType GetKitchenType(int counterId)
    {
        return (KitchenType)GetSubType(counterId);
    }
    
    public static EKitchenStoveType GetStoveKitchenType(int counterId)
    {
        return (EKitchenStoveType)GetSubType(counterId);
    }
}
