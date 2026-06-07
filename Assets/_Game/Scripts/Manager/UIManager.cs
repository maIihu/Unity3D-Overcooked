using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Game.Scripts.DesignPattern.Observer;
using _Game.Scripts.Gameplay;
using _Game.Scripts.UI;
using GameCore.UI;
using DesignPattern;
using GameCore;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>, IMessageHandle
{
    public Camera UICamera;
    public Canvas canvas;
    
    [SerializeField] List<ScreenUI> listScreen;
    [SerializeField] List<PopupUI> listPopup;
    private Dictionary<Type, ScreenUI> _screenCache = new Dictionary<Type, ScreenUI>();
    private Dictionary<Type, PopupUI> _popupCache = new Dictionary<Type, PopupUI>();
    
    public FloatingScoreManager floatingScoreManager;

    private void Awake()
    {
        Initialize(this);
    }

    public void InitUI()
    {
        InitializeUI();
        ShowScreen<_Game.Scripts.UI.MainMenuScreen>();
        Debug.Log("Show");
        CleanDuplicateEventSystems();
    }

    private void OnEnable()
    {
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnSpawnNewRecipe, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRejectRecipe, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRecipeSuccess, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnGameOver, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnScoreChanged, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnTimerTick, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnGameStateChanged, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnToggleSettings, this);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnSpawnNewRecipe, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRejectRecipe, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRecipeSuccess, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnGameOver, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnScoreChanged, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnTimerTick, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnGameStateChanged, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnToggleSettings, this);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        CleanDuplicateEventSystems();
    }

    private void CleanDuplicateEventSystems()
    {
        var eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystems.Length > 1)
        {
            for (int i = 1; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] != null && eventSystems[i].gameObject != null)
                {
                    Destroy(eventSystems[i].gameObject);
                }
            }
        }
    }
    
    public void InitializeUI()
    {
        _screenCache.Clear();
        foreach (var screen in listScreen)
        {
            if (screen != null)
            {
                _screenCache[screen.GetType()] = screen;
                screen.Initialize(this);
                screen.Deactive();
            }
        }
        _popupCache.Clear();
        foreach (var popup in listPopup)
        {
            if (popup != null)
            {
                _popupCache[popup.GetType()] = popup;
                popup.Initialize(this);
                popup.gameObject.SetActive(false);
            }
        }
    }

    public void ShowScreen<T>() where T : ScreenUI
    {
        foreach (var screen in listScreen)
        {
            if (screen != null)
                screen.Deactive();
        }

        // Active the target screen
        var target = GetScreen<T>();
        if (target != null)
            target.Active();
    }
    
    public T GetScreen<T>() where T : ScreenUI
    {
        if (_screenCache.TryGetValue(typeof(T), out var screen))
        {
            return screen as T;
        }
        return null;
    }

    public T GetPopup<T>() where T : PopupUI
    {
        if (_popupCache.TryGetValue(typeof(T), out var popup))
        {
            return popup as T;
        }
        return null;
    }

    public void ShowPopup<T>(Action onClose = null) where T : PopupUI
    {
        var target = GetPopup<T>();
        if (target != null)
            target.Show(onClose);
    }

    public void HidePopup<T>() where T : PopupUI
    {
        var target = GetPopup<T>();
        if (target != null)
            target.Hide();
    }

    public void ToggleSettings()
    {
        if (GameManager.Instance == null) return;
        var settingsPopup = GetPopup<_Game.Scripts.UI.PopupSettings>();
        if (settingsPopup != null && settingsPopup.isShowing)
        {
            HidePopup<_Game.Scripts.UI.PopupSettings>();
            GameManager.Instance.CurrentGameState = EGameState.Play;
        }
        else
        {
            ShowPopup<_Game.Scripts.UI.PopupSettings>();
            GameManager.Instance.CurrentGameState = EGameState.Pause;
        }
    }
    
    public void Handle(Message message)
    {
        var data = message.Data;
        switch (message.Type)
        {
            case ProjectMessageType.OnSpawnNewRecipe:
                GetScreen<GameplayScreen>().SetMenuItem((ActiveRecipe)data[0]);
                break;
            case ProjectMessageType.OnRejectRecipe:
                GetScreen<GameplayScreen>().RemoveMenuItem((ActiveRecipe)data[0]);
                break;
            case ProjectMessageType.OnRecipeSuccess:
                GetScreen<GameplayScreen>().RemoveMenuItemWithEffect((ActiveRecipe)data[0]);
                break;
            case ProjectMessageType.OnScoreChanged:
                int score = (int)data[0];
                GetScreen<GameplayScreen>()?.UpdateScore(score);
                GetScreen<GameOverScreen>()?.SetFinalScore(score);
                break;
            case ProjectMessageType.OnTimerTick:
                GetScreen<GameplayScreen>()?.UpdateTimer((float)data[0]);
                break;
            case ProjectMessageType.OnGameOver:
                ShowScreen<GameOverScreen>();
                break;
            case ProjectMessageType.OnToggleSettings:
                ToggleSettings();
                break;
            case ProjectMessageType.OnGameStateChanged:
                EGameState state = (EGameState)data[0];
                if (state == EGameState.Play)
                {
                    HidePopup<_Game.Scripts.UI.PopupSettings>();
                }
                else if (state == EGameState.Pause)
                {
                    ShowPopup<_Game.Scripts.UI.PopupSettings>();
                }
                break;
        }
    }
}

