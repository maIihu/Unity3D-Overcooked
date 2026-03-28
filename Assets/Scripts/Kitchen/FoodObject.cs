using UnityEngine;

public enum FoodState
{
    Normal,
    Cut,
}

public enum FoodType
{
    None,
    Tomato,
    Onion
}
public class FoodObject : KitchenObject
{
    [SerializeField] private FoodType foodType;
    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject cuttingVisual;
    
    public FoodState FoodState { get; private set; }
    public FoodType FoodType => foodType;

    public void SetInitFood()
    {
        FoodState = FoodState.Normal;
        normalVisual.SetActive(true);
        cuttingVisual.SetActive(false);
    }

    public void Cut()
    {
        FoodState = FoodState.Cut;
        normalVisual.SetActive(false);
        cuttingVisual.SetActive(true);
    }
    
}
