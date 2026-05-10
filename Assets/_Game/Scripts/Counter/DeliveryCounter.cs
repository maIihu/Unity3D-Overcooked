
using System;
using System.Collections.Generic;
using _Game.Scripts.Gameplay;
using GameCore;
using UnityEngine;
using Kitchen;
using Random = UnityEngine.Random;
namespace Counter
{
    public class DeliveryCounter : BaseCounter
    {
        public override void Interact(Player player)
        {
            base.Interact(player);
            if (player.HasKitchenObject())
            {
                if (player.GetKitchenObject() is PlateObject plate)
                {
                    // Chỉ nhận đĩa có đồ ăn
                    if (plate.GetIngredientList().Count > 0)
                    {
                        if (GameManager.Instance != null && GameManager.Instance.DeliveryController != null)
                        {
                            GameManager.Instance.DeliveryController.DeliverPlate(plate);
                        }
                        else
                        {
                            Debug.LogWarning("[DeliveryCounter] Mất GameManager hoặc DeliveryController! Kiểm tra lại scene.");
                        }

                        // Người chơi giao đĩa => Hủy cái đĩa (và các món trên đĩa)
                        player.GetKitchenObject().DestroySelf();
                    }
                }
            }
        }
    }
}