using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttingCounter : BaseCounter, IKitchenObjectParent, IHasProgress
{
    public event EventHandler<IHasProgress.OnProgressBarChangedEventArgs> OnProgressBarChanged;

    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray;
    
    private KitchenObject _kitchenObject;
    private float _cuttingProgress;
    private Animator _ani;

    protected override void Awake()
    {
        base.Awake();
        _ani = GetComponentInChildren<Animator>();
    }

    public override void Interact(Player player)
    { 
        base.Interact(player);

        if (HasKitchenObject())
        {
            if(!GetCuttingRecipeSOForInput(_kitchenObject.GetDataObjectSo))
            {
                if(!player.HasKitchenObject())
                {
                    _kitchenObject.SetKitchenObjectParent(player);
                }
                else
                {
                    if (player.GetKitchenObject() is PlateKitchenObject plateKitchenObject)
                    {
                        if(plateKitchenObject.TryAddIngredient(GetKitchenObject().GetDataObjectSo))
                        {
                            GetKitchenObject().DestroySelf();
                        }
                    }
                }
            }
        }
        else
        {
            if (player.HasKitchenObject() && GetCuttingRecipeSOForInput(player.GetKitchenObject().GetDataObjectSo))
            { // cut
                player.GetKitchenObject().SetKitchenObjectParent(this);
                SoundManagerScript.PlaySound(SoundManagerScript.GetAudioClipRefesSO().objectDrop, this.transform.position);
                _cuttingProgress = 0;
                OnProgressBarChanged?.Invoke(this, 
                    new IHasProgress.OnProgressBarChangedEventArgs() { progressNormalized = 0 });
            }
        }
    }
    
    public override void InteractAlternate(Player player)
    {
        base.InteractAlternate(player);
        if (HasKitchenObject())
        {
            var cuttingRecipe = GetCuttingRecipeSOForInput(GetKitchenObject().GetDataObjectSo);
            if (cuttingRecipe)
            {
                _cuttingProgress += Time.deltaTime;
                OnProgressBarChanged?.Invoke(this, 
                    new IHasProgress.OnProgressBarChangedEventArgs() 
                        { progressNormalized = _cuttingProgress/cuttingRecipe.cuttingTimerMax });
                if (_cuttingProgress >= cuttingRecipe.cuttingTimerMax)
                {
                    _ani.SetBool(ContainString.Cut, false);
                    GetKitchenObject().DestroySelf();
                    //KitchenObject.SpawnKitchenObject(cuttingRecipe.output, this);
                    var go = Instantiate(cuttingRecipe.output.prefab);
                    go.Init(cuttingRecipe.output);
                    go.SetKitchenObjectParent(this);
                }
            }
        }
    }

    public void CuttingSoundAndAnimation()
    {
        _ani.SetBool(ContainString.Cut, true);
        SoundManagerScript.PlaySound(SoundManagerScript.GetAudioClipRefesSO().chop, this.transform.position);
    }
    
    public void StopAnimationCut()
    {
        _ani.SetBool(ContainString.Cut, false);
    }
    
    private CuttingRecipeSO GetCuttingRecipeSOForInput(KitchenObjectSO kitchenObjectSO)
    {
        foreach (var cuttingRecipeSo in cuttingRecipeSOArray)
        {
            if(cuttingRecipeSo.input == kitchenObjectSO) 
                return cuttingRecipeSo;
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
