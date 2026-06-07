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
    /// Supports both Online (Fusion) and Offline (Single Mode) paths.
    /// </summary>
    public class DeliveryController : NetworkBehaviour
    {
        [Header("Recipe Config")]
        [SerializeField] private List<MenuRecipeSO> menuRecipeList;
        [SerializeField] private int maxActiveRecipes = 4;
        [SerializeField] private float initialSpawnDelay = 5f;
        [SerializeField] private float multiPlayerSpawnInterval = 30f;
        [SerializeField] private float singlePlayerSpawnInterval = 60f;
        [SerializeField] private float returnPlateDelay = 3f;

        [Networked]
        [Capacity(8)]
        private NetworkLinkedList<NetworkRecipe> NetworkedRecipes { get; }

        public bool IsSpawning { get; set; }

        private readonly List<ActiveRecipe> _activeRecipes = new List<ActiveRecipe>();
        private float _spawnTimer = 0f;

        // ── Offline state ──
        private bool _isOffline;
        private float _offlineSpawnTimer;
        private int _offlineScore;

        private void Start()
        {
            // Tự động gán reference cho GameManager, tránh dùng FindObjectOfType
            if (GameManager.Instance != null)
            {
                // Nếu đang online, ưu tiên DeliveryController nằm ngoài DontDestroyOnLoad (tức là được spawn trong scene)
                if (gameObject.scene.name != "DontDestroyOnLoad" || GameManager.Instance.DeliveryController == null)
                {
                    GameManager.Instance.DeliveryController = this;
                }
            }
        }

        // ── Public API ─────────────────────────────────────────

        public IReadOnlyList<ActiveRecipe> ActiveRecipes => _activeRecipes;

        public void StartSpawning()
        {
            _isOffline = GameManager.Instance != null && GameManager.Instance.IsOffline;
            IsSpawning = true;

            if (_isOffline)
            {
                _activeRecipes.Clear();
                _offlineSpawnTimer = initialSpawnDelay;
                _offlineScore = 0;
            }

            Debug.Log($"[DeliveryController] Start Spawn (offline={_isOffline})");
        }

        public void StopSpawning()
        {
            IsSpawning = false;
            Debug.Log("[DeliveryController] Stop Spawn");
        }

        public override void Spawned()
        {
            _activeRecipes.Clear();
            _spawnTimer = initialSpawnDelay; // Spawn first recipe after initial delay
            
            // Đảm bảo reference được gán lại khi mạng khởi tạo đối tượng
            if (GameManager.Instance != null && gameObject.scene.name != "DontDestroyOnLoad")
            {
                GameManager.Instance.DeliveryController = this;
            }
        }

        // ── Online path (Fusion) ─────────────────────────────────

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (!IsSpawning) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != EGameState.Play) return;
            
            // Handle spawning
            if (NetworkedRecipes.Count < maxActiveRecipes && menuRecipeList != null && menuRecipeList.Count > 0)
            {
                _spawnTimer -= Runner.DeltaTime;
                if (_spawnTimer <= 0f)
                {
                    Debug.Log("[DeliveryController] Spawning new recipe!");
                    _spawnTimer = GameManager.Instance != null && GameManager.Instance.IsOffline
                        ? singlePlayerSpawnInterval
                        : multiPlayerSpawnInterval;
                    
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
                    MenuRecipeSO recipeData = menuRecipeList[netR.RecipeIndex];
                    ActiveRecipe newRecipe = new ActiveRecipe(recipeData);
                    newRecipe.Id = netR.Id;
                    
                    _activeRecipes.Add(newRecipe);
                    MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnSpawnNewRecipe, new object[] { newRecipe }));
                }
            }
        }

        // ── Offline path (Single Mode — no Runner) ──────────────

        private void Update()
        {
            if (!_isOffline) return;
            if (!IsSpawning) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != EGameState.Play) return;

            OfflineSpawnUpdate();
            OfflineExpirationUpdate();
        }

        private void OfflineSpawnUpdate()
        {
            if (_activeRecipes.Count >= maxActiveRecipes || menuRecipeList == null || menuRecipeList.Count == 0) return;

            _offlineSpawnTimer -= Time.deltaTime;
            if (_offlineSpawnTimer <= 0f)
            {
                _offlineSpawnTimer = singlePlayerSpawnInterval;

                int recipeIndex = UnityEngine.Random.Range(0, menuRecipeList.Count);
                MenuRecipeSO recipeData = menuRecipeList[recipeIndex];
                ActiveRecipe newRecipe = new ActiveRecipe(recipeData);
                _activeRecipes.Add(newRecipe);

                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnSpawnNewRecipe, new object[] { newRecipe }));
                Debug.Log($"[DeliveryController-Offline] Spawned recipe: {recipeData.name}");
            }
        }

        private void OfflineExpirationUpdate()
        {
            // Offline recipes dùng real-time expiration qua UIMenuItem DOTween timer
            // Tạm thời không xử lý expiration ở đây — UIMenuItem timer sẽ xử lý visual
            // Có thể track spawn time nếu cần sau này
        }

        // ── Delivery (shared logic) ─────────────────────────────

        /// <summary>
        /// Called by DeliveryCounter when a plate is delivered.
        /// Checks ingredients against active orders.
        /// </summary>
        public void DeliverPlate(PlateObject plate)
        {
            if (_isOffline)
            {
                DeliverPlateOffline(plate);
                return;
            }

            // Online path
            if (!HasStateAuthority) return;

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
                
                    float age = (Runner.Tick - netRecipe.SpawnTick) * Runner.DeltaTime;
                    float timeRemaining = recipeData.timeRemaining - age;
                    float timePercentage = timeRemaining / recipeData.timeRemaining;
                
                    int scoreAdded = 20;
                    if (timePercentage >= 0.5f) scoreAdded += 10;
                    else if (timePercentage >= 0.25f) scoreAdded += 5;

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

        private void DeliverPlateOffline(PlateObject plate)
        {
            List<EFoodType> plateIngredients = plate.GetIngredientList();

            for (int i = 0; i < _activeRecipes.Count; i++)
            {
                var recipe = _activeRecipes[i];
                List<EFoodType> required = recipe.RequiredIngredients;

                if (plateIngredients.Count == required.Count && !required.Except(plateIngredients).Any())
                {
                    // Success!
                    _activeRecipes.RemoveAt(i);

                    int scoreAdded = 20; // Base score (offline không track spawn time)
                    _offlineScore += scoreAdded;

                    MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnRecipeSuccess, new object[] { recipe, scoreAdded }));
                    MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnScoreChanged, new object[] { _offlineScore }));

                    StartCoroutine(ReturnDirtyPlateAfterDelayOffline());
                    return;
                }
            }

            // No match — reject first recipe
            if (_activeRecipes.Count > 0)
            {
                var first = _activeRecipes[0];
                _activeRecipes.RemoveAt(0);
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnRejectRecipe, new object[] { first }));
            }

            StartCoroutine(ReturnDirtyPlateAfterDelayOffline());
        }

        // ── RPCs (Online only) ─────────────────────────────────

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_DeliverSuccess(int recipeId, int recipeIndex, int scoreAdded)
        {
            var recipeData = menuRecipeList[recipeIndex];
            var recipe = new ActiveRecipe(recipeData);
            recipe.Id = recipeId;
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnRecipeSuccess, new object[] { recipe, scoreAdded }));
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_DeliverFailed()
        {
            Debug.Log("Delivery Failed!");
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_DeliverReject(int recipeId, int recipeIndex)
        {
            var recipeData = menuRecipeList[recipeIndex];
            var recipe = new ActiveRecipe(recipeData);
            recipe.Id = recipeId;
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnRejectRecipe, new object[] { recipe }));
        }

        // ── Plate Return ───────────────────────────────────────

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
                        var returnedPlate = netObj.GetComponent<PlateObject>();
                        if (returnedPlate != null)
                        {
                            returnedPlate.SetKitchenObjectParent(platesCounter);
                            returnedPlate.SetDirty(true);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("[DeliveryController] No empty PlatesCounter found to return dirty plate!");
            }
        }

        private IEnumerator ReturnDirtyPlateAfterDelayOffline()
        {
            yield return new WaitForSeconds(returnPlateDelay);

            var platesCounter = GameManager.Instance.LevelController.GetEmptyPlatesCounter();
            if (platesCounter != null)
            {
                var prefab = PoolManager.Instance.Kitchen.GetPrefab(KitchenType.Plate);
                if (prefab != null)
                {
                    var go = Instantiate(prefab, platesCounter.GetKitchenObjectToTransform().position, Quaternion.identity);
                    var returnedPlate = go.GetComponent<PlateObject>();
                    if (returnedPlate != null)
                    {
                        returnedPlate.SetKitchenObjectParent(platesCounter);
                        returnedPlate.SetDirty(true);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[DeliveryController-Offline] No empty PlatesCounter found to return dirty plate!");
            }
        }

        public void ClearAllRecipes()
        {
            if (_isOffline)
            {
                _activeRecipes.Clear();
                return;
            }

            if (HasStateAuthority)
            {
                NetworkedRecipes.Clear();
            }
        }
    }
}
