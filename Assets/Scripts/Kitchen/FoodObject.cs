using UnityEngine;

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
    public FoodType FoodType => foodType;

    public void SetInitFood()
    {
        normalVisual.SetActive(true);
        cuttingVisual.SetActive(false);
    }

    public void Cut()
    {
        normalVisual.SetActive(false);
        cuttingVisual.SetActive(true);
    }
}
