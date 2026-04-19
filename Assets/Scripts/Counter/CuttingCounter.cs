using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttingCounter : BaseCounter, IKitchenObjectParent
{
    [SerializeField] private float cuttingTime;
    [SerializeField] private ProgressBarUI progressBarUI;

    public event Action OnCutComplete;

    private FoodObject _kitchenObject;
    private float _cuttingProgress;
    //[SerializeField] private Animator _ani;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Interact(Player player)
    {
        base.Interact(player);
        if (HasKitchenObject())
        {
            // Kiểm tra nếu đang cắt (tiến trình > 0) thì không cho lấy ra
            if (_cuttingProgress > 0) return;

            _kitchenObject.SetKitchenObjectParent(player);

            progressBarUI?.UpdateProgress(0f);
        }
        else
        {
            if (player.HasKitchenObject())
            {
                player.GetKitchenObject().SetKitchenObjectParent(this);
                _cuttingProgress = 0f;

                progressBarUI?.UpdateProgress(0f);
            }
        }
    }

    public override void InteractAlternate(Player player)
    {
        base.InteractAlternate(player);
        if (HasKitchenObject())
        {
            _cuttingProgress += Time.deltaTime;

            progressBarUI?.UpdateProgress(_cuttingProgress / cuttingTime);

            if (_cuttingProgress >= cuttingTime)
            {
                _cuttingProgress = 0f;
                _kitchenObject.Cut();
                OnCutComplete?.Invoke();

                progressBarUI?.UpdateProgress(0f);
            }
        }

    }

    public void CuttingSoundAndAnimation()
    {
        //_ani.SetBool(ContainString.Cut, true);
    }

    public void StopAnimationCut()
    {
        //_ani.SetBool(ContainString.Cut, false);
    }


    #region IKitchenObjectParent

    public Transform GetKitchenObjectToTransform()
    {
        return CounterTopPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this._kitchenObject = kitchenObject as FoodObject;
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
