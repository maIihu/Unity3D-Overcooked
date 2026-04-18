using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StoveCounter : BaseCounter, IKitchenObjectParent
{
    private enum State
    {
        Idle, Frying, Fried, Burned
    }

    [SerializeField] private PotObject potObjectPrefab;
    [SerializeField] private Transform potPoint;

    [SerializeField] private float fryingTimerMax = 4f;
    [SerializeField] private float burningTimerMax = 5f;

    [SerializeField] private Sprite completeSprite;
    [SerializeField] private Sprite warningSprite;

    [SerializeField] private Image imageUI;

    private KitchenObject _kitchenObject;
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

    protected override void Awake()
    {
        base.Awake();
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

        // Tự động tạo nồi khi bắt đầu nếu được gán prefab
        if (potObjectPrefab != null)
        {
            var go = Instantiate(potObjectPrefab, potPoint.position, Quaternion.identity);
            go.SetKitchenObjectParent(this);
        }
    }

    private void Update()
    {
        // Sử dụng cache _currentPot để tối ưu, không cần cast mỗi frame
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
                        Debug.Log("StoveCounter: Nấu xong! Chuyển sangFried.");

                        if (_currentPot.HasKitchenObject() && _currentPot.GetKitchenObject() is FoodObject food)
                        {
                            food.Fried();
                        }
                    }
                    break;

                case State.Fried:
                    _currentPot.BurningTimer += Time.deltaTime;
                    _currentPot.UpdateCookingProgress(_currentPot.BurningTimer / burningTimerMax);

                    // Xử lý UI Warning / Complete
                    HandleFriedUI();

                    if (_currentPot.BurningTimer >= burningTimerMax)
                    {
                        _currentPot.IsBurned = true;
                        CurrentState = State.Burned;
                        Debug.Log("StoveCounter: Khét lẹt!!!");

                        if (_currentPot.HasKitchenObject() && _currentPot.GetKitchenObject() is FoodObject food)
                        {
                            food.Burned();
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
            // Trạng thái gần cháy: Sử dụng DOTween để nhấp nháy, càng gần cháy càng nhanh
            if (imageUI.sprite != warningSprite || !imageUI.enabled)
            {
                _imageFadeTween?.Kill();
                imageUI.sprite = warningSprite;
                imageUI.color = Color.white;
                imageUI.enabled = true;

                // Khởi tạo tween nhấp nháy (Yoyo)
                _imageFadeTween = imageUI.DOFade(0.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }

            // Tăng tốc độ nhấp nháy dựa trên tiến trình cháy (từ 0.5 đến 1.0)
            float intensity = (burnProgress - 0.5f) / 0.5f; // 0 -> 1
            _imageFadeTween.timeScale = Mathf.Lerp(1f, 5f, intensity); // Tốc độ nhanh dần từ x1 đến x5
        }
        else
        {
            // Trạng thái nấu thành công: Hiện dần completeSprite rồi mờ dần
            if (!_isCompleteUIShown)
            {
                _isCompleteUIShown = true;
                _imageFadeTween?.Kill();

                imageUI.sprite = completeSprite;
                imageUI.color = new Color(1, 1, 1, 0); // Bắt đầu từ trong suốt
                imageUI.enabled = true;

                // Tạo chuỗi hiệu ứng: Hiện dần -> Chờ -> Mờ dần
                Sequence completeSequence = DOTween.Sequence();
                completeSequence.Append(imageUI.DOFade(1f, 0.3f)); // Hiện dần trong 0.3s
                completeSequence.AppendInterval(1f);              // Chờ 1s
                completeSequence.Append(imageUI.DOFade(0f, 0.5f)); // Mờ dần trong 0.5s
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

        // Reset trạng thái UI thông báo (icon hoàn thành/cảnh báo)
        if (_state != State.Fried)
        {
            _imageFadeTween?.Kill();
            imageUI.enabled = false;
            _isCompleteUIShown = false;
        }
    }

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
                        food.Soup();

                        CurrentState = State.Frying;
                        Debug.Log("StoveCounter: Đã bỏ thêm nguyên liệu, nấu lại!");
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

    #region IKitchenObjectParent

    public Transform GetKitchenObjectToTransform()
    {
        return CounterTopPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this._kitchenObject = kitchenObject;
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

    public KitchenObject GetKitchenObject()
    {
        return this._kitchenObject;
    }

    public void ClearKitchenObject()
    {
        this._kitchenObject = null;
        this._currentPot = null;
        _imageFadeTween?.Kill();
        CurrentState = State.Idle;
    }

    public bool HasKitchenObject()
    {
        return this._kitchenObject != null;
    }

    #endregion
}
