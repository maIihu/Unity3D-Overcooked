using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pooling;
using Kitchen;
using System;
using _Game.Scripts.Gameplay;
using UnityEngine.Serialization;
using Fusion;

namespace Counter
{
    public class ContainerCounter : BaseCounter
    {
        [SerializeField] private Animation anim;
        [SerializeField] private ContainerData[] containerDataArr;
        [SerializeField] private Renderer[] decalRendererArr;

        [SerializeField] private EFoodType containerEFoodType;

        [Networked]
        [OnChangedRender(nameof(OnContainerChanged))]
        private EFoodType NetworkedContainerEFoodType { get; set; }

        public EFoodType ContainerEFoodType => containerEFoodType;

        public void SetContainer(EFoodType eFoodType)
        {
            if (GameCore.GameManager.Instance != null && GameCore.GameManager.Instance.IsOffline)
            {
                // Offline mode: just update visual, skip network state
            }
            else if (HasStateAuthority)
            {
                NetworkedContainerEFoodType = eFoodType;
            }
            UpdateVisual(eFoodType);
        }

        private void OnContainerChanged()
        {
            UpdateVisual(NetworkedContainerEFoodType);
        }

        private void UpdateVisual(EFoodType eFoodType)
        {
            containerEFoodType = eFoodType;
            foreach (var data in containerDataArr)
            {
                if (data.eFoodType == eFoodType)
                {
                    foreach (var ren in decalRendererArr)
                        ren.sharedMaterial = data.material;
                    break;
                }
            }
        }

        public override void Init()
        {
            base.Init();
            UpdateVisual(containerEFoodType);
        }

        public override void Spawned()
        {
            base.Spawned();
            UpdateVisual(NetworkedContainerEFoodType);
        }

        public override void Interact(IPlayer player)
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

                    var food = SpawnKitchenObject(containerEFoodType) as FoodObject;
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
