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
}

/// <summary>
/// Utility class to convert between counterId (single int) and CounterType + subType.
/// 
/// Encoding: counterId = (int)CounterType + subType
/// Example:
///   ClearCounter       = 100
///   ContainerCounter   = 200 (base), 201 = Tomato, 202 = Onion
///   CuttingCounter     = 300
///   StoveCounter       = 400 (base), 401 = Plate, 402 = Pot
///   TrashCounter       = 500
///   PlatesCounter      = 600
///   DeliveryCounter    = 700
/// </summary>
public static class CounterIdConverter
{
    private const int Multiplier = 100;

    /// <summary>
    /// Encode CounterType + sub-type index into a single int.
    /// Example: ContainerCounter(2) + Tomato(1) → 201
    /// </summary>
    public static int ToId(CounterType type, int subType = 0)
    {
        return (int)type * Multiplier + subType;
    }

    /// <summary>
    /// Decode a counterId back to CounterType.
    /// Example: 201 → 201/100 = 2 → ContainerCounter
    /// </summary>
    public static CounterType GetCounterType(int counterId)
    {
        return (CounterType)(counterId / Multiplier);
    }

    /// <summary>
    /// Decode a counterId back to the sub-type index.
    /// Example: 201 → 201%100 = 1 → Tomato
    /// </summary>
    public static int GetSubType(int counterId)
    {
        return counterId % Multiplier;
    }

    /// <summary>
    /// Convert counterId to FoodType (for ContainerCounter).
    /// </summary>
    public static FoodType GetFoodType(int counterId)
    {
        return (FoodType)GetSubType(counterId);
    }

    /// <summary>
    /// Convert counterId to KitchenType (for StoveCounter).
    /// </summary>
    public static KitchenType GetKitchenType(int counterId)
    {
        return (KitchenType)GetSubType(counterId);
    }
}
