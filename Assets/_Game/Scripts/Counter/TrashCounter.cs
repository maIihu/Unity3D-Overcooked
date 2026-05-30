using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Gameplay;
using UnityEngine;
using Kitchen;

namespace Counter
{
    public class TrashCounter : BaseCounter
    {
        public override void Interact(IPlayer player)
        {
            base.Interact(player);
            if (player.HasKitchenObject())
            {
                KitchenObject playerObject = player.GetKitchenObject();

                if (playerObject is PlateObject plate)
                {
                    // if (plate.HasKitchenObject())
                    // {
                    //     plate.GetKitchenObject().DestroySelf();
                    // }
                }
                else if (playerObject is PotObject pot)
                {
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
