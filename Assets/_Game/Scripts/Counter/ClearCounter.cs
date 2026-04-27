using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kitchen;
using Player;

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
                                food.SetState(FoodState.Soup);
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
    }
}
