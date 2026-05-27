using System.Collections.Generic;
using _Game.Scripts.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Pooling;
using Kitchen;
using GameUI;
using Fusion;

namespace Counter
{
    public class StoveCounter : BaseCounter
    {
        [SerializeField] private Transform potPoint;

        [SerializeField] private float fryingTimerMax = 4f;
        [SerializeField] private float burningTimerMax = 5f;

        [SerializeField] private Sprite completeSprite;
        [SerializeField] private Sprite warningSprite;

        [SerializeField] private Image imageUI;

        // --- Networked state (Host tính, sync sang Client) ---
        [Networked] private float FryingTimer { get; set; }
        [Networked] private float BurningTimer { get; set; }
        [Networked] private StoveState NetworkStoveState { get; set; }

        // --- Local visual state (không cần sync) ---
        private StoveState _lastRenderedState;
        private bool _isCompleteUIShown;
        private Tween _imageFadeTween;

        private KitchenType _kitchenType;

        // -------------------------------------------------------
        #region Fusion Lifecycle

        public override void Spawned()
        {
            FryingTimer = 0f;
            BurningTimer = 0f;
            NetworkStoveState = StoveState.Idle;
            _lastRenderedState = StoveState.Idle;
        }

        /// <summary>
        /// Timer logic chạy trên Host theo Fusion fixed tick.
        /// Dùng Runner.DeltaTime thay vì Time.deltaTime để đảm bảo sync.
        /// </summary>
        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if (_kitchenObject == null)
            {
                if (NetworkStoveState != StoveState.Idle)
                    NetworkStoveState = StoveState.Idle;
                return;
            }

            PotObject pot = _kitchenObject as PotObject;
            if (pot == null) return;

            switch (NetworkStoveState)
            {
                case StoveState.Idle:
                    if (pot.HasIngredients())
                    {
                        if (pot.IsBurned) NetworkStoveState = StoveState.Burned;
                        else if (pot.IsCooked) NetworkStoveState = StoveState.Fried;
                        else NetworkStoveState = StoveState.Frying;
                    }
                    break;

                case StoveState.Frying:
                    FryingTimer += Runner.DeltaTime;
                    if (FryingTimer >= fryingTimerMax)
                    {
                        pot.IsCooked = true;
                        FryingTimer = fryingTimerMax; // Clamp
                        NetworkStoveState = StoveState.Fried;
                    }
                    break;

                case StoveState.Fried:
                    BurningTimer += Runner.DeltaTime;
                    if (BurningTimer >= burningTimerMax)
                    {
                        pot.IsBurned = true;
                        BurningTimer = burningTimerMax; // Clamp
                        NetworkStoveState = StoveState.Burned;
                    }
                    break;

                case StoveState.Burned:
                    // Không làm gì thêm
                    break;
            }
        }

        /// <summary>
        /// Render() chạy mỗi frame trên tất cả client — dùng để cập nhật UI và VFX.
        /// </summary>
        public override void Render()
        {
            PotObject pot = _kitchenObject as PotObject;

            // Cập nhật progress bar của nồi
            if (pot != null)
            {
                switch (NetworkStoveState)
                {
                    case StoveState.Frying:
                        pot.UpdateCookingProgress(fryingTimerMax > 0f ? FryingTimer / fryingTimerMax : 0f);
                        break;
                    case StoveState.Fried:
                        pot.UpdateCookingProgress(burningTimerMax > 0f ? BurningTimer / burningTimerMax : 0f);
                        break;
                    case StoveState.Burned:
                        pot.UpdateCookingProgress(0f);
                        break;
                    default:
                        pot.UpdateCookingProgress(0f);
                        break;
                }
            }

            // Cập nhật UI khi state thay đổi
            if (NetworkStoveState != _lastRenderedState)
            {
                _lastRenderedState = NetworkStoveState;
                OnStoveStateChangedVisual(NetworkStoveState);
            }

            // Hiệu ứng warning khi sắp cháy
            if (NetworkStoveState == StoveState.Fried && pot != null)
            {
                HandleFriedUI();
            }
        }

        private void OnDestroy()
        {
            _imageFadeTween?.Kill();
        }

        #endregion

        // -------------------------------------------------------
        #region Setup

        public void SetStoveData(KitchenType kitchenType)
        {
            _kitchenType = kitchenType;
        }

        public override void Init()
        {
            base.Init();
            if (imageUI != null) imageUI.enabled = false;
            _kitchenObject = SpawnKitchenObject(_kitchenType);
            // Reset networked state handled in Spawned()
        }

        #endregion

        // -------------------------------------------------------
        #region Interact

