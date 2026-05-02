using System;
using System.Collections.Generic;
using Kitchen;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.Gameplay
{
    public enum EMenuType
    {
        OnionSoup = 0,
        TomatoSoup = 1,
    }
    [Serializable]
    public struct MenuItem
    {
        public EFoodType foodType;
        public Sprite sprite;
    }
    
    [CreateAssetMenu(fileName = "Recipe", menuName = "Gameplay/MenuRecipeSO")]
    public class MenuRecipeSO : ScriptableObject
    {
        public EMenuType menuType;
        public Sprite icon;
        public List<MenuItem> foodObjectMenu;
        public float timeRemaining;
    }
}