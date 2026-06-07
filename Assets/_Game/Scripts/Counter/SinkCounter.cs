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

        // ── Offline local state ──
        private bool _isOffline;
        private float _offlineWashingProgress;
        private bool _offlineIsWashing;

        public override void Init()
        {
            base.Init();
            _isOffline = GameCore.GameManager.Instance != null && GameCore.GameManager.Instance.IsOffline;
            _offlineWashingProgress = 0f;
            _offlineIsWashing = false;
        }

        public override void Spawned()
        {
            base.Spawned();
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
                bool isWashingNow = _isOffline ? _offlineIsWashing : (bool)IsWashing;
                if (isWashingNow)
                {
                    progressBarUI.Show();
                    float progress = _isOffline ? _offlineWashingProgress : (float)WashingProgress;
                    progressBarUI.UpdateProgress(progress / washingTime);
                }
                else
                {
                    progressBarUI.Hide();
                }
            }
        }

        private void Update()
        {
            if (!_isOffline) return;
            if (!_offlineIsWashing) return;

            KitchenObject ko = GetKitchenObject();
            if (ko == null || ko is not PlateObject plate || !plate.IsDirty())
            {
                _offlineIsWashing = false;
                return;
            }

            _offlineWashingProgress += Time.deltaTime;

            if (_offlineWashingProgress >= washingTime)
            {
                _offlineWashingProgress = 0f;
                _offlineIsWashing = false;
                plate.SetDirty(false);
            }
        }

        public override void Interact(IPlayer player)
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

        public override void InteractAlternate(IPlayer player)
        {
            base.InteractAlternate(player);
            
            if (!_isOffline && !HasStateAuthority) return;

            if (HasKitchenObject() && GetKitchenObject() is PlateObject plate && plate.IsDirty())
            {
                if (_isOffline) _offlineIsWashing = true;
                else IsWashing = true;
            }
        }
    }
}
