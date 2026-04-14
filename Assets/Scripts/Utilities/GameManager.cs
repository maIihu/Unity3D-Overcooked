using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DesignPattern;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        GamePlaying, GameOver, Pause
    }

    public event EventHandler OnStateChanged;

    private GameState _currentState;

    private void Awake()
    {
        Initialize(this);
    }

    protected override void OnRegistration()
    {
        ChangeState(GameState.GamePlaying);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_currentState == GameState.GamePlaying)
            {
                ChangeState(GameState.Pause);
            }
            else if (_currentState == GameState.Pause)
            {
                ChangeState(GameState.GamePlaying);
            }
        }
    }

    public void ChangeState(GameState newState)
    {
        _currentState = newState;
        ApplyState();
    }

    private void ApplyState()
    {
        switch (_currentState)
        {
            case GameState.GamePlaying:
                Time.timeScale = 1f;
                break;
            case GameState.Pause:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
        }

        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public GameState GetCurrentState()
    {
        return _currentState;
    }
}
