using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Game.Scripts.DesignPattern.Observer;
using _Game.Scripts.Gameplay;
using _Game.Scripts.UI;
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
    }

    private void OnDisable()
    {
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnSpawnNewRecipe, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRejectRecipe, this);
        MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRecipeSuccess, this);
    }
    
    public void InitializeUI()
    {
        foreach (var screen in listScreen)
        {
            screen.Initialize(this);
            screen.Deactive();
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
        for (int i = 0; i < listScreen.Count; i++)
        {
            if (listScreen[i] is T)
            {
                var screen = listScreen[i].GetComponent<T>();
                return screen;
            }
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
        }
    }
}

