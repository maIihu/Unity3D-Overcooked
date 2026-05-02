using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Gameplay;
using UnityEngine;
using Kitchen;
using Pooling;

namespace Counter
{
    public class ClearCounter : BaseCounter
    {
        public override void Interact(Player player)
        {
            base.Interact(player);

            if (HasKitchenObject())
            { // player carrying kitchen obj
                HandleCounterHasObject(player);
            }
            else
            {
                if (player.HasKitchenObject())
                {
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                }
            }
        }

        private void HandleCounterHasObject(Player player)
        {
            if (!player.HasKitchenObject()) 
                GetKitchenObject().SetKitchenObjectParent(player);
            else
            {
                if (GetKitchenObject() is PotObject clearPot)
                { 
                    if (player.GetKitchenObject() is PlateObject playerPlate 
                        && clearPot.IsCooked && !clearPot.IsBurned && clearPot.IsFull())
                    { 
                        TransferPotToPlate(clearPot, playerPlate);
                    }
                    
                    if (!clearPot.IsBurned && !clearPot.IsFull() && player.HasKitchenObject())
                    {
                        if (player.GetKitchenObject() is FoodObject { FoodState: FoodState.Cut } food 
                            && clearPot.CanAddIngredient(food))
                        {
                            clearPot.OnIngredientAdded(food);
                            food.DestroySelf();
                        }
                    }
                }
                else if (GetKitchenObject() is FoodObject counterFood && 
                         player.GetKitchenObject() is PlateObject playerPlate)
                {
                    if (playerPlate.TryAddIngredient(counterFood)) 
                        counterFood.DestroySelf();
                }
                else if (GetKitchenObject() is PlateObject counterPlate && 
                         player.GetKitchenObject() is FoodObject playerFood)
                {
                    if (counterPlate.TryAddIngredient(playerFood))
                        playerFood.DestroySelf();
                }
                else if (GetKitchenObject() is PlateObject clearPlate && player.GetKitchenObject() is PotObject playerPot)
                { 
                    if (playerPot.IsCooked && !playerPot.IsBurned && playerPot.IsFull())
                        TransferPotToPlate(playerPot, clearPlate);
                }

            }
        }
        
        private void TransferPotToPlate(PotObject pot, PlateObject plate)
        {
            List<EFoodType> ingredients = pot.GetIngredientTypeList();

            foreach (var ingredient in ingredients)
            {
                if (!plate.TryAddIngredient(ingredient))
                    return;
            }

            pot.EmptyPot();
        }
    }
}
