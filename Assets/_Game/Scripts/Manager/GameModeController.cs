using UnityEngine;
using UnityEngine.SceneManagement;
using DesignPattern;
using _Game.Scripts.DesignPattern.Observer;
using _Game.Scripts.UI;
using _Game.Scripts.Utilities;
using System;

namespace GameCore
{
    /// <summary>
    /// Quản lý chế độ chơi toàn cục (Singleplayer / Multiplayer).
    /// </summary>
    public class GameModeController : MonoBehaviour
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

        /// <summary>
        /// Bắt đầu chế độ Single Player.
        /// Load GameScene trực tiếp (không khởi chạy Fusion Runner) để tránh networking overhead.
        /// </summary>
        public void StartSingleplayer()
        {
            Debug.Log("[GameModeController] Starting Singleplayer mode (Offline — no Fusion).");
            CurrentMode = PlayMode.Singleplayer;

            // Gửi message qua Observer để UIManager tự mở Loading Screen và load cảnh
            MessageManager.Instance.SendMessage(
                new Message(
                    ProjectMessageType.OnShowScreen, 
                    new object[] { typeof(LoadingScreen), GAMEPLAY_SCENE_NAME, (Action)OnSingleplayerSceneLoaded }
                )
            );
        }

        private void OnSingleplayerSceneLoaded()
        {
            // Trigger load level + spawn player offline
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnLoadLevel));
        }

        private System.Collections.IEnumerator DelayedOnSingleplayerSceneLoaded()
        {
            yield return null;
            OnSingleplayerSceneLoaded();
        }

        /// <summary>
        /// Đặt mode = Multiplayer (không load scene).
        /// UIManager sẽ chuyển sang MultiplayerLobbyScreen.
        /// </summary>
        public void SetMultiplayerMode()
        {
            Debug.Log("[GameModeController] Switching to Multiplayer mode.");
            CurrentMode = PlayMode.Multiplayer;
        }

        /// <summary>
        /// Reset về trạng thái ban đầu khi quay về Main Menu.
        /// </summary>
        public void ResetMode()
        {
            CurrentMode = PlayMode.None;
        }
    }
}
