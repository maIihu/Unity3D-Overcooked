using System;
using UnityEngine;
using Pooling;
using Kitchen;
using Player;

namespace Counter
{
    public class PlatesCounter : BaseCounter
    {
        private float _spawnPlateTimer;
        private float _spawnPlateTimerMax = 4f;

        private void Update()
        {
            if (!HasKitchenObject())
            {
                _spawnPlateTimer += Time.deltaTime;
                if (_spawnPlateTimer > _spawnPlateTimerMax)
                {
                    SpawnPlate();
                    _spawnPlateTimer = 0;
                }
            }
        }

        public override void Interact(Player.Player player)
        {
            base.Interact(player);
            if (HasKitchenObject())
            {
                if (!player.HasKitchenObject())
                {
                    GetKitchenObject().SetKitchenObjectParent(player);
                }
                else
                {
                    if (player.GetKitchenObject() is FoodObject food)
                    {
                        PlateObject plate = GetKitchenObject() as PlateObject;
                        if (food.FoodState == FoodState.Cut)
                        {
                            food.SetKitchenObjectParent(plate);
                        }
                    }
                }
            }
            else
            {
                if (player.HasKitchenObject() && player.GetKitchenObject() is PlateObject)
                {
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                }
            }
        }

        private void SpawnPlate()
        {
            // Use the new pooling helper from BaseCounter
            SpawnKitchenObject(KitchenType.Plate);
        }
    }
}
