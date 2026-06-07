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
            _isOffline = GameCore.GameManager.Instance != null && GameCore.GameManager.Instance.IsOffline;
            liquidGO.SetActive(false);
            if (dirtyVisual != null) dirtyVisual.SetActive(_isOffline ? _offlineIsDirtyState : (bool)IsDirtyState);
        }

        // ── Offline local state ──
        private bool _offlineIsDirtyState;

        private void Start()
        {
            if (GameCore.GameManager.Instance != null && GameCore.GameManager.Instance.IsOffline)
            {
                _isOffline = true;
                _offlineIsDirtyState = false; // default
                if (dirtyVisual != null) dirtyVisual.SetActive(_offlineIsDirtyState);
            }
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            _ingredientList.Clear();
        }

        public bool TryAddIngredient(FoodObject foodObject)
        {
            if (IsDirty() || !validIngredientList.Contains(foodObject.EFoodType))
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
            if (IsDirty() || !validIngredientList.Contains(foodType))
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

        public bool IsDirty() => _isOffline ? _offlineIsDirtyState : (bool)IsDirtyState;

        public void SetDirty(bool dirty)
        {
            if (_isOffline)
            {
                _offlineIsDirtyState = dirty;
            }
            else if (HasStateAuthority)
            {
                IsDirtyState = dirty;
            }
            // Update local visual as well
            if (dirtyVisual != null) dirtyVisual.SetActive(dirty);
        }

        private void OnDirtyStateChanged()
        {
            if (_isOffline) return;
            if (dirtyVisual != null) dirtyVisual.SetActive(IsDirtyState);
        }
    }
}
