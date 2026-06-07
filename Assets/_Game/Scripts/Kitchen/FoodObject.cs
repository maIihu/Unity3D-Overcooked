using Fusion;
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
            _isOffline = GameCore.GameManager.Instance != null && GameCore.GameManager.Instance.IsOffline;
            if (HasStateAuthority)
            {
                NetworkedFoodState = FoodState.Normal;
            }
            UpdateVisuals(_isOffline ? _offlineFoodState : NetworkedFoodState);
        }

        // ── Offline local state ──
        private FoodState _offlineFoodState;

        private void Start()
        {
            if (GameCore.GameManager.Instance != null && GameCore.GameManager.Instance.IsOffline)
            {
                _isOffline = true;
                _offlineFoodState = FoodState.Normal;
                UpdateVisuals(_offlineFoodState);
            }
        }

        [Networked]
        [OnChangedRender(nameof(OnStateChanged))]
        private FoodState NetworkedFoodState { get; set; }

        public FoodState FoodState => _isOffline ? _offlineFoodState : NetworkedFoodState;
        public EFoodType EFoodType => eFoodType;

        public void SetState(FoodState newState)
        {
            if (_isOffline)
            {
                _offlineFoodState = newState;
            }
            else if (HasStateAuthority)
            {
                NetworkedFoodState = newState;
            }
            UpdateVisuals(newState);
        }

        private void OnStateChanged()
        {
            if (_isOffline) return;
            UpdateVisuals(NetworkedFoodState);
        }

        private void UpdateVisuals(FoodState state)
        {
            if (normalVisual != null) normalVisual.SetActive(false);
            if (cuttingVisual != null) cuttingVisual.SetActive(false);
            if (soupVisual != null) soupVisual.SetActive(false);

            switch (state)
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