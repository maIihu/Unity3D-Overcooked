using System.Collections.Generic;
using _Game.Scripts.Gameplay;
using GameCore;
using Kitchen;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using _Game.Scripts.DesignPattern.Observer;

namespace _Game.Scripts.UI
{
    public class GameplayScreen : ScreenUI
    {
        [SerializeField] private UIMenuItem menuItemPrefab;
        [SerializeField] private GameObject menuHolder;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Button settingsButton;

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

        private void OnEnable()
        {
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnDisable()
        {
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                OnSettingsClicked();
            }
        }

        private void OnSettingsClicked()
        {
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnToggleSettings));
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

        public void RemoveMenuItemWithEffect(ActiveRecipe activeRecipe)
        {
            if (activeItems.TryGetValue(activeRecipe.Id, out var uiItem))
            {
                activeItems.Remove(activeRecipe.Id);

                uiItem.PlaySuccessAnimation(() =>
                {
                    uiItem.gameObject.SetActive(false);
                });
            }
        }

        protected override void OnScreenDestroyed()
        {
        }
        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }
        }
        
        public void UpdateTimer(float time)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }
    }
}