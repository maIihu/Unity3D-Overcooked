using System;
using _Game.Scripts.Gameplay;
using UnityEngine;
using Pooling;
using Kitchen;
using UnityEngine.SceneManagement;

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

        public override void Interact(Player player)
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
                        if (plate.TryAddIngredient(food))
                        {
                            food.DestroySelf();
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
            var scene = SceneManager.GetSceneByName("LevelDesigner");
            if (scene.IsValid() && scene.isLoaded && SceneManager.GetActiveScene() == scene) return;
            SpawnKitchenObject(KitchenType.Plate);
        }
    }
}
