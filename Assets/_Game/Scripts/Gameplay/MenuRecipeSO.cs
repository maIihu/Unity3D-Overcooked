using System;
using System.Collections.Generic;
using Kitchen;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.Gameplay
{
    [Serializable]
    public struct MenuItem
    {
        public EFoodType foodType;
        public Sprite sprite;
    }
    
    [CreateAssetMenu(fileName = "Recipe", menuName = "Gameplay/MenuRecipeSO")]
    public class MenuRecipeSO : ScriptableObject
    {
        public Sprite icon;
        public List<MenuItem> foodObjectMenu;
        public float timeRemaining;
    }
}