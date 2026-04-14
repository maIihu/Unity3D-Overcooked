
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DeliveryCounter : BaseCounter
{
    protected override void Start()
    {
        base.Start();
    }

    public override void Interact(Player player)
    {
        base.Interact(player);
        if (player.HasKitchenObject())
        {
            if (player.GetKitchenObject() is PlateObject plate)
            {
                // Chỉ nhận đĩa có đồ ăn
                if (plate.HasKitchenObject())
                {
                    if (LevelController.Instance.DeliveryControl != null)
                    {
                        LevelController.Instance.DeliveryControl.DeliverRecipe(plate);
                    }
                    else
                    {
                        Debug.LogWarning("[DeliveryCounter] Mất DeliveryController instance! Kéo file vào inspector đi kìa.");
                    }

                    // Người chơi giao đĩa => Hủy cái đĩa (và các món trên đĩa)
                    player.GetKitchenObject().DestroySelf();
                }
            }
        }
    }
}
