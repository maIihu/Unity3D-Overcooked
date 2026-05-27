using System;
using _Game.Scripts.Gameplay;
using UnityEngine;
using Kitchen;
using GameUI;
using Fusion;

namespace Counter
{
    public class CuttingCounter : BaseCounter
    {
        [SerializeField] private float cuttingTime = 3f;
        [SerializeField] private ProgressBarUI progressBarUI;

        public event Action OnCutComplete;

        // Networked timer: được sync từ Host sang Client, tránh desync
        [Networked] private float CuttingProgress { get; set; }
        [Networked] private NetworkBool IsCutting { get; set; }

        // -------------------------------------------------------
        #region Fusion Lifecycle

        public override void Spawned()
        {
            CuttingProgress = 0f;
            IsCutting = false;
        }

        public override void FixedUpdateNetwork()
        {
            // Chỉ Host (StateAuthority) cập nhật timer để đảm bảo tính xác thực
            if (!HasStateAuthority) return;
            if (!IsCutting) return;

            KitchenObject ko = GetKitchenObject();
            if (ko == null || ko is not FoodObject food)
            {
                IsCutting = false;
                return;
            }

            CuttingProgress += Runner.DeltaTime;

            if (CuttingProgress >= cuttingTime)
            {
                CuttingProgress = 0f;
                IsCutting = false;
                food.SetState(FoodState.Cut);
                OnCutComplete?.Invoke();
            }
        }

        public override void Render()
        {
            // Cập nhật progress bar dựa trên giá trị networked — chạy mỗi frame trên tất cả client
            if (progressBarUI != null)
            {
                float ratio = cuttingTime > 0f ? CuttingProgress / cuttingTime : 0f;
                progressBarUI.UpdateProgress(ratio);
            }
        }

        #endregion

        // -------------------------------------------------------
        #region Interact

        public override void Interact(Player player)
        {
            base.Interact(player);

            if (HasKitchenObject())
            {
                // Không cho nhặt khi đang cắt
                if (IsCutting) return;

                GetKitchenObject().SetKitchenObjectParent(player);
                if (HasStateAuthority)
                {
                    CuttingProgress = 0f;
                }
            }
            else
            {
                if (player.HasKitchenObject())
                {
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                    if (HasStateAuthority)
                    {
                        CuttingProgress = 0f;
                        IsCutting = false;
                    }
                }
            }
        }

        public override void InteractAlternate(Player player)
        {
            base.InteractAlternate(player);

            // Được gọi bởi Player.FixedUpdateNetwork() mỗi tick khi đang cắt
            if (!HasStateAuthority) return;
            if (HasKitchenObject() && GetKitchenObject() is FoodObject { FoodState: FoodState.Normal })
            {
                IsCutting = true; // Timer sẽ chạy trong FixedUpdateNetwork
            }
        }

        #endregion

        // -------------------------------------------------------
        #region Animation / Sound Hooks

        public void CuttingSoundAndAnimation()
        {
            // TODO: phát âm thanh và animation
        }

        public void StopAnimationCut()
        {
            // TODO: dừng âm thanh và animation
        }

        #endregion
    }
}
