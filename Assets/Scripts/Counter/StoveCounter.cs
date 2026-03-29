using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounter : BaseCounter, IKitchenObjectParent
{
    private enum State
    {
        Idle, Frying, Fried, Burned
    }

    [SerializeField] private ProgressBarUI progressBarUI;
    [SerializeField] private PotObject potObject;
    [SerializeField] private Transform potPoint;

    [SerializeField] private float fryingTimerMax = 4f;
    [SerializeField] private float burningTimerMax = 5f;

    private KitchenObject _kitchenObject;

    private State _state;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        _state = State.Idle;
        HideEffect();
        var go = Instantiate(potObject, potPoint.position, Quaternion.identity);
        go.SetKitchenObjectParent(this);
    }

    private void Update()
    {
        if (HasKitchenObject() && GetKitchenObject() is PotObject pot)
        {
            switch (_state)
            {
                case State.Idle:
                    if (pot.HasIngredients())
                    {
                        if (pot.IsBurned) _state = State.Burned;
                        else if (pot.IsCooked) _state = State.Fried;
                        else _state = State.Frying;
                    }
                    break;
                case State.Frying:
                    pot.FryingTimer += Time.deltaTime;

                    if (progressBarUI == null) Debug.LogWarning("Chưa gán ProgressBarUI vào StoveCounter!");
                    progressBarUI?.UpdateProgress(pot.FryingTimer / fryingTimerMax);

                    if (pot.FryingTimer >= fryingTimerMax)
                    {
                        _state = State.Fried;
                        pot.IsCooked = true;
                        Debug.Log("StoveCounter: Nấu xong! Chuyển sang đợi khét.");

                        if (pot.HasKitchenObject() && pot.GetKitchenObject() is FoodObject food)
                        {
                            food.Fried();
                        }
                    }
                    break;
                case State.Fried:
                    pot.BurningTimer += Time.deltaTime;
                    progressBarUI?.UpdateProgress(pot.BurningTimer / burningTimerMax);

                    if (pot.BurningTimer >= burningTimerMax)
                    {
                        _state = State.Burned;
                        pot.IsBurned = true;
                        Debug.Log("StoveCounter: Khét lẹt!!!");

                        if (pot.HasKitchenObject() && pot.GetKitchenObject() is FoodObject food)
                        {
                            food.Burned();
                        }
                    }
                    break;
                case State.Burned:
                    progressBarUI?.UpdateProgress(0f);
                    break;
            }
        }
        else
        {
            if (_state != State.Idle)
            {
                _state = State.Idle;
                progressBarUI?.UpdateProgress(0f);
            }
        }
    }

    private void ShowEffect()
    {

    }

    private void HideEffect()
    {

    }

    public override void Interact(Player player)
    {
        base.Interact(player);
        if (HasKitchenObject())
        {
            if (!player.HasKitchenObject())
            {
                GetKitchenObject().SetKitchenObjectParent(player);
                _state = State.Idle;
                progressBarUI?.UpdateProgress(0f);
                HideEffect();
            }
            else
            {
                if (GetKitchenObject() is PotObject pot)
                {
                    if (player.GetKitchenObject() is FoodObject { FoodState: FoodState.Cut } food && pot.CanAddIngredient())
                    {
                        if (!pot.HasKitchenObject())
                        {
                            food.SetKitchenObjectParent(pot);
                        }
                        else
                        {
                            food.DestroySelf();
                        }

                        pot.OnIngredientAdded();
                        food.Soup();

                        // Nếu bỏ món mới trong lúc đang nấu hoặc nấu xong => Reset timer nấu (đã xử lý trong OnIngredientAdded của Pot)
                        _state = State.Frying;
                        Debug.Log("StoveCounter: Đã bỏ thêm nguyên liệu, nấu lại từ đầu!");
                    }
                    else if (player.GetKitchenObject() is PlateObject plate && pot.IsCooked && !pot.IsBurned && pot.IsFull())
                    {
                        if (pot.HasKitchenObject() && !plate.HasKitchenObject())
                        {
                            KitchenObject potFood = pot.GetKitchenObject();
                            potFood.SetKitchenObjectParent(plate);
                            pot.EmptyPot();
                            _state = State.Idle;
                            progressBarUI?.UpdateProgress(0f);
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
                if (pot.HasIngredients())
                {
                    _state = State.Frying;
                    Debug.Log("StoveCounter: Bắt đầu nấu!");

                }
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
    }

    public KitchenObject GetKitchenObject()
    {
        return this._kitchenObject;
    }

    public void ClearKitchenObject()
    {
        this._kitchenObject = null;
    }

    public bool HasKitchenObject()
    {
        return this._kitchenObject != null;
    }

    #endregion

}
