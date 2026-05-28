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

    private void Awake()
    {
        Initialize(this);
    }

    private void Start()
    {
        InitializeUI();
        ShowScreen<_Game.Scripts.UI.MainMenuScreen>();
    }

    private void OnEnable()
    {
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnSpawnNewRecipe, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRejectRecipe, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRecipeSuccess, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnGameOver, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnScoreChanged, this);
        MessageManager.Instance.AddSubscriber(ProjectMessageType.OnTimerTick, this);
    }

    private void OnDisable()
    {
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnSpawnNewRecipe, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRejectRecipe, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRecipeSuccess, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnGameOver, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnScoreChanged, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnTimerTick, this);
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
        foreach (var popup in listPopup)
        {
            popup.Initialize(this);
           // popup.Deactive();
        }
    }

    public void ShowScreen<T>() where T : ScreenUI
    {
        // Deactive all screens first
        foreach (var screen in listScreen)
            screen.Deactive();

        // Active the target screen
        var target = GetScreen<T>();
        if (target != null)
            target.Active();
    }
    
    private T GetScreen<T>() where T : ScreenUI
    {
        if (_screenCache.TryGetValue(typeof(T), out var screen))
        {
            return screen as T;
        }
        return null;
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
        }
    }
}

