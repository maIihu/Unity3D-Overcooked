using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kitchen;
using Player;

namespace Counter
{
    public class TrashCounter : BaseCounter
    {
        public override void Interact(Player.Player player)
        {
            base.Interact(player);
            if (player.HasKitchenObject())
            {
                KitchenObject playerObject = player.GetKitchenObject();

                if (playerObject is PlateObject plate)
                {
                    if (plate.HasKitchenObject())
                    {
                        plate.GetKitchenObject().DestroySelf();
                    }
                }
                else if (playerObject is PotObject pot)
                {
                    if (pot.HasKitchenObject())
                    {
                        pot.GetKitchenObject().DestroySelf();
                    }
                    pot.EmptyPot();
                }
                else
                {
                    playerObject.DestroySelf();
                }
            }
        }
    }
}
