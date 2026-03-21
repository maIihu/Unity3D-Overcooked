using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashCounter : BaseCounter
{
    public override void Interact(Player player)
    {
        base.Interact(player);
        if(player.HasKitchenObject())
        {
            player.GetKitchenObject().DestroySelf();
        }
    }
}
