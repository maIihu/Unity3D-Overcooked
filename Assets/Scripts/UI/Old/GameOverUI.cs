using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        playButton.onClick.AddListener(() =>
        {
            Loader.LoadScene(Loader.Scene.GameScene);
        });
        
        quitButton.onClick.AddListener(() =>
        {
            Loader.LoadScene(Loader.Scene.MainMenuScene);
        });
    }
}
