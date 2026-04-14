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

    [SerializeField] private ParticleSystem steamCookingEffect;
    [SerializeField] private ParticleSystem burnedCookingEffect;

    private KitchenObject _kitchenObject;

    private State _state;
    private State CurrentState
    {
        get => _state;
        set
        {
            _state = value;
            ShowEffect();
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        CurrentState = State.Idle;
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
                        if (pot.IsBurned) CurrentState = State.Burned;
                        else if (pot.IsCooked) CurrentState = State.Fried;
                        else CurrentState = State.Frying;
                    }
                    break;
                case State.Frying:
                    pot.FryingTimer += Time.deltaTime;

                    if (progressBarUI == null) Debug.LogWarning("Chưa gán ProgressBarUI vào StoveCounter!");
                    progressBarUI?.UpdateProgress(pot.FryingTimer / fryingTimerMax);

                    if (pot.FryingTimer >= fryingTimerMax)
                    {
                        CurrentState = State.Fried;
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
                        CurrentState = State.Burned;
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
                CurrentState = State.Idle;
                progressBarUI?.UpdateProgress(0f);
            }
        }
    }

    private void ShowEffect()
    {
        if (steamCookingEffect != null) 
        {
            if (_state == State.Fried)
            {
                if (!steamCookingEffect.isPlaying) steamCookingEffect.Play();
            }
            else
            {
                steamCookingEffect.Stop();
            }
        }
        
        if (burnedCookingEffect != null) 
        {
            if (_state == State.Burned)
            {
                if (!burnedCookingEffect.isPlaying) burnedCookingEffect.Play();
            }
            else
            {
                burnedCookingEffect.Stop();
            }
        }
    }

    private void HideEffect()
    {
        if (steamCookingEffect != null) steamCookingEffect.Stop();
        if (burnedCookingEffect != null) burnedCookingEffect.Stop();
    }

    public override void Interact(Player player)
    {
        base.Interact(player);
        if (HasKitchenObject())
        {
            if (!player.HasKitchenObject())
            {
                GetKitchenObject().SetKitchenObjectParent(player);
                CurrentState = State.Idle;
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
                        CurrentState = State.Frying;
                        Debug.Log("StoveCounter: Đã bỏ thêm nguyên liệu, nấu lại từ đầu!");
                    }
                    else if (player.GetKitchenObject() is PlateObject plate && pot.IsCooked && !pot.IsBurned && pot.IsFull())
                    {
                        if (pot.HasKitchenObject() && !plate.HasKitchenObject())
                        {
                            KitchenObject potFood = pot.GetKitchenObject();
                            potFood.SetKitchenObjectParent(plate);
                            pot.EmptyPot();
                            CurrentState = State.Idle;
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
                    CurrentState = State.Frying;
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
