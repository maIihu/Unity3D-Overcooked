using UnityEngine;

namespace Kitchen
{
    public class FoodObject : KitchenObject
    {
        [SerializeField] private EFoodType eFoodType;
        [SerializeField] private GameObject normalVisual;
        [SerializeField] private GameObject cuttingVisual;
        [SerializeField] private GameObject soupVisual;
        [SerializeField] private float timeCooked;

        public override void OnSpawn()
        {
            base.OnSpawn();
            SetState(FoodState.Normal);
        }

        public FoodState FoodState { get; private set; }
        public EFoodType EFoodType => eFoodType;

        public void SetState(FoodState newState)
        {
            FoodState = newState;

            if (normalVisual != null) normalVisual.SetActive(false);
            if (cuttingVisual != null) cuttingVisual.SetActive(false);
            if (soupVisual != null) soupVisual.SetActive(false);

            switch (newState)
            {
                case FoodState.Normal:
                    if (normalVisual != null) normalVisual.SetActive(true);
                    break;
                case FoodState.Cut:
                    if (cuttingVisual != null) cuttingVisual.SetActive(true);
                    break;
                case FoodState.Soup:
                case FoodState.Fried:
                case FoodState.Burned:
                    if (soupVisual != null) soupVisual.SetActive(true);
                    break;
            }
        }
    }

    public enum FoodState
    {
        Normal,
        Cut,
        Soup,
        Fried,
        Burned
    }

    public enum EFoodType
    {
        Tomato = 0,
        Onion = 1,
        Bread = 2,
        Meat = 3,
        Cabbage = 4,
        
    }
}