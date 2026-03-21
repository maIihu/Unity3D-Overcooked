using UnityEngine;

public enum FoodType
{
    None, 
    Tomato
}
public class FoodObject : KitchenObject
{
    [SerializeField] private FoodType foodType;
    public FoodType FoodType => foodType;
}
