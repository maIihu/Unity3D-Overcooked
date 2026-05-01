using System.Collections.Generic;
using _Game.Scripts.Gameplay;
using GameCore;
using Kitchen;
using UnityEngine;

namespace _Game.Scripts.UI
{
    public class GameplayScreen : ScreenUI
    {
        [SerializeField] private UIMenuItem menuItemPrefab;
        [SerializeField] private GameObject menuHolder;

        private readonly Dictionary<int, UIMenuItem> activeItems
            = new Dictionary<int, UIMenuItem>();

        public override void Initialize(UIManager uiManager)
        {
            base.Initialize(uiManager);
        }

        public override void Active()
        {
            base.Active();
        }

        public void SetMenuItem(ActiveRecipe activeRecipe)
        {
            UIMenuItem uiItem = GetAvailableItem();

            uiItem.SetImage(
                activeRecipe.Data.icon,
                activeRecipe.Data.foodObjectMenu[0].sprite
            );

            uiItem.Initialize(activeRecipe.Data.timeRemaining);

            activeItems[activeRecipe.Id] = uiItem;
        }

        private UIMenuItem GetAvailableItem()
        {
            foreach (Transform child in menuHolder.transform)
            {
                if (!child.gameObject.activeSelf)
                {
                    var component = child.GetComponent<UIMenuItem>();

                    if (component != null)
                    {
                        child.gameObject.SetActive(true);
                        return component;
                    }
                }
            }

            return Instantiate(menuItemPrefab, menuHolder.transform);
        }

        public void RemoveMenuItem(ActiveRecipe activeRecipe)
        {
            if (activeItems.TryGetValue(activeRecipe.Id, out var uiItem))
            {
                uiItem.gameObject.SetActive(false);

                activeItems.Remove(activeRecipe.Id);
            }
        }

        protected override void OnScreenDestroyed()
        {
        }
    }
}