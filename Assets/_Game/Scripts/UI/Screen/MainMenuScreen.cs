using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameCore;
using DG.Tweening;
using _Game.Scripts.UI;

namespace _Game.Scripts.UI
{
    /// <summary>
    /// Màn hình đầu tiên - chọn chế độ chơi: Single Player hoặc Multiplayer.
    /// </summary>
    public class MainMenuScreen : ScreenUI
    {
        [Header("Buttons")]
        [SerializeField] private Button singlePlayerButton;
        [SerializeField] private Button multiplayerButton;
        [SerializeField] private Button settingButton;
        [SerializeField] private Button quitButton;

        [Header("Hover Settings")]
        [SerializeField] private Sprite normalButtonSprite;
        [SerializeField] private Sprite hoverButtonSprite;
        [SerializeField] private float hoverScale = 1.2f;
        [SerializeField] private float hoverDuration = 0.2f;

        public override void Initialize(UIManager uiManager)
        {
            base.Initialize(uiManager);

            SetupButtonHover(singlePlayerButton, OnSinglePlayerClicked);
            SetupButtonHover(multiplayerButton, OnMultiplayerClicked);
            SetupButtonHover(settingButton, OnSettingClicked);
            SetupButtonHover(quitButton, OnQuitClicked);
        }

        private void SetupButtonHover(Button btn, UnityEngine.Events.UnityAction onClickAction)
        {
            if (btn == null) return;
            
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(onClickAction);

            // Gắn EventTrigger để nhận sự kiện Hover bằng code
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();
            
            Image btnImage = btn.GetComponent<Image>();
            trigger.triggers.Clear();

            // Khi chuột di chuyển vào (Pointer Enter)
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => {
                if (btn.interactable)
                {
                    DOTween.Kill(btn.transform);
                    btn.transform.DOScale(hoverScale, hoverDuration).SetUpdate(true);
                    
                    if (btnImage != null && hoverButtonSprite != null) 
                    {
                        btnImage.sprite = hoverButtonSprite;
                    }
                }
            });
            trigger.triggers.Add(enterEntry);

            // Khi chuột di chuyển ra (Pointer Exit)
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => {
                if (btn.interactable)
                {
                    DOTween.Kill(btn.transform);
                    btn.transform.DOScale(1f, hoverDuration).SetUpdate(true);
                    
                    if (btnImage != null && normalButtonSprite != null) 
                    {
                        btnImage.sprite = normalButtonSprite;
                    }
                }
            });
            trigger.triggers.Add(exitEntry);
        }

        private void OnSinglePlayerClicked()
        {
            Debug.Log("[MainMenuScreen] Single Player selected.");

            // Disable button to prevent double-click
            if (singlePlayerButton != null) 
            {
                singlePlayerButton.interactable = false;
                ResetButtonState(singlePlayerButton);
            }

            // Show loading screen immediately with fake progress
            if (uiManager != null)
            {
                var loadingScreen = uiManager.GetScreen<LoadingScreen>();
                if (loadingScreen != null)
                {
                    uiManager.ShowScreen<LoadingScreen>();
                    loadingScreen.StartFakeProgress();
                }
            }

            // Trigger singleplayer start (Fusion will load GameScene)
            _Game.Scripts.DesignPattern.Observer.MessageManager.Instance.SendMessage(new _Game.Scripts.DesignPattern.Observer.Message(_Game.Scripts.DesignPattern.Observer.ProjectMessageType.OnStartSingleplayer));
        }

        private void OnMultiplayerClicked()
        {
            Debug.Log("[MainMenuScreen] Multiplayer selected.");
            _Game.Scripts.DesignPattern.Observer.MessageManager.Instance.SendMessage(new _Game.Scripts.DesignPattern.Observer.Message(_Game.Scripts.DesignPattern.Observer.ProjectMessageType.OnSetMultiplayerMode));

            if (uiManager != null)
            {
                uiManager.ShowScreen<MultiplayerLobbyScreen>();
            }
        }

        private void OnSettingClicked()
        {
            Debug.Log("[MainMenuScreen] Setting selected.");
            if (uiManager != null)
            {
                uiManager.ShowPopup<PopupSetting>();
            }
        }

        private void OnQuitClicked()
        {
            Debug.Log("[MainMenuScreen] Quit Game.");
            Application.Quit();
        }

        public override void Active()
        {
            base.Active();
            if (singlePlayerButton != null) singlePlayerButton.interactable = true;
            if (multiplayerButton != null) multiplayerButton.interactable = true;
            if (settingButton != null) settingButton.interactable = true;
        }

        private void ResetButtonState(Button btn)
        {
            if (btn == null) return;
            
            DOTween.Kill(btn.transform);
            btn.transform.localScale = Vector3.one;
            
            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null && normalButtonSprite != null)
            {
                btnImage.sprite = normalButtonSprite;
            }
        }

        private void OnDisable()
        {
            ResetButtonState(singlePlayerButton);
            ResetButtonState(multiplayerButton);
            ResetButtonState(settingButton);
            ResetButtonState(quitButton);
        }

        protected override void OnScreenDestroyed()
        {
            if (singlePlayerButton != null) DOTween.Kill(singlePlayerButton.transform);
            if (multiplayerButton != null) DOTween.Kill(multiplayerButton.transform);
            if (settingButton != null) DOTween.Kill(settingButton.transform);
            if (quitButton != null) DOTween.Kill(quitButton.transform);
        }
    }
}
