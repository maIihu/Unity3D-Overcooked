using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContainerCounter : BaseCounter
{
    [SerializeField] private Animation anim;
    [SerializeField] private FoodType containerFoodType;
    [SerializeField] private FoodObject[] foodObjects;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    
    public override void Interact(Player player)
    {
        foreach (var food in foodObjects)
        {
            if (food.FoodType == containerFoodType)
            {
                anim.Play("OpenClose");
                var foodGO = Instantiate(food);
                foodGO.SetKitchenObjectParent(player);
            }
        }
    }
    
    
}
