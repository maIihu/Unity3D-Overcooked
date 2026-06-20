using UnityEngine;
using UnityEngine.SceneManagement;
using DesignPattern;
using _Game.Scripts.DesignPattern.Observer;
using _Game.Scripts.UI;
using _Game.Scripts.Utilities;
using System;

namespace GameCore
{

    public class GameModeController : MonoBehaviour
    {
        public enum PlayMode
        {
            None,
            Singleplayer,
            Multiplayer
        }

        private const string GAMEPLAY_SCENE_NAME = "GameScene";

        [SerializeField] private GameObject _localPlayerPrefab;
        public GameObject LocalPlayerPrefab => _localPlayerPrefab;

        [SerializeField] private Vector3 _localPlayerSpawnPosition = new Vector3(0f, 1f, 0f);

        public PlayMode CurrentMode { get; private set; } = PlayMode.None;

        public bool IsOffline => CurrentMode == PlayMode.Singleplayer;

        public bool IsOnline => CurrentMode == PlayMode.Multiplayer;

        public void StartSingleplayer()
        {
            Debug.Log("[GameModeController] Starting Singleplayer mode (Offline — no Fusion).");
            CurrentMode = PlayMode.Singleplayer;

            MessageManager.Instance.SendMessage(
                new Message(
                    ProjectMessageType.OnShowScreen, 
                    new object[] { typeof(LoadingScreen), GAMEPLAY_SCENE_NAME, (Action)OnSingleplayerSceneLoaded }
                )
            );
        }

        private void OnSingleplayerSceneLoaded()
        {
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnLoadLevel));
        }

        private System.Collections.IEnumerator DelayedOnSingleplayerSceneLoaded()
        {
            yield return null;
            OnSingleplayerSceneLoaded();
        }

        public void SetMultiplayerMode()
        {
            Debug.Log("[GameModeController] Switching to Multiplayer mode.");
            CurrentMode = PlayMode.Multiplayer;
        }

        public void ResetMode()
        {
            CurrentMode = PlayMode.None;
        }
    }
}
