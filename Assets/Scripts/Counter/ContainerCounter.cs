using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pooling;
using Kitchen;
using Player;
using System;

namespace Counter
{
    public class ContainerCounter : BaseCounter
    {
        [SerializeField] private Animation anim;
        [SerializeField] private ContainerData[] containerDataArr;
        [SerializeField] private Renderer decalRenderer;

        private FoodType _containerFoodType;


        public void SetContainer(FoodType foodType)
        {
            _containerFoodType = foodType;
            foreach (var data in containerDataArr)
            {
                if (data.foodType == foodType)
                {
                    decalRenderer.material = data.material;
                    break;
                }
            }
        }

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

                    var food = SpawnKitchenObject(_containerFoodType) as FoodObject;
                    if (food != null)
                    {
                        food.SetState(FoodState.Normal);
                        food.SetKitchenObjectParent(player);
                    }
                }
            }
        }
    }

    [Serializable]
    public struct ContainerData
    {
        public FoodType foodType;
        public Material material;
    }
}
