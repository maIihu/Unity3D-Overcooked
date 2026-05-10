using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kitchen
{
    public class PlateObject : KitchenObject
    {
        [SerializeField] private Transform topPoint;
        [SerializeField] private List<EFoodType> validIngredientList;
        [SerializeField] private GameObject liquidGO;
        
        [SerializeField] private GameObject dirtyVisual;
        
        private List<EFoodType> _ingredientList = new List<EFoodType>();
        private bool _isDirty;
        
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
            if (_isDirty || !validIngredientList.Contains(foodObject.EFoodType))
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
            if (_isDirty || !validIngredientList.Contains(foodType))
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

        public bool IsDirty() => _isDirty;

        public void SetDirty(bool dirty)
        {
            _isDirty = dirty;
            if (dirtyVisual != null) dirtyVisual.SetActive(dirty);
        }
    }
}
