
using System;
using UnityEngine;

public class PotObject : KitchenObject, IKitchenObjectParent
{
    [SerializeField] private float liquidHeight;
    [SerializeField] private GameObject liquidGO;
    [SerializeField] private Color burnedColor = new Color(0.2f, 0.2f, 0.2f);

    [SerializeField] private ParticleSystem steamCookingEffect;
    [SerializeField] private ParticleSystem burnedCookingEffect;

    private Material _liquidMaterial;
    private Color _defaultLiquidColor;
    private bool _isBurned;

    public bool IsCooked { get; set; }
    public bool IsBurned 
    { 
        get => _isBurned; 
        set 
        {
            _isBurned = value;
            if (_liquidMaterial != null)
            {
                _liquidMaterial.color = _isBurned ? burnedColor : _defaultLiquidColor;
            }
        }
    }

    public float FryingTimer { get; set; }
    public float BurningTimer { get; set; }

    private const int MaxCapacity = 3;

    private int _currentCount;

    [SerializeField] private Transform topPoint;
    private KitchenObject _kitchenObject;

    private void Awake()
    {
        if (liquidGO != null && liquidGO.TryGetComponent<Renderer>(out var renderer))
        {
            _liquidMaterial = renderer.material;
            _defaultLiquidColor = _liquidMaterial.color;
        }
    }

    private void Update()
    {
        // Tự động điều khiển VFX dựa trên trạng thái của nồi
        bool isOnStove = GetKitchenObjectParent() is StoveCounter;
        
        // Hiệu ứng khói: Chỉ hiển thị khi đã nấu chín nhưng chưa cháy, và phải đang ở trên bếp
        bool shouldShowSteam = isOnStove && HasIngredients() && !IsBurned && IsCooked;
        PlayFryingEffect(shouldShowSteam);

        // Hiệu ứng cháy: Khi đã bị cháy (vẫn hiện kể cả khi nhấc ra khỏi bếp)
        bool shouldShowBurned = IsBurned;
        PlayBurnedEffect(shouldShowBurned);
    }

    public void PlayFryingEffect(bool isPlaying)
    {
        if (steamCookingEffect == null) return;
        if (isPlaying)
        {
            if (!steamCookingEffect.isPlaying) steamCookingEffect.Play();
        }
        else
        {
            steamCookingEffect.Stop();
        }
    }

    public void PlayBurnedEffect(bool isPlaying)
    {
        if (burnedCookingEffect == null) return;
        if (isPlaying)
        {
            if (!burnedCookingEffect.isPlaying) burnedCookingEffect.Play();
        }
        else
        {
            burnedCookingEffect.Stop();
        }
    }

    public void HideEffects()
    {
        if (steamCookingEffect != null) steamCookingEffect.Stop();
        if (burnedCookingEffect != null) burnedCookingEffect.Stop();
    }

    public bool CanAddIngredient()
    {
        return _currentCount < MaxCapacity && !IsBurned;
    }

    public void OnIngredientAdded()
    {
        _currentCount++;
        liquidGO.transform.localPosition = Vector3.up * (liquidHeight * _currentCount);
        IsCooked = false;
        IsBurned = false;
        FryingTimer = 0f;
        BurningTimer = 0f;
    }

    public bool IsFull()
    {
        return _currentCount >= MaxCapacity;
    }

    public bool HasIngredients()
    {
        return _currentCount > 0;
    }


    public void EmptyPot()
    {
        _currentCount = 0;
        IsCooked = false;
        IsBurned = false;
        FryingTimer = 0f;
        BurningTimer = 0f;
        liquidGO.transform.localPosition = Vector3.zero;
    }

    #region IKitchenObjectParent

    public Transform GetKitchenObjectToTransform()
    {
        return topPoint;
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