        public override void Interact(Player player)
        {
            if (HasKitchenObject())
            {
                if (!player.HasKitchenObject())
                {
                    GetKitchenObject().SetKitchenObjectParent(player);
                }
                else
                {
                    PotObject pot = _kitchenObject as PotObject;

                    if (player.GetKitchenObject() is FoodObject { FoodState: FoodState.Cut } food && pot != null && pot.CanAddIngredient(food))
                    {
                        pot.OnIngredientAdded(food);
                        if (HasStateAuthority)
                        {
                            FryingTimer = 0f;
                            BurningTimer = 0f;
                            NetworkStoveState = StoveState.Frying;
                        }
                        food.DestroySelf();
                    }
                    else if (player.GetKitchenObject() is PlateObject plate && pot != null && pot.IsCooked && !pot.IsBurned && pot.IsFull())
                    {
                        List<EFoodType> potIngredients = pot.GetIngredientTypeList();
                        bool transferSuccess = true;

                        foreach (var ingredientType in potIngredients)
                        {
                            if (!plate.TryAddIngredient(ingredientType))
                            {
                                transferSuccess = false;
                                break;
                            }
                        }

                        if (transferSuccess)
                        {
                            pot.EmptyPot();
                            if (HasStateAuthority)
                            {
                                FryingTimer = 0f;
                                BurningTimer = 0f;
                                NetworkStoveState = StoveState.Idle;
                            }
                        }
                    }
                }
            }
            else
            {
                if (player.HasKitchenObject() && player.GetKitchenObject() is PotObject or PanObject)
                {
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                }
            }
        }

        public override void SetKitchenObject(KitchenObject kitchenObject)
        {
            base.SetKitchenObject(kitchenObject);

            _isCompleteUIShown = false;
            _imageFadeTween?.Kill();

            if (HasStateAuthority)
            {
                FryingTimer = 0f;
                BurningTimer = 0f;

                if (kitchenObject != null)
                {
                    PotObject pot = kitchenObject as PotObject;
                    if (pot != null)
                    {
                        pot.BurningTimerMax = burningTimerMax;
                        if (pot.HasIngredients())
                        {
                            if (pot.IsBurned) NetworkStoveState = StoveState.Burned;
                            else if (pot.IsCooked) NetworkStoveState = StoveState.Fried;
                            else NetworkStoveState = StoveState.Frying;
                        }
                        else
                        {
                            NetworkStoveState = StoveState.Idle;
                        }
                    }
                }
                else
                {
                    NetworkStoveState = StoveState.Idle;
                }
            }
        }

        public override void ClearKitchenObject()
        {
            base.ClearKitchenObject();
            _imageFadeTween?.Kill();
            if (HasStateAuthority)
            {
                FryingTimer = 0f;
                BurningTimer = 0f;
                NetworkStoveState = StoveState.Idle;
            }
        }

        #endregion

        // -------------------------------------------------------
        #region Visual

        private void OnStoveStateChangedVisual(StoveState state)
        {
            if (imageUI == null) return;

            if (state != StoveState.Fried)
            {
                _imageFadeTween?.Kill();
                imageUI.enabled = false;
                _isCompleteUIShown = false;
            }
        }

        private void HandleFriedUI()
        {
            if (imageUI == null) return;

            float burnProgress = burningTimerMax > 0f ? BurningTimer / burningTimerMax : 0f;

            if (burnProgress >= 0.5f)
            {
                if (imageUI.sprite != warningSprite || !imageUI.enabled)
                {
                    _imageFadeTween?.Kill();
                    imageUI.sprite = warningSprite;
                    imageUI.color = Color.white;
                    imageUI.enabled = true;

                    _imageFadeTween = imageUI.DOFade(0.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                }

                float intensity = (burnProgress - 0.5f) / 0.5f;
                if (_imageFadeTween != null) _imageFadeTween.timeScale = Mathf.Lerp(1f, 5f, intensity);
            }
            else
            {
                if (!_isCompleteUIShown)
                {
                    _isCompleteUIShown = true;
                    _imageFadeTween?.Kill();

                    imageUI.sprite = completeSprite;
                    imageUI.color = new Color(1f, 1f, 1f, 0f);
                    imageUI.enabled = true;

                    Sequence completeSequence = DOTween.Sequence();
                    completeSequence.Append(imageUI.DOFade(1f, 0.3f));
                    completeSequence.AppendInterval(1f);
                    completeSequence.Append(imageUI.DOFade(0f, 0.5f));
                    completeSequence.OnComplete(() => { imageUI.enabled = false; });

                    _imageFadeTween = completeSequence;
                }
            }
        }

        #endregion
    }

    public enum StoveState
    {
        Idle, Frying, Fried, Burned
    }
}
