using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using _Game.Scripts.DesignPattern.Observer;
using _Game.Scripts.Gameplay;
using _Game.Scripts.UI;
using UnityEngine.SceneManagement;

namespace GameCore.Network
{
    public class FusionNetworkRunner : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkObject playerPrefab;
        private NetworkRunner _runner;
        public NetworkRunner Runner => _runner;
        private static FusionNetworkRunner _instance;
        public static FusionNetworkRunner Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async void StartGameSession(GameMode mode, string sessionName)
        {
            if (_runner == null)
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
            }
            _runner.ProvideInput = true;
            var inputHandler = GetComponent<NetworkInputHandler>();
            Debug.Log($"[FusionNetworkRunner] InputHandler found: {inputHandler != null}");

            if (inputHandler != null)
            {
                _runner.AddCallbacks(inputHandler); // ← thêm dòng này
            }

            // Load thẳng GameScene (index 1) ở Phase 3 để chạy gameplay trực tiếp
            int sceneIndex = 1;

            // Start the runner
            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = sessionName,
                Scene = SceneRef.FromIndex(sceneIndex),
                SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
            {
                Debug.Log($"Started Fusion in {mode} mode.");
            }
            else
            {
                Debug.LogError($"Failed to start Fusion: {result.ShutdownReason}");
            }
        }

        public static event Action<List<SessionInfo>> OnSessionListChanged;

        public async void JoinLobby()
        {
            if (_runner == null)
            {
                _runner = gameObject.AddComponent<NetworkRunner>();
            }
            _runner.ProvideInput = true;

            var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

            if (result.Ok)
            {
                Debug.Log("Joined Fusion Lobby successfully.");
            }
            else
            {
                Debug.LogError($"Failed to join Fusion Lobby: {result.ShutdownReason}");
            }
        }

        public async void LeaveSession()
        {
            if (_runner != null)
            {
                await _runner.Shutdown();
                _runner = null;
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                // Trong chế độ Host/Client hoặc Single, server/host sinh nhân vật cho tất cả client tham gia
                Vector3 spawnPosition = new Vector3((player.PlayerId % 4) * 2f, 1, 0);
                NetworkObject playerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
                runner.SetPlayerObject(player, playerObject);
                Debug.Log($"Host spawned player object for player {player.PlayerId}");
            }
            else if (runner.GameMode == GameMode.Shared)
            {
                // Trong chế độ Shared, mỗi client tự sinh nhân vật của chính mình
                if (player == runner.LocalPlayer)
                {
                    Vector3 spawnPosition = new Vector3((player.PlayerId % 4) * 2f, 1, 0);
                    NetworkObject playerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
                    runner.SetPlayerObject(player, playerObject);
                    Debug.Log($"Client spawned player object for player {player.PlayerId} (Shared Mode)");
                }
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                NetworkObject playerObject = runner.GetPlayerObject(player);
                if (playerObject != null)
                {
                    runner.Despawn(playerObject);
                    Debug.Log($"Host despawned player object for player {player.PlayerId}");
                }
            }
        }

        // --- Empty INetworkRunnerCallbacks implementation ---
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            Debug.Log($"Fusion OnSessionListUpdated. Count: {sessionList.Count}");
            OnSessionListChanged?.Invoke(sessionList);
        }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[FusionNetworkRunner] OnSceneLoadDone: {sceneName}");

            if (sceneName == "GameScene")
            {
                // Gửi event load level cho GameplayManager
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnLoadLevel));

                // Hiện GameplayScreen cho tất cả client
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowScreen<GameplayScreen>();
                }
            }
            else if (sceneName == "MainMenuScene" || sceneName == "LobbyScene")
            {
                // Khi quay về menu (ví dụ sau khi game kết thúc)
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowScreen<MainMenuScreen>();
                }
            }
            else
            {
                Debug.Log($"[FusionNetworkRunner] Unhandled scene: {sceneName}");
            }
        }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}
