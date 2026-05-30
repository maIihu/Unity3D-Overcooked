using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Game.Scripts.DesignPattern.Observer;
using _Game.Scripts.Gameplay;
using Kitchen;
using Pooling;
using UnityEngine;
using Fusion;

namespace GameCore
{
    /// <summary>
    /// Wrapper that pairs a recipe with a unique runtime ID for tracking.
    /// </summary>
    public class ActiveRecipe
    {
        private static int _nextId;
        public int Id { get; set; }
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

    public struct NetworkRecipe : INetworkStruct, IEquatable<NetworkRecipe>
    {
        public int Id;
        public int RecipeIndex;
        public int SpawnTick;
        public bool Equals(NetworkRecipe other) => Id == other.Id;
    }

    /// <summary>
    /// Manages recipe spawning, delivery validation, and scoring.
    /// </summary>
    public class DeliveryController : NetworkBehaviour
    {
        [Header("Recipe Config")]
        [SerializeField] private List<MenuRecipeSO> menuRecipeList;
        [SerializeField] private int maxActiveRecipes = 4;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private float returnPlateDelay = 3f;

        [Networked]
        [Capacity(8)]
        private NetworkLinkedList<NetworkRecipe> NetworkedRecipes { get; }

        [Networked] public NetworkBool IsSpawning { get; set; }

        private readonly List<ActiveRecipe> _activeRecipes = new List<ActiveRecipe>();
        private float _spawnTimer = 0f;

        // ── Public API ─────────────────────────────────────────

        public IReadOnlyList<ActiveRecipe> ActiveRecipes => _activeRecipes;

        public void StartSpawning()
        {
            if (HasStateAuthority) IsSpawning = true;
        }

        public void StopSpawning()
        {
            if (HasStateAuthority) IsSpawning = false;
        }

        public override void Spawned()
        {
            _activeRecipes.Clear();
            _spawnTimer = spawnInterval; // Spawn first recipe after interval
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (!IsSpawning) return;
            
            // Handle spawning
            if (NetworkedRecipes.Count < maxActiveRecipes && menuRecipeList != null && menuRecipeList.Count > 0)
            {
                _spawnTimer -= Runner.DeltaTime;
                if (_spawnTimer <= 0f)
                {
                    _spawnTimer = spawnInterval;
                    SpawnNewRecipe();
                }
            }

            // Handle expiration
            for (int i = NetworkedRecipes.Count - 1; i >= 0; i--)
            {
                var netRecipe = NetworkedRecipes[i];
                MenuRecipeSO recipeData = menuRecipeList[netRecipe.RecipeIndex];
                float age = (Runner.Tick - netRecipe.SpawnTick) * Runner.DeltaTime;
                
                if (age >= recipeData.timeRemaining)
                {
                    // Expired!
                    NetworkedRecipes.Remove(netRecipe);
                    RPC_DeliverReject(netRecipe.Id, netRecipe.RecipeIndex);
                }
            }
        }

        private void SpawnNewRecipe()
        {
            int recipeIndex = UnityEngine.Random.Range(0, menuRecipeList.Count);
            NetworkedRecipes.Add(new NetworkRecipe
            {
                Id = UnityEngine.Random.Range(1, 9999999),
                RecipeIndex = recipeIndex,
                SpawnTick = Runner.Tick
            });
            // We don't send MessageManager here, we wait for Render to sync it on ALL clients!
        }

        /// <summary>
        /// Called by DeliveryCounter when a plate is delivered.
        /// Checks ingredients against active orders.
        /// </summary>
        public void DeliverPlate(PlateObject plate)
        {
            if (!HasStateAuthority) return; // Only host evaluates deliveries

            List<EFoodType> plateIngredients = plate.GetIngredientList();

            for (int i = 0; i < NetworkedRecipes.Count; i++)
            {
                var netRecipe = NetworkedRecipes[i];
                MenuRecipeSO recipeData = menuRecipeList[netRecipe.RecipeIndex];
                
                List<EFoodType> required = new List<EFoodType>();
                if (recipeData.foodObjectMenu != null)
                {
                    foreach (var m in recipeData.foodObjectMenu) required.Add(m.foodType);
                }

                if (plateIngredients.Count == required.Count && !required.Except(plateIngredients).Any())
                {
                    // Success!
                    NetworkedRecipes.Remove(netRecipe);
                    
                    // Tính điểm (Tip system)
                    float age = (Runner.Tick - netRecipe.SpawnTick) * Runner.DeltaTime;
                    float timeRemaining = recipeData.timeRemaining - age;
                    float timePercentage = timeRemaining / recipeData.timeRemaining;
                    
                    int scoreAdded = 20; // Base score
                    if (timePercentage >= 0.5f) scoreAdded += 10; // Tip cao (giao sớm)
                    else if (timePercentage >= 0.25f) scoreAdded += 5; // Tip thấp (giao vừa)

                    RPC_DeliverSuccess(netRecipe.Id, netRecipe.RecipeIndex, scoreAdded);
                    
                    StartCoroutine(ReturnDirtyPlateAfterDelay());
                    return;
                }
            }

            // No match — reject
            if (NetworkedRecipes.Count > 0)
            {
                var first = NetworkedRecipes[0];
                NetworkedRecipes.Remove(first);
                
                RPC_DeliverReject(first.Id, first.RecipeIndex);
            }
            
            StartCoroutine(ReturnDirtyPlateAfterDelay());
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_DeliverSuccess(int recipeId, int recipeIndex, int scoreAdded)
        {
            var recipeData = menuRecipeList[recipeIndex];
            var recipe = new ActiveRecipe(recipeData);
            recipe.Id = recipeId;
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnRecipeSuccess, new object[] { recipe, scoreAdded }));
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_DeliverReject(int recipeId, int recipeIndex)
        {
            var recipeData = menuRecipeList[recipeIndex];
            var recipe = new ActiveRecipe(recipeData);
            recipe.Id = recipeId;
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnRejectRecipe, new object[] { recipe }));
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

        public void ClearAllRecipes()
        {
            if (HasStateAuthority)
            {
                NetworkedRecipes.Clear();
            }
        }

        public override void Render()
        {
            // Sync NetworkedRecipes to local _activeRecipes for UI
            
            // 1. Check for removed recipes
            for (int i = _activeRecipes.Count - 1; i >= 0; i--)
            {
                bool found = false;
                foreach (var netR in NetworkedRecipes)
                {
                    if (netR.Id == _activeRecipes[i].Id)
                    {
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    // Item was removed! (We don't know if success or reject here on Client, but UI just needs to remove it)
                    // If we need to trigger UI remove animation, we could broadcast a message here.
                    // The UI will re-read ActiveRecipes list and remove the old one.
                    _activeRecipes.RemoveAt(i);
                }
            }

            // 2. Check for newly spawned recipes
            foreach (var netR in NetworkedRecipes)
            {
                bool found = false;
                foreach (var actR in _activeRecipes)
                {
                    if (actR.Id == netR.Id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // New recipe spawned!
                    MenuRecipeSO recipeData = menuRecipeList[netR.RecipeIndex];
                    ActiveRecipe newRecipe = new ActiveRecipe(recipeData);
                    newRecipe.Id = netR.Id; // Override the ID to match network ID
                    
                    _activeRecipes.Add(newRecipe);
                    MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnSpawnNewRecipe, new object[] { newRecipe }));
                }
            }
        }
    }
}
