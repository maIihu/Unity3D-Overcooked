using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounter : BaseCounter, IKitchenObjectParent, IHasProgress
{
    private enum State
    {
        Idle, Frying, Fried, Burned
    }
    
    public event EventHandler<IHasProgress.OnProgressBarChangedEventArgs> OnProgressBarChanged;
    
    [SerializeField] private FryingRecipeSO[] fryingRecipeSOArray;
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;
    
    private KitchenObject _kitchenObject;
    
    private State _state;
    private FryingRecipeSO _fryingRecipeSO;
    private float _fryingTimer;
    private BurningRecipeSO _burningRecipeSO;
    private float _burningTimer;

    private GameObject _sizzlingEffect;
    private GameObject _burningEffect;
    private AudioSource _audioSource;

    protected override void Awake()
    {
        base.Awake();
        _sizzlingEffect = this.transform.Find("StoveCounter_Visual/SizzlingParticles").gameObject;
        _burningEffect = this.transform.Find("StoveCounter_Visual/StoveOnVisual").gameObject;
        _audioSource = this.GetComponentInChildren<AudioSource>();
    }

    protected override void Start()
    {
        base.Start();
        _state = State.Idle;
        HideEffect();
    }

    private void Update()
    {
        if (HasKitchenObject())
        {
            switch (_state)
            {
                case State.Idle:
                    break;
                case State.Frying:
                    _fryingTimer += Time.deltaTime;
                    
                    OnProgressBarChanged?.Invoke(this, new IHasProgress.OnProgressBarChangedEventArgs()
                    {
                        progressNormalized = _fryingTimer /  _fryingRecipeSO.fryingTimerMax
                    });
                    
                    if (_fryingTimer > _fryingRecipeSO.fryingTimerMax)
                    {
                        GetKitchenObject().DestroySelf();
                        //KitchenObject.SpawnKitchenObject(_fryingRecipeSO.output, this);
                        var go = Instantiate(_fryingRecipeSO.output.prefab);
                        go.Init(_fryingRecipeSO.output);
                        go.SetKitchenObjectParent(this);
                        _state = State.Fried;
                        _burningTimer = 0;
                        _burningRecipeSO = GetBurningRecipeSOForInput(GetKitchenObject().GetDataObjectSo);
                    }
                    break;
                case State.Fried:
                    _burningTimer += Time.deltaTime;
                    
                    OnProgressBarChanged?.Invoke(this, new IHasProgress.OnProgressBarChangedEventArgs()
                    {
                        progressNormalized = _burningTimer /  _burningRecipeSO.burningTimerMax
                    });
                    
                    if (_burningTimer > _burningRecipeSO.burningTimerMax)
                    {
                        GetKitchenObject().DestroySelf();
                        //KitchenObject.SpawnKitchenObject(_burningRecipeSO.output, this);
                        var go = Instantiate(_burningRecipeSO.output.prefab);
                        go.Init(_burningRecipeSO.output);
                        go.SetKitchenObjectParent(this);
                        _state = State.Burned;
                        
                        OnProgressBarChanged?.Invoke(this, new IHasProgress.OnProgressBarChangedEventArgs()
                        {
                            progressNormalized = 0
                        });
                    }
                    break;
                case State.Burned: 
                    break;
            }
        }
    }

    private void ShowEffect()
    {
        _sizzlingEffect.SetActive(true);
        _burningEffect.SetActive(true);
        _audioSource.Play();
    }

    private void HideEffect()
    {
        _sizzlingEffect.SetActive(false);
        _burningEffect.SetActive(false);
        _audioSource.Stop();
    }

    public override void Interact(Player player)
    {
        base.Interact(player);
        if (HasKitchenObject())
        {
            if(!player.HasKitchenObject())
            {
                GetKitchenObject().SetKitchenObjectParent(player);
                _state = State.Idle;
                OnProgressBarChanged?.Invoke(this, new IHasProgress.OnProgressBarChangedEventArgs()
                {
                    progressNormalized = 0
                });
                HideEffect();
            }
            else
            {
                if (player.GetKitchenObject() is PlateKitchenObject plateKitchenObject)
                {
                    if(plateKitchenObject.TryAddIngredient(GetKitchenObject().GetDataObjectSo))
                    {
                        GetKitchenObject().DestroySelf();
                        _state = State.Idle;
                        OnProgressBarChanged?.Invoke(this, new IHasProgress.OnProgressBarChangedEventArgs()
                        {
                            progressNormalized = 0
                        });
                        HideEffect();
                    }
                }
            }
        }
        else
        {
            if (player.HasKitchenObject())
            {
                var fryingRecipeSO = GetPryingRecipeSOForInput(player.GetKitchenObject().GetDataObjectSo);
                if(fryingRecipeSO)
                {
                    SoundManagerScript.PlaySound(SoundManagerScript.GetAudioClipRefesSO().objectDrop, this.transform.position);
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                    _fryingRecipeSO = fryingRecipeSO;
                    _state = State.Frying;
                    ShowEffect();
                    _fryingTimer = 0;
                    OnProgressBarChanged?.Invoke(this, new IHasProgress.OnProgressBarChangedEventArgs()
                    {
                        progressNormalized = 0
                    });
                }
            }
        }
    }
    
    private FryingRecipeSO GetPryingRecipeSOForInput(KitchenObjectSO kitchenObjectSO)
    {
        foreach (var fryingRecipe in fryingRecipeSOArray)
        {
            if(fryingRecipe.input == kitchenObjectSO) 
                return fryingRecipe;
        }
        return null;
    }
    
    private BurningRecipeSO GetBurningRecipeSOForInput(KitchenObjectSO kitchenObjectSO)
    {
        foreach (var burningRecipe in burningRecipeSOArray)
        {
            if(burningRecipe.input == kitchenObjectSO) 
                return burningRecipe;
        }
        return null;
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
