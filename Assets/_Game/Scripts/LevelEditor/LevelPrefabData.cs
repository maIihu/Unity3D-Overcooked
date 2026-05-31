using System.Collections.Generic;
using Counter;
using Kitchen;
using UnityEngine;

/// <summary>
/// Gắn trên root của Level Prefab để lưu metadata mà Prefab hierarchy không tự động gán.
/// </summary>
public class LevelPrefabData : MonoBehaviour
{
    [SerializeField] public List<BaseCounter> baseCounters = new List<BaseCounter>();
    [SerializeField] public List<KitchenObject> kitchenObjects = new List<KitchenObject>();

    [SerializeField] public Vector3 cameraPosition;
    [SerializeField] public Vector3 cameraEulerAngles;
}
