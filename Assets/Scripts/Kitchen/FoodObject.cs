using UnityEngine;

public enum FoodState
{
    Normal,
    Cut,
    Soup,
    Fried,
    Burned
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
    [SerializeField] private GameObject soupVisual;
    [SerializeField] private float timeCooked;

    public FoodState FoodState { get; private set; }
    public FoodType FoodType => foodType;

    public void SetInitFood()
    {
        FoodState = FoodState.Normal;
        normalVisual.SetActive(true);
        cuttingVisual.SetActive(false);
        soupVisual.SetActive(false);
    }

    public void Cut()
    {
        FoodState = FoodState.Cut;
        normalVisual.SetActive(false);
        cuttingVisual.SetActive(true);
        soupVisual.SetActive(false);
    }

    public void Soup()
    {
        FoodState = FoodState.Soup;
        normalVisual.SetActive(false);
        cuttingVisual.SetActive(false);
        soupVisual.SetActive(true);
    }

    public void Fried()
    {
        FoodState = FoodState.Fried;
        normalVisual.SetActive(false);
        cuttingVisual.SetActive(false);
        soupVisual.SetActive(true);
    }

    public void Burned()
    {
        FoodState = FoodState.Burned;
        normalVisual.SetActive(false);
        cuttingVisual.SetActive(false);
        soupVisual.SetActive(true); // User can set another visual later
    }
}
