using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pooling;
using Kitchen;
using Player;

namespace Counter
{
    public class ContainerCounter : BaseCounter
    {
        [SerializeField] private Animation anim;
        [SerializeField] private FoodType containerFoodType;

        public override void Interact(Player.Player player)
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
                    if (anim != null) anim.Play("OpenClose");

                    // Use the new pooling helper from BaseCounter
                    var food = SpawnKitchenObject(containerFoodType) as FoodObject;
                    if (food != null) food.SetState(FoodState.Normal);

                    food.SetKitchenObjectParent(player);
                }
            }
        }
    }
}
