using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Game.Scripts.DesignPattern.Observer;
using _Game.Scripts.Gameplay;
using Kitchen;
using Pooling;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Wrapper that pairs a recipe with a unique runtime ID for tracking.
    /// </summary>
    public class ActiveRecipe
    {
        private static int _nextId;
        public int Id { get; }
        public MenuRecipeSO Data { get; }
        public List<EFoodType> RequiredIngredients { get; }

        public ActiveRecipe(MenuRecipeSO data)
        {
            Id = _nextId++;
            Data = data;
            RequiredIngredients = new List<EFoodType>();
            if (data != null && data.foodObjectMenu != null)
            {
                foreach (var m in data.foodObjectMenu) RequiredIngredients.Add(m.foodType);
            }
        }
    }

    /// <summary>
    /// Manages recipe spawning, delivery validation, and scoring.
    /// NOT a Singleton — accessed via GameManager.Instance.DeliveryController.
    /// </summary>
    public class DeliveryController : MonoBehaviour
    {
        [Header("Recipe Config")]
        [SerializeField] private List<MenuRecipeSO> menuRecipeList;
        [SerializeField] private int maxActiveRecipes = 4;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private float returnPlateDelay = 3f;

        private readonly List<ActiveRecipe> _activeRecipes = new List<ActiveRecipe>();
        private Coroutine _spawnCoroutine;

        // ── Public API ─────────────────────────────────────────

        public IReadOnlyList<ActiveRecipe> ActiveRecipes => _activeRecipes;

        /// <summary>
        /// Start periodically spawning recipe orders.
        /// </summary>
        public void StartSpawning()
        {
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        /// <summary>
        /// Stop spawning new orders.
        /// </summary>
        public void StopSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }

        /// <summary>
        /// Called by DeliveryCounter when a plate is delivered.
        /// Checks ingredients against active orders.
        /// </summary>
        public void DeliverPlate(PlateObject plate)
        {
            List<EFoodType> plateIngredients = plate.GetIngredientList();

            for (int i = 0; i < _activeRecipes.Count; i++)
            {
                ActiveRecipe recipe = _activeRecipes[i];
                List<EFoodType> required = recipe.RequiredIngredients;

                // Check if plate matches recipe
                if (plateIngredients.Count == required.Count
                    && !required.Except(plateIngredients).Any())
                {
                    // Success!
                    _activeRecipes.RemoveAt(i);
                    MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnRecipeSuccess, new object[] { recipe }));
                    Debug.Log($"[DeliveryController] Recipe delivered: {recipe.Data.menuType}");
                    
                    // Return dirty plate
                    StartCoroutine(ReturnDirtyPlateAfterDelay());
                    return;
                }
            }

            // No match — reject
            Debug.Log("[DeliveryController] Wrong recipe delivered!");
            if (_activeRecipes.Count > 0)
            {
                ActiveRecipe rejected = _activeRecipes[0];
                _activeRecipes.RemoveAt(0);
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnRejectRecipe, new object[] { rejected }));
            }
            
            // Return dirty plate even if wrong recipe
            StartCoroutine(ReturnDirtyPlateAfterDelay());
        }

        private IEnumerator ReturnDirtyPlateAfterDelay()
        {
            yield return new WaitForSeconds(returnPlateDelay);

            var platesCounter = GameManager.Instance.LevelController.GetEmptyPlatesCounter();
            if (platesCounter != null)
            {
                if (GameCore.Network.FusionNetworkRunner.Instance.Runner.IsServer)
                {
                    var prefab = PoolManager.Instance.Kitchen.GetPrefab(KitchenType.Plate);
                    if (prefab != null)
                    {
                        var netObj = GameCore.Network.FusionNetworkRunner.Instance.Runner.Spawn(prefab, platesCounter.GetKitchenObjectToTransform().position, Quaternion.identity);
                        var plate = netObj.GetComponent<PlateObject>();
                        if (plate != null)
                        {
                            plate.SetKitchenObjectParent(platesCounter);
                            plate.SetDirty(true);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("[DeliveryController] No empty PlatesCounter found to return dirty plate!");
            }
        }

        /// <summary>
        /// Clear all active recipes (e.g. on level reset).
        /// </summary>
        public void ClearAllRecipes()
        {
            _activeRecipes.Clear();
        }

        // ── Private ────────────────────────────────────────────

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);

                if (_activeRecipes.Count >= maxActiveRecipes) continue;
                if (menuRecipeList == null || menuRecipeList.Count == 0) continue;

                MenuRecipeSO randomRecipe = menuRecipeList[UnityEngine.Random.Range(0, menuRecipeList.Count)];
                ActiveRecipe active = new ActiveRecipe(randomRecipe);
                _activeRecipes.Add(active);

                MessageManager.Instance.SendMessage(
                    new Message(ProjectMessageType.OnSpawnNewRecipe,
                        new object[] { active }));

                Debug.Log($"[DeliveryController] Spawned recipe: {randomRecipe.menuType}");
            }
        }
    }
}
