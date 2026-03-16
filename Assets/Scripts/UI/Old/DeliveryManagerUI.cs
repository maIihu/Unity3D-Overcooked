using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryManagerUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private Transform recipeTemplate;

    private Dictionary<RecipeSO, GameObject> _recipeUIMap;

    private void Start()
    {
        _recipeUIMap = new Dictionary<RecipeSO, GameObject>();
        DeliveryManager.Instance.OnRecipeSpawned += DeliveryManager_OnRecipeSpawned;
        DeliveryManager.Instance.OnRecipeDestroy += DeliveryManagerOnRecipeDestroy;
    }

    private void DeliveryManager_OnRecipeSpawned(object sender, DeliveryManager.RecipeSpawnedEventArgs e)
    {
        Transform recipeTransform = Instantiate(recipeTemplate, container);
        var ui = recipeTransform.GetComponent<DeliveryManagerSingleUI>();
        ui.SetRecipe(e.waitingRecipe.recipeSO);

        // Gán UI vào lại chính instance WaitingRecipe
        e.waitingRecipe.uiObject = recipeTransform.gameObject;
    }


    private void DeliveryManagerOnRecipeDestroy(object sender, DeliveryManager.RecipeSpawnedEventArgs e)
    {
        // if (_recipeUIMap.TryGetValue(e.recipeSO, out GameObject recipeGO))
        // {
        //     Destroy(recipeGO);
        //     _recipeUIMap.Remove(e.recipeSO);
        // }
    }
}