using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearCounter : BaseCounter, IKitchenObjectParent
{
    private KitchenObject _kitchenObject;
    public override void Interact(Player player)
    {
        base.Interact(player);

        if (HasKitchenObject())
        { // player carrying kitchen obj
            if (!player.HasKitchenObject())
                _kitchenObject.SetKitchenObjectParent(player);
            else
            {
                if (GetKitchenObject() is PotObject pot)
                { // put pot has cooked food on plate
                    if (player.GetKitchenObject() is PlateObject playerPlate && pot.IsCooked && !pot.IsBurned && pot.IsFull() && pot.HasKitchenObject() && !playerPlate.HasKitchenObject())
                    {
                        KitchenObject potFood = pot.GetKitchenObject();
                        potFood.SetKitchenObjectParent(playerPlate);
                        pot.EmptyPot();
                    }

                    if (!pot.IsBurned && !pot.IsFull() && player.HasKitchenObject())
                    {
                        if (player.GetKitchenObject() is FoodObject { FoodState: FoodState.Cut } food && pot.CanAddIngredient())
                        {

                            if (!pot.HasKitchenObject())
                            {
                                food.SetKitchenObjectParent(pot);
                            }
                            else
                            {
                                food.DestroySelf();
                            }

                            pot.OnIngredientAdded();
                            food.Soup();
                        }

                    }
                }
                else if (GetKitchenObject() is PlateObject plate && player.GetKitchenObject() is PotObject playerPot)
                { // take cooked food from pot to plate
                    if (playerPot.IsCooked && !playerPot.IsBurned && playerPot.IsFull() && playerPot.HasKitchenObject() && !plate.HasKitchenObject())
                    {
                        KitchenObject potFood = playerPot.GetKitchenObject();
                        potFood.SetKitchenObjectParent(plate);
                        playerPot.EmptyPot();
                    }
                }

            }
        }
        else
        {
            if (player.HasKitchenObject())
            {
                player.GetKitchenObject().SetKitchenObjectParent(this);
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
