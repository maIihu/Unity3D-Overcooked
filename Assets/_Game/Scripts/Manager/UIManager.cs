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
        LoadUIFromResources();
        InitializeUI();
        ShowScreen<_Game.Scripts.UI.MainMenuScreen>();
        CleanDuplicateEventSystems();
    }

    public void LoadUIFromResources()
    {
        if (listScreen == null) listScreen = new List<ScreenUI>();
        else listScreen.Clear();

        if (listPopup == null) listPopup = new List<PopupUI>();
        else listPopup.Clear();

        if (canvas == null)
        {
            Debug.LogError("[UIManager] Canvas is null! Cannot instantiate UI screens or popups.");
            return;
        }

        // Load Screens
        var screenPrefabs = Resources.LoadAll<ScreenUI>("UI/Screens");
        if (screenPrefabs == null || screenPrefabs.Length == 0)
            screenPrefabs = Resources.LoadAll<ScreenUI>("UI/Screen");
        if (screenPrefabs == null || screenPrefabs.Length == 0)
            screenPrefabs = Resources.LoadAll<ScreenUI>("Screens");
        if (screenPrefabs == null || screenPrefabs.Length == 0)
            screenPrefabs = Resources.LoadAll<ScreenUI>("");

        foreach (var prefab in screenPrefabs)
        {
            if (prefab == null) continue;
            var instance = Instantiate(prefab, canvas.transform);
            instance.name = prefab.name;
            listScreen.Add(instance);
        }

        // Load Popups
        var popupPrefabs = Resources.LoadAll<PopupUI>("UI/Popups");
        if (popupPrefabs == null || popupPrefabs.Length == 0)
            popupPrefabs = Resources.LoadAll<PopupUI>("UI/Popup");
        if (popupPrefabs == null || popupPrefabs.Length == 0)
            popupPrefabs = Resources.LoadAll<PopupUI>("Popups");
        if (popupPrefabs == null || popupPrefabs.Length == 0)
            popupPrefabs = Resources.LoadAll<PopupUI>("");

        foreach (var prefab in popupPrefabs)
        {
            if (prefab == null) continue;
            var instance = Instantiate(prefab, canvas.transform);
            instance.name = prefab.name;
            listPopup.Add(instance);
        }
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
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnShowScreen, this);
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
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnShowScreen, this);
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

        // Dynamically instantiate PopupDisconnect if not present in prefabs resources
        if (typeof(T) == typeof(_Game.Scripts.UI.PopupDisconnect))
        {
            GameObject go = new GameObject("PopupDisconnect", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var newPopup = go.AddComponent<_Game.Scripts.UI.PopupDisconnect>();
            newPopup.Initialize(this);
            _popupCache[typeof(T)] = newPopup;
            listPopup.Add(newPopup);
            return newPopup as T;
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
        var settingsPopup = GetPopup<_Game.Scripts.UI.PopupSetting>();
        if (settingsPopup != null && settingsPopup.isShowing)
        {
            HidePopup<_Game.Scripts.UI.PopupSetting>();
            GameManager.Instance.CurrentGameState = EGameState.Play;
        }
        else
        {
            ShowPopup<_Game.Scripts.UI.PopupSetting>();
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
                    HidePopup<_Game.Scripts.UI.PopupSetting>();
                }
                else if (state == EGameState.Pause)
                {
                    ShowPopup<_Game.Scripts.UI.PopupSetting>();
                }
                break;
            case ProjectMessageType.OnShowScreen:
                if (data != null && data.Length > 0 && data[0] is Type screenType)
                {
                    if (screenType == typeof(LoadingScreen) && data.Length > 2)
                    {
                        string sceneName = (string)data[1];
                        Action onComplete = (Action)data[2];
                        var loadingScreen = GetScreen<LoadingScreen>();
                        if (loadingScreen != null)
                        {
                            ShowScreen<LoadingScreen>();
                            loadingScreen.TriggerLoad(sceneName, onComplete);
                        }
                        else
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                        }
                    }
                    else
                    {
                        if (screenType == typeof(GameplayScreen)) ShowScreen<GameplayScreen>();
                        else if (screenType == typeof(GameOverScreen)) ShowScreen<GameOverScreen>();
                        else if (screenType == typeof(MainMenuScreen)) ShowScreen<MainMenuScreen>();
                        else if (screenType == typeof(LoadingScreen)) ShowScreen<LoadingScreen>();
                        else if (screenType == typeof(MultiplayerLobbyScreen)) ShowScreen<MultiplayerLobbyScreen>();
                        else if (screenType == typeof(RoomWaitingScreen)) ShowScreen<RoomWaitingScreen>();
                    }
                }
                break;
        }
    }
}

