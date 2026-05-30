using System;
using _Game.Scripts.Gameplay;
using UnityEngine;
using Pooling;
using Kitchen;

namespace Counter
{
    public class PlatesCounter : BaseCounter
    {
        public override void Interact(IPlayer player)
        {
            base.Interact(player);
            if (HasKitchenObject())
            {
                if (!player.HasKitchenObject())
                {
                    GetKitchenObject().SetKitchenObjectParent(player);
                }
                else
                {
                    if (player.GetKitchenObject() is FoodObject food)
                    {
                        PlateObject plate = GetKitchenObject() as PlateObject;
                        if (plate.TryAddIngredient(food))
                        {
                            food.DestroySelf();
                        }
                    }
                }
            }
            else
            {
                if (player.HasKitchenObject() && player.GetKitchenObject() is PlateObject)
                {
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                }
            }
        }
    }
}
