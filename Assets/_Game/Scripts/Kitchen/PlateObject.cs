using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

namespace Kitchen
{
    public class PlateObject : KitchenObject
    {
        [SerializeField] private Transform topPoint;
        [SerializeField] private List<EFoodType> validIngredientList;
        [SerializeField] private GameObject liquidGO;
        
        [SerializeField] private GameObject dirtyVisual;
        
        private List<EFoodType> _ingredientList = new List<EFoodType>();
        [Networked, OnChangedRender(nameof(OnDirtyStateChanged))] 
        private NetworkBool IsDirtyState { get; set; }
        
        public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;
        public class OnIngredientAddedEventArgs : EventArgs
        {
            public EFoodType eFoodType;
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            liquidGO.SetActive(false);
            SetDirty(false);
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            _ingredientList.Clear();
        }

        public bool TryAddIngredient(FoodObject foodObject)
        {
            if (IsDirtyState || !validIngredientList.Contains(foodObject.EFoodType))
            {
                return false;
            }

            _ingredientList.Add(foodObject.EFoodType);
            if(foodObject.EFoodType == EFoodType.Onion) liquidGO.SetActive(true);

            OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
            {
                eFoodType = foodObject.EFoodType
            });
            
            return true;
        }

        public bool TryAddIngredient(EFoodType foodType)
        {
            if (IsDirtyState || !validIngredientList.Contains(foodType))
            {
                return false;
            }

            _ingredientList.Add(foodType);
            if(foodType == EFoodType.Onion) liquidGO.SetActive(true);


            OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
            {
                eFoodType = foodType
            });
            
            return true;
        }

        public List<EFoodType> GetIngredientList() => _ingredientList;

        public bool IsDirty() => IsDirtyState;

        public void SetDirty(bool dirty)
        {
            if (HasStateAuthority)
            {
                IsDirtyState = dirty;
            }
            // Update local visual as well
            if (dirtyVisual != null) dirtyVisual.SetActive(dirty);
        }

        private void OnDirtyStateChanged()
        {
            if (dirtyVisual != null) dirtyVisual.SetActive(IsDirtyState);
        }
    }
}
