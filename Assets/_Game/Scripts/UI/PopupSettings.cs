using UnityEngine;
using UnityEngine.UI;
using _Game.Scripts.DesignPattern.Observer;
using GameCore;
using GameCore.Network;
using _Game.Scripts.Utilities;

namespace _Game.Scripts.UI
{
    public class PopupSettings : PopupUI
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        private void OnEnable()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnDisable()
        {
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void OnResumeClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CurrentGameState = EGameState.Play;
            }
        }

        private void OnMainMenuClicked()
        {
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnExitGame));
            Loader.Load(Loader.Scene.MainMenuScene);
        }
    }
}
