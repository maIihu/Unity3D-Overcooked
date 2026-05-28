using System;
using Fusion;
using Kitchen;
using GameUI;
using UnityEngine;
using GameCore.Network;
using _Game.Scripts.Gameplay;

namespace Counter
{
    public class SinkCounter : BaseCounter
    {
        [SerializeField] private float washingTime = 3f;
        [SerializeField] private ProgressBarUI progressBarUI;

        [Networked] private float WashingProgress { get; set; }
        [Networked] private NetworkBool IsWashing { get; set; }

        public override void Spawned()
        {
            WashingProgress = 0f;
            IsWashing = false;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (!IsWashing) return;

            KitchenObject ko = GetKitchenObject();
            if (ko == null || ko is not PlateObject plate || !plate.IsDirty())
            {
                IsWashing = false;
                return;
            }

            WashingProgress += Runner.DeltaTime;

            if (WashingProgress >= washingTime)
            {
                WashingProgress = 0f;
                IsWashing = false;
                plate.SetDirty(false);
            }
        }

        public override void Render()
        {
            if (progressBarUI != null)
            {
                if (IsWashing)
                {
                    progressBarUI.Show();
                    progressBarUI.UpdateProgress(WashingProgress / washingTime);
                }
                else
                {
                    progressBarUI.Hide();
                }
            }
        }

        public override void Interact(Player player)
        {
            if (!HasKitchenObject())
            {
                if (player.HasKitchenObject())
                {
                    if (player.GetKitchenObject() is PlateObject plate && plate.IsDirty())
                    {
                        plate.SetKitchenObjectParent(this);
                    }
                }
            }
            else
            {
                if (!player.HasKitchenObject())
                {
                    GetKitchenObject().SetKitchenObjectParent(player);
                }
                else
                {
                    // If player has a dirty plate and sink has one? Just ignore for simplicity
                }
            }
        }

        public override void InteractAlternate(Player player)
        {
            base.InteractAlternate(player);
            if (!HasStateAuthority) return;

            if (HasKitchenObject() && GetKitchenObject() is PlateObject plate && plate.IsDirty())
            {
                IsWashing = true;
            }
        }
    }
}
