using UnityEngine;
using UnityEngine.UI;
using GameCore;

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
        [SerializeField] private Button quitButton;

        public override void Initialize(UIManager uiManager)
        {
            base.Initialize(uiManager);

            if (singlePlayerButton != null)
            {
                singlePlayerButton.onClick.RemoveAllListeners();
                singlePlayerButton.onClick.AddListener(OnSinglePlayerClicked);
            }

            if (multiplayerButton != null)
            {
                multiplayerButton.onClick.RemoveAllListeners();
                multiplayerButton.onClick.AddListener(OnMultiplayerClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void OnSinglePlayerClicked()
        {
            Debug.Log("[MainMenuScreen] Single Player selected.");

            // Disable button to prevent double-click
            if (singlePlayerButton != null) singlePlayerButton.interactable = false;

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
        }

        protected override void OnScreenDestroyed()
        {
        }
    }
}
