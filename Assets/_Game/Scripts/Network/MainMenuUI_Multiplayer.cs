using UnityEngine;
using UnityEngine.UI;
using GameCore.Network;

public class MainMenuUI_Multiplayer : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    
    private FusionNetworkRunner _networkRunner;

    private void Start()
    {
        _networkRunner = FindObjectOfType<FusionNetworkRunner>();

        if (playButton != null)
        {
            playButton.onClick.AddListener(() =>
            {
                if (_networkRunner != null)
                {
                    _networkRunner.StartSharedGame("OvercookedRoom");
                    playButton.interactable = false; // Prevent multiple clicks
                }
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() =>
            {
                Application.Quit();
            });
        }
    }
}
