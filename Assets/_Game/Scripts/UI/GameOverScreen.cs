using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Game.Scripts.DesignPattern.Observer;
using _Game.Scripts.UI;
using GameCore.Network;

namespace GameCore.UI
{
    public class GameOverScreen : ScreenUI
    {
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button mainMenuButton;

        private int _finalScore;

        private void OnEnable()
        {
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnDisable()
        {
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        public void SetFinalScore(int score)
        {
            _finalScore = score;
            if (scoreText != null)
            {
                scoreText.text = _finalScore.ToString();
            }
        }

        private void OnMainMenuClicked()
        {
            // Disconnect and go back to main menu
            FusionNetworkRunner.Instance.LeaveSession();
            Loader.Load(Loader.Scene.MainMenuScene);
        }

        protected override void OnScreenDestroyed()
        {
        }
    }
}
