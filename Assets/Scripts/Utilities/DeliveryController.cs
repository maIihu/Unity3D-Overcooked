using System;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryController : MonoBehaviour
{
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeFailed;

    private List<FoodType> possibleRecipes;
    private List<FoodType> waitingRecipeList;

    [SerializeField] private float spawnRecipeTimerMax = 4f;
    [SerializeField] private int waitingRecipesMax = 4;
    private float _spawnRecipeTimer;

    private void Awake()
    {
        waitingRecipeList = new List<FoodType>();

        possibleRecipes = new List<FoodType>
        {
            FoodType.Tomato,
            FoodType.Onion
        };
    }

    private void Update()
    {
        // Require the game to be in GamePlaying state
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentState() != GameManager.GameState.GamePlaying)
        {
            return;
        }

        _spawnRecipeTimer -= Time.deltaTime;
        if (_spawnRecipeTimer <= 0f)
        {
            _spawnRecipeTimer = spawnRecipeTimerMax;

            if (waitingRecipeList.Count < waitingRecipesMax)
            {
                FoodType waitingRecipe = possibleRecipes[UnityEngine.Random.Range(0, possibleRecipes.Count)];
                waitingRecipeList.Add(waitingRecipe);

                Debug.Log($"[DeliveryController] Đã spawn một thực đơn mới: Khách gọi món {waitingRecipe}");
                OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void DeliverRecipe(PlateObject plateObject)
    {
        if (plateObject.HasKitchenObject() && plateObject.GetKitchenObject() is FoodObject foodObject)
        {
            for (int i = 0; i < waitingRecipeList.Count; i++)
            {
                FoodType waitingRecipe = waitingRecipeList[i];

                if (waitingRecipe == foodObject.FoodType)
                {
                    if (foodObject.FoodState == FoodState.Fried || foodObject.FoodState == FoodState.Burned)
                    {
                        waitingRecipeList.RemoveAt(i);
                        Debug.Log($"[DeliveryController] Đã giao thành công món {waitingRecipe}!");
                        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                    else
                    {
                        Debug.Log($"[DeliveryController] Món {waitingRecipe} chưa được nấu chín!");
                    }
                }
            }
        }

        Debug.Log("[DeliveryController] Đĩa giao không hợp lệ, không có thực đơn nào khớp!");
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    public List<FoodType> GetWaitingRecipeList()
    {
        return waitingRecipeList;
    }
}
