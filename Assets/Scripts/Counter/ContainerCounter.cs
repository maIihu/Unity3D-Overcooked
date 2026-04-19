using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContainerCounter : BaseCounter, IKitchenObjectParent
{
    private KitchenObject _kitchenObject;
    [SerializeField] private Animation anim;
    [SerializeField] private FoodType containerFoodType;
    [SerializeField] private FoodObject[] foodObjects;
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    
    public override void Interact(Player player)
    {
        if (HasKitchenObject())
        {
            if (!player.HasKitchenObject())
            {
                GetKitchenObject().SetKitchenObjectParent(player);
            }
        }
        else
        {
            if (player.HasKitchenObject())
            {
                player.GetKitchenObject().SetKitchenObjectParent(this);
            }
            else
            {
                foreach (var food in foodObjects)
                {
                    if (food.FoodType == containerFoodType)
                    {
                        anim.Play("OpenClose");
                        var foodGO = Instantiate(food);
                        foodGO.SetInitFood();
                        foodGO.SetKitchenObjectParent(player);
                    }
                }
            }
        }
    }

    #region IKitchenObjectParent

    public Transform GetKitchenObjectToTransform()
    {
        return CounterTopPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this._kitchenObject = kitchenObject;
    }

    public KitchenObject GetKitchenObject()
    {
        return this._kitchenObject;
    }

    public void ClearKitchenObject()
    {
        this._kitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return this._kitchenObject != null;
    }

    #endregion
    
    
}
