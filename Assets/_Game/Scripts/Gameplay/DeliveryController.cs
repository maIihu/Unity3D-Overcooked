using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.DesignPattern.Observer;
using UnityEngine;
using Kitchen;
using _Game.Scripts.Gameplay;
using _Game.Scripts.UI;
using Random = UnityEngine.Random;

namespace GameCore
{
    public class ActiveRecipe
    {
        public int Id;
        public MenuRecipeSO Data;
    }
    public class DeliveryController : MonoBehaviour
    {
        public static DeliveryController Instance { get; private set; }
        [SerializeField] private List<MenuRecipeSO> menuRecipeList;
        [SerializeField] private float spawnInterval = 5f; 
        
        private int currentRecipeId;
        private List<ActiveRecipe> currentRecipes = new();
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (menuRecipeList != null && menuRecipeList.Count > 0)
            {
                StartCoroutine(SpawnRoutine());
            }
            else
            {
                Debug.LogWarning("Menu recipe list is empty or not assigned.");
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                SpawnRandom();
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        private void SpawnRandom()
        {
            Debug.Log("Spawning recipe");
            var recipe = menuRecipeList[Random.Range(0, menuRecipeList.Count)];

            var activeRecipe = new ActiveRecipe
            {
                Id = currentRecipeId++,
                Data = recipe
            };

            currentRecipes.Add(activeRecipe);

            MessageManager.Instance.SendMessage(
                new Message(ProjectMessageType.OnSpawnNewRecipe,
                    new object[] { activeRecipe })
            );

            StartCoroutine(RemoveRecipeRoutine(activeRecipe, recipe.timeRemaining));
        }
        
        private IEnumerator RemoveRecipeRoutine(ActiveRecipe activeRecipe, float delay)
        {
            yield return new WaitForSeconds(delay);
            Debug.Log("Removing recipe");
            if (currentRecipes.Contains(activeRecipe))
            {
                currentRecipes.Remove(activeRecipe);

                MessageManager.Instance.SendMessage(
                    new Message(ProjectMessageType.OnRejectRecipe,
                        new object[] { activeRecipe })
                );
            }
        }

        public void DeliverPlate(PlateObject plate)
        {
            List<EFoodType> plateIngredients = plate.GetIngredientList();
            ActiveRecipe matchedRecipe = null;

            foreach (var activeRecipe in currentRecipes)
            {
                MenuRecipeSO recipeSO = activeRecipe.Data;

                if (recipeSO.foodObjectMenu.Count != plateIngredients.Count) continue;

                bool isMatch = true;
                foreach (var menuItem in recipeSO.foodObjectMenu)
                {
                    if (!plateIngredients.Contains(menuItem.foodType))
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    matchedRecipe = activeRecipe;
                    break;
                }
            }

            if (matchedRecipe != null)
            {
                Debug.Log("Recipe Matched: " + matchedRecipe.Data.menuType);
                currentRecipes.Remove(matchedRecipe);
                
                MessageManager.Instance.SendMessage(
                    new Message(ProjectMessageType.OnRecipeSuccess, 
                        new object[] { matchedRecipe })
                );
            }
            else
            {
                Debug.Log("No Recipe Matched");
                // Optional: Penalty or message for wrong delivery
            }
        }
    }
}
