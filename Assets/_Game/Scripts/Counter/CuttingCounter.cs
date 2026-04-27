using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kitchen;
using Player;
using GameUI;

namespace Counter
{
    public class CuttingCounter : BaseCounter
    {
        [SerializeField] private float cuttingTime;
        [SerializeField] private ProgressBarUI progressBarUI;

        public event Action OnCutComplete;

        private float _cuttingProgress;

        public override void Interact(Player.Player player)
        {
            base.Interact(player);
            if (HasKitchenObject())
            {
                // Kiểm tra nếu đang cắt (tiến trình > 0) thì không cho lấy ra
                if (_cuttingProgress > 0) return;

                GetKitchenObject().SetKitchenObjectParent(player);

                progressBarUI?.UpdateProgress(0f);
            }
            else
            {
                if (player.HasKitchenObject())
                {
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                    _cuttingProgress = 0f;

                    progressBarUI?.UpdateProgress(0f);
                }
            }
        }

        public override void InteractAlternate(Player.Player player)
        {
            base.InteractAlternate(player);
            if (HasKitchenObject() && GetKitchenObject() is FoodObject food)
            {
                _cuttingProgress += Time.deltaTime;

                progressBarUI?.UpdateProgress(_cuttingProgress / cuttingTime);

                if (_cuttingProgress >= cuttingTime)
                {
                    _cuttingProgress = 0f;
                    food.SetState(FoodState.Cut);
                    OnCutComplete?.Invoke();

                    progressBarUI?.UpdateProgress(0f);
                }
            }
        }

        public void CuttingSoundAndAnimation()
        {
            // Implementation
        }

        public void StopAnimationCut()
        {
            // Implementation
        }
    }
}
