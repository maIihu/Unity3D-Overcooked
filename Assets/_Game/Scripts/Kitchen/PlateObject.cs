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
        
        private List<EFoodType> _ingredientList = new List<EFoodType>();
        
        public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;
        public class OnIngredientAddedEventArgs : EventArgs
        {
            public EFoodType eFoodType;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            _ingredientList.Clear();
        }

        public bool TryAddIngredient(FoodObject foodObject)
        {
            if (!validIngredientList.Contains(foodObject.EFoodType))
            {
                return false;
            }

            _ingredientList.Add(foodObject.EFoodType);

            OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
            {
                eFoodType = foodObject.EFoodType
            });
            
            return true;
        }

        public bool TryAddIngredient(EFoodType foodType)
        {
            if (!validIngredientList.Contains(foodType))
            {
                return false;
            }

            _ingredientList.Add(foodType);

            OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
            {
                eFoodType = foodType
            });
            
            return true;
        }

        public List<EFoodType> GetIngredientList() => _ingredientList;

    }
}
