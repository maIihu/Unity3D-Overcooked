using UnityEngine;
using UnityEngine.SceneManagement;
using DesignPattern;
using _Game.Scripts.DesignPattern.Observer;
using _Game.Scripts.UI;
using _Game.Scripts.Utilities;

namespace GameCore
{
    /// <summary>
    /// Quản lý chế độ chơi toàn cục (Singleplayer / Multiplayer).
    /// Singleton tồn tại xuyên suốt toàn bộ game (DontDestroyOnLoad).
    /// ⚠️ Nhớ tick '_dontDestroyOnLoad = true' trong Inspector trên GameObject.
    /// </summary>
    public class GameModeManager : Singleton<GameModeManager>
    {
        public enum PlayMode
        {
            None,
            Singleplayer,
            Multiplayer
        }

        /// <summary>Tên Scene Gameplay (phải khớp với tên file scene).</summary>
        private const string GAMEPLAY_SCENE_NAME = "GameScene";

        /// <summary>Prefab Player dùng cho Single Mode (PlayerLocal component, không có NetworkObject).</summary>
        [SerializeField] private GameObject _localPlayerPrefab;
        public GameObject LocalPlayerPrefab => _localPlayerPrefab;

        /// <summary>Vị trí spawn Player khi bắt đầu Single Mode.</summary>
        [SerializeField] private Vector3 _localPlayerSpawnPosition = new Vector3(0f, 1f, 0f);

        /// <summary>Chế độ chơi hiện tại.</summary>
        public PlayMode CurrentMode { get; private set; } = PlayMode.None;

        /// <summary>Đang ở chế độ Offline (Single Player)?</summary>
        public bool IsOffline => CurrentMode == PlayMode.Singleplayer;

        /// <summary>Đang ở chế độ Online (Multiplayer)?</summary>
        public bool IsOnline => CurrentMode == PlayMode.Multiplayer;

        private void Awake()
        {
            // Gọi Initialize để kích hoạt cơ chế DontDestroyOnLoad từ Singleton base.
            // ⚠️ Phải tick _dontDestroyOnLoad = true trong Inspector.
            Initialize(this);
        }

        /// <summary>
        /// Bắt đầu chế độ Single Player.
        /// Đặt mode = Singleplayer và khởi chạy Fusion dưới dạng Single player (Local simulation).
        /// </summary>
        public void StartSingleplayer()
        {
            Debug.Log("[GameModeManager] Starting Singleplayer mode via Fusion.");
            CurrentMode = PlayMode.Singleplayer;

            if (GameCore.Network.FusionNetworkRunner.Instance != null)
            {
                GameCore.Network.FusionNetworkRunner.Instance.StartGameSession(Fusion.GameMode.Single, "OfflineRoom").FireAndForget();
            }
            else
            {
                Debug.LogError("[GameModeManager] FusionNetworkRunner.Instance not found! Falling back to standard scene load.");
                SceneManager.LoadScene(GAMEPLAY_SCENE_NAME);
            }
        }

        /// <summary>
        /// Đặt mode = Multiplayer (không load scene).
        /// UIManager sẽ chuyển sang MultiplayerLobbyScreen.
        /// </summary>
        public void SetMultiplayerMode()
        {
            Debug.Log("[GameModeManager] Switching to Multiplayer mode.");
            CurrentMode = PlayMode.Multiplayer;
        }

        /// <summary>
        /// Reset về trạng thái ban đầu khi quay về Main Menu.
        /// </summary>
        public void ResetMode()
        {
            CurrentMode = PlayMode.None;
        }

        protected override void OnRegistration()
        {
            base.OnRegistration();
        }
    }
}
