using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kitchen;
using Player;
using Pooling;

namespace Counter
{
    public class ClearCounter : BaseCounter
    {
        public override void Interact(Player.Player player)
        {
            base.Interact(player);

            if (HasKitchenObject())
            { // player carrying kitchen obj
                if (!player.HasKitchenObject())
                    GetKitchenObject().SetKitchenObjectParent(player);
                else
                {
                    if (GetKitchenObject() is PotObject pot)
                    { // put pot has cooked food on plate
                        if (player.GetKitchenObject() is PlateObject playerPlate && pot.IsCooked && !pot.IsBurned && pot.IsFull())
                        {
                            List<EFoodType> potIngredients = pot.GetIngredientTypeList();
                            bool transferSuccess = true;

                            foreach (var ingredientType in potIngredients)
                            {
                                if (!playerPlate.TryAddIngredient(ingredientType))
                                {
                                    transferSuccess = false;
                                    break;
                                }
                            }

                            if (transferSuccess)
                            {
                                pot.EmptyPot();
                            }
                        }

                        if (!pot.IsBurned && !pot.IsFull() && player.HasKitchenObject())
                        {
                            if (player.GetKitchenObject() is FoodObject { FoodState: FoodState.Cut } food && pot.CanAddIngredient(food))
                            {
                                pot.OnIngredientAdded(food);
                                food.DestroySelf();
                            }
                        }
                    }
                    else if (GetKitchenObject() is FoodObject counterFood && player.GetKitchenObject() is PlateObject playerPlate)
                    {
                        if (playerPlate.TryAddIngredient(counterFood))
                        {
                            counterFood.DestroySelf();
                        }
                    }
                    else if (GetKitchenObject() is PlateObject counterPlate && player.GetKitchenObject() is FoodObject playerFood)
                    {
                        if (counterPlate.TryAddIngredient(playerFood))
                        {
                            playerFood.DestroySelf();
                        }
                    }
                    else if (GetKitchenObject() is PlateObject plate && player.GetKitchenObject() is PotObject playerPot)
                    { // take cooked food from pot to plate
                        if (playerPot.IsCooked && !playerPot.IsBurned && playerPot.IsFull())
                        {
                            List<EFoodType> potIngredients = playerPot.GetIngredientTypeList();
                            bool transferSuccess = true;

                            foreach (var ingredientType in potIngredients)
                            {
                                if (!plate.TryAddIngredient(ingredientType))
                                {
                                    transferSuccess = false;
                                    break;
                                }
                            }

                            if (transferSuccess)
                            {
                                playerPot.EmptyPot();
                            }
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
    }
}
