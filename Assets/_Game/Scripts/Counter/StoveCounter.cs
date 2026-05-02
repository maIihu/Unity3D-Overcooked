using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Pooling;
using Kitchen;
using Player;
using GameUI;

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

        private bool _isCompleteUIShown;
        private Tween _imageFadeTween;

        private KitchenType _kitchenType;

        private StoveState _StoveState;
        private StoveState CurrentStoveState
        {
            get => _StoveState;
            set
            {
                _StoveState = value;
                UpdateUIStoveState();
            }
        }

        private void OnDestroy()
        {
            _imageFadeTween?.Kill();
        }

        public void SetStoveData(KitchenType kitchenType)
        {
            _kitchenType = kitchenType;
        }

        public override void Init()
        {
            base.Init();
            CurrentStoveState = StoveState.Idle;
            if (imageUI != null) imageUI.enabled = false;
            
            _kitchenObject = SpawnKitchenObject(_kitchenType);
        }

        private void Update()
        {
            if (_kitchenObject != null)
            {
                PotObject pot = _kitchenObject as PotObject;
                if (pot)
                    switch (_StoveState)
                    {
                        case StoveState.Idle:
                            if (pot.HasIngredients())
                            {
                                if (pot.IsBurned) CurrentStoveState = StoveState.Burned;
                                else if (pot.IsCooked) CurrentStoveState = StoveState.Fried;
                                else CurrentStoveState = StoveState.Frying;
                            }
                            break;

                        case StoveState.Frying:
                            pot.FryingTimer += Time.deltaTime;
                            pot.UpdateCookingProgress(pot.FryingTimer / fryingTimerMax);

                            if (pot.FryingTimer >= fryingTimerMax)
                            {
                                pot.IsCooked = true;
                                CurrentStoveState = StoveState.Fried;
                            }
                            break;

                        case StoveState.Fried:
                            pot.BurningTimer += Time.deltaTime;
                            pot.UpdateCookingProgress(pot.BurningTimer / burningTimerMax);

                            HandleFriedUI();

                            if (pot.BurningTimer >= burningTimerMax)
                            {
                                pot.IsBurned = true;
                                CurrentStoveState = StoveState.Burned;
                            }
                            break;

                        case StoveState.Burned:
                            if (pot != null) pot.UpdateCookingProgress(0f);
                            break;
                    }
            }
            else
            {
                if (_StoveState != StoveState.Idle)
                {
                    CurrentStoveState = StoveState.Idle;
                }
            }
        }

        private void HandleFriedUI()
        {
            if (imageUI == null) return;

            PotObject pot = _kitchenObject as PotObject;
            if (pot == null) return;

            float burnProgress = pot.BurningTimer / burningTimerMax;

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
                _imageFadeTween.timeScale = Mathf.Lerp(1f, 5f, intensity);
            }
            else
            {
                if (!_isCompleteUIShown)
                {
                    _isCompleteUIShown = true;
                    _imageFadeTween?.Kill();

                    imageUI.sprite = completeSprite;
                    imageUI.color = new Color(1, 1, 1, 0);
                    imageUI.enabled = true;

                    Sequence completeSequence = DOTween.Sequence();
                    completeSequence.Append(imageUI.DOFade(1f, 0.3f));
                    completeSequence.AppendInterval(1f);
                    completeSequence.Append(imageUI.DOFade(0f, 0.5f));
                    completeSequence.OnComplete(() =>
                    {
                        imageUI.enabled = false;
                    });

                    _imageFadeTween = completeSequence;
                }
            }
        }

        private void UpdateUIStoveState()
        {
            if (imageUI == null) return;

            if (_StoveState != StoveState.Fried)
            {
                _imageFadeTween?.Kill();
                imageUI.enabled = false;
                _isCompleteUIShown = false;
            }
        }

        public override void Interact(Player.Player player)
        {
            if (HasKitchenObject())
            {
                if (!player.HasKitchenObject())
                {
                    GetKitchenObject().SetKitchenObjectParent(player);
                }
                else
                {
                    if (_kitchenObject != null)
                    {
                        PotObject pot = _kitchenObject as PotObject;

                        if (player.GetKitchenObject() is FoodObject { FoodState: FoodState.Cut } food && pot.CanAddIngredient(food))
                        {
                            pot.OnIngredientAdded(food);
                            CurrentStoveState = StoveState.Frying;
                            food.DestroySelf();
                        }
                        else if (player.GetKitchenObject() is PlateObject plate && pot.IsCooked && !pot.IsBurned && pot.IsFull())
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
                                CurrentStoveState = StoveState.Idle;
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
            this._kitchenObject = kitchenObject as PotObject;

            _isCompleteUIShown = false;
            _imageFadeTween?.Kill();

            if (kitchenObject != null)
            {
                PotObject pot = kitchenObject as PotObject;
                if(pot != null)
                {
                    pot.BurningTimerMax = burningTimerMax;
                    if (pot.HasIngredients())
                    {
                        if (pot.IsBurned) CurrentStoveState = StoveState.Burned;
                        else if (pot.IsCooked) CurrentStoveState = StoveState.Fried;
                        else CurrentStoveState = StoveState.Frying;
                    }
                    else
                    {
                        CurrentStoveState = StoveState.Idle;
                    }
                }
            }
            else
            {
                CurrentStoveState = StoveState.Idle;
            }
        }

        public override void ClearKitchenObject()
        {
            base.ClearKitchenObject();
            this._kitchenObject = null;
            _imageFadeTween?.Kill();
            CurrentStoveState = StoveState.Idle;
        }
    }

    public enum StoveState
    {
        Idle, Frying, Fried, Burned
    }
    
}
