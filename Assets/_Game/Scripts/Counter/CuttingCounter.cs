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

        // ── Offline local state ──
        private bool _isOffline;
        private float _offlineCuttingProgress;
        private bool _offlineIsCutting;

        // -------------------------------------------------------
        #region Fusion Lifecycle

        public override void Init()
        {
            base.Init();
            _isOffline = GameCore.GameManager.Instance != null && GameCore.GameManager.Instance.IsOffline;
            _offlineCuttingProgress = 0f;
            _offlineIsCutting = false;
        }

        public override void Spawned()
        {
            base.Spawned();
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
            if (!_isOffline)
            {
                UpdateVisualProgress((float)CuttingProgress, (bool)IsCutting);
            }
        }

        private void UpdateVisualProgress(float progress, bool isCuttingNow)
        {
            if (progressBarUI != null)
            {
                if (isCuttingNow)
                {
                    progressBarUI.Show();
                    float ratio = cuttingTime > 0f ? progress / cuttingTime : 0f;
                    progressBarUI.UpdateProgress(ratio);
                }
                else
                {
                    progressBarUI.Hide();
                }
            }
        }

        // ── Offline Path ──
        private void Update()
        {
            if (!_isOffline) return;

            // Cập nhật Visual cho Offline vì Render() không được gọi khi mất Fusion
            UpdateVisualProgress(_offlineCuttingProgress, _offlineIsCutting);

            if (!_offlineIsCutting) return;

            KitchenObject ko = GetKitchenObject();
            if (ko == null || ko is not FoodObject food)
            {
                _offlineIsCutting = false;
                return;
            }

            _offlineCuttingProgress += Time.deltaTime;

            if (_offlineCuttingProgress >= cuttingTime)
            {
                _offlineCuttingProgress = 0f;
                _offlineIsCutting = false;
                food.SetState(FoodState.Cut);
                OnCutComplete?.Invoke();
            }
        }

        #endregion

        // -------------------------------------------------------
        #region Interact

        public override void Interact(IPlayer player)
        {
            base.Interact(player);

            if (HasKitchenObject())
            {
                // Không cho nhặt khi đang cắt
                if ((_isOffline && _offlineIsCutting) || (!_isOffline && IsCutting)) return;

                GetKitchenObject().SetKitchenObjectParent(player);
                if (_isOffline)
                {
                    _offlineCuttingProgress = 0f;
                }
                else if (HasStateAuthority)
                {
                    CuttingProgress = 0f;
                }
            }
            else
            {
                if (player.HasKitchenObject())
                {
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                    if (_isOffline)
                    {
                        _offlineCuttingProgress = 0f;
                        _offlineIsCutting = false;
                    }
                    else if (HasStateAuthority)
                    {
                        CuttingProgress = 0f;
                        IsCutting = false;
                    }
                }
            }
        }

        public override void InteractAlternate(IPlayer player)
        {
            base.InteractAlternate(player);

            if (!_isOffline && !HasStateAuthority) return;

            if (HasKitchenObject() && GetKitchenObject() is FoodObject { FoodState: FoodState.Normal })
            {
                if (_isOffline) _offlineIsCutting = true;
                else IsCutting = true;
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
