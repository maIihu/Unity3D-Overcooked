using System;
using UnityEngine;
using DesignPattern;

public class LevelController : Singleton<LevelController>
{

    private float _gamePlayingTimer;
    [SerializeField] private float gamePlayingTimerMax = 360f;
    [SerializeField] private DeliveryController deliveryControl;

    public DeliveryController DeliveryControl => deliveryControl;

    private void Awake()
    {
        Initialize(this);
    }

    protected override void OnRegistration()
    {
        _gamePlayingTimer = gamePlayingTimerMax;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
        }
    }

    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        var state = GameManager.Instance.GetCurrentState();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentState() == GameManager.GameState.GamePlaying)
        {
            _gamePlayingTimer -= Time.deltaTime;
            if (_gamePlayingTimer <= 0f)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
            }
        }
    }

    public float GetPlayingTimerNormalized()
    {
        return 1 - (_gamePlayingTimer / gamePlayingTimerMax);
    }
}
