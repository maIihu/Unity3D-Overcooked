using System;
using _Game.Scripts.DesignPattern.Observer;
using DesignPattern;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Central manager — the ONLY Singleton in the gameplay layer.
    /// All sub-controllers are referenced here and accessed via
    /// GameManager.Instance.DeliveryController / GameManager.Instance.LevelController.
    /// </summary>
    public class GameManager : Singleton<GameManager>, IMessageHandle
    {
        [Header("Controllers")]
        [SerializeField] private DeliveryController deliveryController;
        [SerializeField] private LevelController levelController;

        [Header("Game State")]
        [SerializeField] private int targetScore = 50;
        private EGameState _currentGameState = EGameState.Play;
        private int _currentScore = 0;

        public EGameState CurrentGameState
        {
            get => _currentGameState;
            set
            {
                if (_currentGameState != value)
                {
                    _currentGameState = value;
                    MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnGameStateChanged, new object[] { _currentGameState }));
                }
            }
        }

        public string levelData; // temp

        // ── Public Accessors ───────────────────────────────────
        // Trả về trực tiếp — không gọi FindObjectsOfType mỗi lần access.
        // deliveryController được resolve một lần duy nhất trong LoadLevel().
        public DeliveryController DeliveryController => deliveryController;
        public LevelController LevelController => levelController;
        
        private Counter.DeliveryCounter _deliveryCounter;

        private void Awake()
        {
            Initialize(this);
            Application.targetFrameRate = 60;
            Application.runInBackground = true; // Rất quan trọng khi test Multiplayer trên cùng 1 máy!
        }

        private void OnEnable()
        {
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnLoadLevel, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRecipeSuccess, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnGameOver, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnScoreChanged, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnExitGame, this);
        }

        private void OnDisable()
        {
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnLoadLevel, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRecipeSuccess, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnGameOver, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnScoreChanged, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnExitGame, this);
        }

        public void Handle(Message message)
        {
            switch (message.Type)
            {
                case ProjectMessageType.OnLoadLevel:
                    CurrentGameState = EGameState.Play;
                    _currentScore = 0;
                    LoadLevel();
                    break;
                case ProjectMessageType.OnScoreChanged:
                    _currentScore = (int)message.Data[0];
                    break;
                case ProjectMessageType.OnGameOver:
                    CurrentGameState = _currentScore >= targetScore ? EGameState.Win : EGameState.Lose;
                    break;
                case ProjectMessageType.OnExitGame:
                    if (levelController != null)
                    {
                        levelController.ClearLevel();
                    }
                    break;
                case ProjectMessageType.OnRecipeSuccess:
                    if (message.Data.Length > 1 && _deliveryCounter != null && UIManager.Instance != null && UIManager.Instance.floatingScoreManager != null)
                    {
                        int scoreAdded = (int)message.Data[1];
                        UIManager.Instance.floatingScoreManager.SpawnFloatingScore(scoreAdded, _deliveryCounter.transform.position);
                    }
                    break;
            }
        }

        private async void LoadLevel()
        {
            await levelController.LoadLevelAsync(levelData);
            _deliveryCounter = levelController.GetDeliveryCounter();

            // Resolve DeliveryController từ scene hiện tại một lần duy nhất (tránh FindObjectsOfType trong getter)
            var controllers = FindObjectsOfType<DeliveryController>();
            deliveryController = null;
            foreach (var c in controllers)
            {
                if (c.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    deliveryController = c;
                    break;
                }
            }

            if (deliveryController != null)
            {
                deliveryController.StartSpawning();
            }
            else
            {
                Debug.LogWarning("[GameManager] DeliveryController not found in active scene!");
            }
        }

        protected override void OnRegistration()
        {
            base.OnRegistration();
        }
    }
}

