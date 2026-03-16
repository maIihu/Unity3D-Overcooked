using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        playButton.onClick.AddListener(() =>
        {
            this.gameObject.SetActive(false);
            KitchenGameManager.Instance.ChangeState(KitchenGameManager.GameState.GamePlaying);
        });
        
        quitButton.onClick.AddListener(() =>
        {
            Loader.LoadScene(Loader.Scene.MainMenuScene);
        });
    }
}
