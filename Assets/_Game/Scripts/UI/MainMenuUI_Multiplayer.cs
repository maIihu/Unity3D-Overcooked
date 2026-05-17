using UnityEngine;
using UnityEngine.UI;
using GameCore.Network;

namespace _Game.Scripts.UI
{
    public class MainMenuUI_Multiplayer : ScreenUI
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;
        private FusionNetworkRunner _networkRunner;

        public override void Initialize(UIManager uiManager)
        {
            base.Initialize(uiManager);

            _networkRunner = FindObjectOfType<FusionNetworkRunner>();

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() =>
                {
                    if (_networkRunner != null)
                    {
                        playButton.interactable = false;
                        Debug.Log("Play Button Clicked: Starting Fusion Lobby");
                        _networkRunner.StartSharedGame("OvercookedRoom");
                        this.Deactive();
                    }
                    else
                    {
                        Debug.LogError("FusionNetworkRunner not found!");
                    }
                });
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(() =>
                {
                    Application.Quit();
                });
            }
        }

        public override void Active()
        {
            base.Active();
            if (playButton != null)
            {
                playButton.interactable = true;
            }
        }

        protected override void OnScreenDestroyed()
        {
        }
    }
}
