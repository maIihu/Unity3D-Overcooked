using System;
using _Game.Scripts.DesignPattern.Observer;
using DesignPattern;
using UnityEngine;
using _Game.Scripts.UI;

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
        [SerializeField] private GameModeController gameModeController;

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
        public DeliveryController DeliveryController 
        {
            get => deliveryController;
            set => deliveryController = value;
        }
        public LevelController LevelController => levelController;
        public GameModeController GameModeController => gameModeController;

        public bool IsOffline => gameModeController != null && gameModeController.IsOffline;
        public bool IsOnline => gameModeController != null && gameModeController.IsOnline;
        public GameObject LocalPlayerPrefab => gameModeController != null ? gameModeController.LocalPlayerPrefab : null;
        
        private Counter.DeliveryCounter _deliveryCounter;

        private void Awake()
        {
            Initialize(this);
            if (gameModeController == null)
            {
                gameModeController = GetComponent<GameModeController>();
                if (gameModeController == null)
                {
                    gameModeController = gameObject.AddComponent<GameModeController>();
                }
            }
        }

        public void InitGame()
        {
            QualitySettings.vSyncCount = 0; // Tắt VSync — tránh override targetFrameRate
            Application.targetFrameRate = 60;
            Application.runInBackground = true; 
        }

        private void OnEnable()
        {
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnLoadLevel, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRecipeSuccess, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnGameOver, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnScoreChanged, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnExitGame, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnStartSingleplayer, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnSetMultiplayerMode, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnResetPlayMode, this);
        }

        private void OnDisable()
        {
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnLoadLevel, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRecipeSuccess, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnGameOver, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnScoreChanged, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnExitGame, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnStartSingleplayer, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnSetMultiplayerMode, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnResetPlayMode, this);
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
                    if (_deliveryCounter == null && levelController != null)
                    {
                        _deliveryCounter = levelController.GetDeliveryCounter();
                    }
                    if (message.Data.Length > 1 && _deliveryCounter != null && UIManager.Instance != null && UIManager.Instance.floatingScoreManager != null)
                    {
                        int scoreAdded = (int)message.Data[1];
                        UIManager.Instance.floatingScoreManager.SpawnFloatingScore(scoreAdded, _deliveryCounter.transform.position);
                    }
                    break;
                case ProjectMessageType.OnStartSingleplayer:
                    if (gameModeController != null)
                    {
                        gameModeController.StartSingleplayer();
                    }
                    break;
                case ProjectMessageType.OnSetMultiplayerMode:
                    if (gameModeController != null)
                    {
                        gameModeController.SetMultiplayerMode();
                    }
                    break;
                case ProjectMessageType.OnResetPlayMode:
                    if (gameModeController != null)
                    {
                        gameModeController.ResetMode();
                    }
                    break;
            }
        }

        private async void LoadLevel()
        {
            await levelController.LoadLevelAsync(levelData);
            _deliveryCounter = levelController.GetDeliveryCounter();

            if (IsOffline)
            {
                // ── Offline (Single Mode) ──
                // Lấy instance từ LevelController (nếu map có DeliveryController riêng)
                var levelDeliveryController = levelController.GetDeliveryControllerInstance();
                if (levelDeliveryController != null)
                {
                    deliveryController = levelDeliveryController;
                }

                if (deliveryController != null)
                {
                    deliveryController.StartSpawning();
                }
                else
                {
                    Debug.LogWarning("[GameManager] DeliveryController not found in level (offline)!");
                }

                // Start offline timer
                var timer = FindObjectOfType<GameTimerController>();
                if (timer != null)
                {
                    timer.StartOfflineTimer();
                }

                // Spawn player local
                SpawnLocalPlayer();

                // Chờ 1 frame để player kịp khởi tạo
                await System.Threading.Tasks.Task.Yield();
                UIManager.Instance?.ShowScreen<GameplayScreen>();
            }
            else
            {
                // ── Online (Multiplayer) ──
                // DeliveryController (NetworkBehaviour) được spawn qua mạng sẽ tự động đăng ký 
                // vào GameManager.DeliveryController thông qua hàm Awake/Spawned của nó.
                // Chờ 1 frame để object mạng có thời gian Instantiate và Awake
                await System.Threading.Tasks.Task.Yield();

                if (deliveryController != null)
                {
                    deliveryController.StartSpawning();
                }
                else
                {
                    Debug.LogWarning("[GameManager] DeliveryController not found in active scene!");
                }
            }
        }

        private void SpawnLocalPlayer()
        {
            if (LocalPlayerPrefab == null)
            {
                Debug.LogError("[GameManager] LocalPlayerPrefab is null! Cannot spawn player in Single Mode.");
                return;
            }
            Vector3 spawnPos = new Vector3(0f, 1f, 0f);
            Instantiate(LocalPlayerPrefab, spawnPos, Quaternion.identity);
            Debug.Log("[GameManager] Spawned PlayerLocal for Singleplayer.");
        }

        protected override void OnRegistration()
        {
            base.OnRegistration();
        }
    }
}

