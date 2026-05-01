using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pooling;
using Kitchen;
using Player;
using System;
using UnityEngine.Serialization;

namespace Counter
{
    public class ContainerCounter : BaseCounter
    {
        [SerializeField] private Animation anim;
        [SerializeField] private ContainerData[] containerDataArr;
        [SerializeField] private Renderer decalRenderer;

        private EFoodType _containerEFoodType;


        public void SetContainer(EFoodType eFoodType)
        {
            _containerEFoodType = eFoodType;
            foreach (var data in containerDataArr)
            {
                if (data.eFoodType == eFoodType)
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

                    var food = SpawnKitchenObject(_containerEFoodType) as FoodObject;
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
        public EFoodType eFoodType;
        public Material material;
    }
}
