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
        private enum State
        {
            Idle, Frying, Fried, Burned
        }

        [SerializeField] private Transform potPoint;

        [SerializeField] private float fryingTimerMax = 4f;
        [SerializeField] private float burningTimerMax = 5f;

        [SerializeField] private Sprite completeSprite;
        [SerializeField] private Sprite warningSprite;

        [SerializeField] private Image imageUI;

        private PotObject _currentPot;
        private bool _isCompleteUIShown;
        private Tween _imageFadeTween;

        private State _state;
        private State CurrentState
        {
            get => _state;
            set
            {
                _state = value;
                UpdateUIState();
            }
        }

        private void OnDestroy()
        {
            _imageFadeTween?.Kill();
        }

        protected override void Start()
        {
            base.Start();
            CurrentState = State.Idle;
            if (imageUI != null) imageUI.enabled = false;

            // Use the new pooling helper from BaseCounter
            var pot = SpawnKitchenObject(KitchenType.Pot);
            pot.transform.position = potPoint.position;
        }

        private void Update()
        {
            if (_currentPot != null)
            {
                switch (_state)
                {
                    case State.Idle:
                        if (_currentPot.HasIngredients())
                        {
                            if (_currentPot.IsBurned) CurrentState = State.Burned;
                            else if (_currentPot.IsCooked) CurrentState = State.Fried;
                            else CurrentState = State.Frying;
                        }
                        break;

                    case State.Frying:
                        _currentPot.FryingTimer += Time.deltaTime;
                        _currentPot.UpdateCookingProgress(_currentPot.FryingTimer / fryingTimerMax);

                        if (_currentPot.FryingTimer >= fryingTimerMax)
                        {
                            _currentPot.IsCooked = true;
                            CurrentState = State.Fried;

                            if (_currentPot.HasKitchenObject() && _currentPot.GetKitchenObject() is FoodObject food)
                            {
                                food.SetState(FoodState.Fried);
                            }
                        }
                        break;

                    case State.Fried:
                        _currentPot.BurningTimer += Time.deltaTime;
                        _currentPot.UpdateCookingProgress(_currentPot.BurningTimer / burningTimerMax);

                        HandleFriedUI();

                        if (_currentPot.BurningTimer >= burningTimerMax)
                        {
                            _currentPot.IsBurned = true;
                            CurrentState = State.Burned;

                            if (_currentPot.HasKitchenObject() && _currentPot.GetKitchenObject() is FoodObject food)
                            {
                                food.SetState(FoodState.Burned);
                            }
                        }
                        break;

                    case State.Burned:
                        if (_currentPot != null) _currentPot.UpdateCookingProgress(0f);
                        break;
                }
            }
            else
            {
                if (_state != State.Idle)
                {
                    CurrentState = State.Idle;
                }
            }
        }

        private void HandleFriedUI()
        {
            if (imageUI == null) return;

            float burnProgress = _currentPot.BurningTimer / burningTimerMax;

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

        private void UpdateUIState()
        {
            if (imageUI == null) return;

            if (_state != State.Fried)
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
                    if (_currentPot != null)
                    {
                        if (player.GetKitchenObject() is FoodObject { FoodState: FoodState.Cut } food && _currentPot.CanAddIngredient())
                        {
                            if (!_currentPot.HasKitchenObject())
                            {
                                food.SetKitchenObjectParent(_currentPot);
                            }
                            else
                            {
                                food.DestroySelf();
                            }

                            _currentPot.OnIngredientAdded();
                            food.SetState(FoodState.Soup);

                            CurrentState = State.Frying;
                        }
                        else if (player.GetKitchenObject() is PlateObject plate && _currentPot.IsCooked && !_currentPot.IsBurned && _currentPot.IsFull())
                        {
                            if (_currentPot.HasKitchenObject() && !plate.HasKitchenObject())
                            {
                                KitchenObject potFood = _currentPot.GetKitchenObject();
                                potFood.SetKitchenObjectParent(plate);
                                _currentPot.EmptyPot();
                                CurrentState = State.Idle;
                            }
                        }
                    }
                }
            }
            else
            {
                if (player.HasKitchenObject() && player.GetKitchenObject() is PotObject pot)
                {
                    pot.SetKitchenObjectParent(this);
                }
            }
        }

        public override void SetKitchenObject(KitchenObject kitchenObject)
        {
            base.SetKitchenObject(kitchenObject);
            this._currentPot = kitchenObject as PotObject;

            _isCompleteUIShown = false;
            _imageFadeTween?.Kill();

            if (_currentPot != null)
            {
                _currentPot.BurningTimerMax = burningTimerMax;
                if (_currentPot.HasIngredients())
                {
                    if (_currentPot.IsBurned) CurrentState = State.Burned;
                    else if (_currentPot.IsCooked) CurrentState = State.Fried;
                    else CurrentState = State.Frying;
                }
                else
                {
                    CurrentState = State.Idle;
                }
            }
            else
            {
                CurrentState = State.Idle;
            }
        }

        public override void ClearKitchenObject()
        {
            base.ClearKitchenObject();
            this._currentPot = null;
            _imageFadeTween?.Kill();
            CurrentState = State.Idle;
        }
    }
}
