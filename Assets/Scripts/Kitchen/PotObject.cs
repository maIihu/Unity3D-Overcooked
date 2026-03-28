
using System;
using UnityEngine;

public class PotObject : KitchenObject
{
    [SerializeField] private float liquidHeight;
    [SerializeField] private GameObject liquidGO;
    [SerializeField] private float cookTime = 3f;

    private float _cookTimer;
    private bool _isCooking;
    
    private const int MaxCapacity = 3;

    private int _currentCount;
    

    public bool CanAddIngredient()
    {
        return _currentCount < MaxCapacity;
    }

    public void OnIngredientAdded()
    {
        _currentCount++;
        liquidGO.transform.localPosition = Vector3.up * (liquidHeight * _currentCount);
        
        _cookTimer = 0f;
        _isCooking = true;
    }

    private void Update()
    {
        if (_isCooking)
        {
            _cookTimer += Time.deltaTime;

            if (_cookTimer >= cookTime)
            {
                _isCooking = false;
                OnCooked();
            }
        }
    }
    
    private void OnCooked()
    {
        Debug.Log("Cooked!");
    }
}
