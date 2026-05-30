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
        [SerializeField] private NetworkObject lobbyPlayerPrefab;
        private NetworkRunner _runner;
        public NetworkRunner Runner => _runner;
        private static FusionNetworkRunner _instance;
        public static FusionNetworkRunner Instance => _instance;

        // Cache runner GameObject để tránh FindObjectsOfType tốn kém
        private GameObject _runnerGo;
        private NetworkInputHandler _inputHandler;

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
            if (_runner != null)
            {
                Debug.Log("[FusionNetworkRunner] Shutting down active runner before starting new session.");
                var oldRunner = _runner;
                _runner = null;
                await oldRunner.Shutdown();
                // Dùng cached reference thay vì FindObjectsOfType
                if (_runnerGo != null)
                {
                    DestroyImmediate(_runnerGo);
                    _runnerGo = null;
                }
                await System.Threading.Tasks.Task.Delay(300);
            }

            _runnerGo = new GameObject("FusionNetworkRunner_Instance");
            DontDestroyOnLoad(_runnerGo);

            _runner = _runnerGo.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            // Đăng ký callbacks
            _runner.AddCallbacks(this);
            
            // Đảm bảo chỉ có 1 InputHandler, thêm vào _runnerGo thay vì this
            _inputHandler = _runnerGo.GetComponent<NetworkInputHandler>();
            if (_inputHandler == null)
            {
                _inputHandler = _runnerGo.AddComponent<NetworkInputHandler>();
                Debug.Log("[FusionNetworkRunner] InputHandler added to runner GameObject.");
            }
            _runner.AddCallbacks(_inputHandler);

            // Singleplayer -> GameScene (index 1), Multiplayer -> MainMenuScene (index 0)
            int sceneIndex = (mode == GameMode.Single) ? 1 : 0;

            var sceneManager = _runnerGo.AddComponent<NetworkSceneManagerDefault>();

            // Start the runner
            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = sessionName,
                Scene = SceneRef.FromIndex(sceneIndex),
                SceneManager = sceneManager,
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
            if (_runner != null)
            {
                Debug.Log("[FusionNetworkRunner] Shutting down active runner before joining lobby.");
                var oldRunner = _runner;
                _runner = null;
                await oldRunner.Shutdown();
                if (_runnerGo != null)
                {
                    DestroyImmediate(_runnerGo);
                    _runnerGo = null;
                }
                await System.Threading.Tasks.Task.Delay(300);
            }

            _runnerGo = new GameObject("FusionNetworkRunner_Instance");
            DontDestroyOnLoad(_runnerGo);

            _runner = _runnerGo.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
            _runner.AddCallbacks(this);

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
                Debug.Log("[FusionNetworkRunner] Leaving session and destroying runner component.");
                var oldRunner = _runner;
                _runner = null;
                await oldRunner.Shutdown();
                if (_runnerGo != null)
                {
                    DestroyImmediate(_runnerGo);
                    _runnerGo = null;
                }
                _inputHandler = null;
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                if (runner.GameMode == GameMode.Single)
                {
                    // Chơi đơn: Không sinh ở đây, sẽ sinh ở OnSceneLoadDone khi GameScene load xong
                    Debug.Log($"[FusionNetworkRunner] Singleplayer player {player.PlayerId} joined. Deferring spawn to OnSceneLoadDone.");
                }
                else
                {
                    // Chơi mạng: Sinh LobbyPlayer tạm thời để lưu giữ thông tin chờ
                    NetworkObject lobbyPlayerObject = runner.Spawn(lobbyPlayerPrefab, Vector3.zero, Quaternion.identity, player);
                    runner.SetPlayerObject(player, lobbyPlayerObject);
                    runner.MakeDontDestroyOnLoad(lobbyPlayerObject.gameObject); // Giữ LobbyPlayer qua scene load để đọc dữ liệu màu sắc
                    Debug.Log($"[FusionNetworkRunner] Host spawned lobby player object for player {player.PlayerId}");
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
                if (runner.IsServer)
                {
                    // Lấy danh sách LobbyPlayer hiện có trong scene mới
                    var lobbyPlayers = FindObjectsOfType<LobbyPlayer>();
                    Dictionary<PlayerRef, EPlayerColor> playerColors = new Dictionary<PlayerRef, EPlayerColor>();
                    foreach (var lp in lobbyPlayers)
                    {
                        if (lp.Object != null && lp.Object.IsValid)
                        {
                            playerColors[lp.Object.InputAuthority] = lp.PlayerColor;
                        }
                    }

                    // Host sinh nhân vật gameplay cho tất cả người chơi hoạt động
                    foreach (var playerRef in runner.ActivePlayers)
                    {
                        Vector3 spawnPosition = new Vector3((playerRef.PlayerId % 4) * 2f, 1, 0);
                        
                        if (runner.GameMode == GameMode.Single)
                        {
                            // Trong Singleplayer: Dùng PlayerLocal (thuần MonoBehaviour) để di chuyển mượt, 
                            // nhưng vẫn giữ Fusion runner cho các Counter & Food.
                            if (GameModeManager.Instance != null && GameModeManager.Instance.LocalPlayerPrefab != null)
                            {
                                Instantiate(GameModeManager.Instance.LocalPlayerPrefab, spawnPosition, Quaternion.identity);
                                Debug.Log("[FusionNetworkRunner] Spawned PlayerLocal for Singleplayer.");
                            }
                            continue;
                        }

                        EPlayerColor selectedColor = EPlayerColor.Red;
                        if (playerColors.TryGetValue(playerRef, out EPlayerColor lpColor))
                        {
                            selectedColor = lpColor;
                        }

                        // Host sinh gameplay player và truyền data màu sắc trước khi spawn hoàn tất
                        NetworkObject playerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, playerRef, (runner, obj) => {
                            var playerComp = obj.GetComponent<Player>();
                            if (playerComp != null)
                            {
                                playerComp.PlayerColor = selectedColor;
                            }
                        });

                        // Cập nhật PlayerObject của người chơi sang đối tượng gameplay Player
                        runner.SetPlayerObject(playerRef, playerObject);
                        Debug.Log($"[FusionNetworkRunner] Host spawned gameplay player for player {playerRef.PlayerId} with color {selectedColor}");
                    }

                    // Dọn dẹp các đối tượng LobbyPlayer sau khi đã chuyển thông tin màu sắc xong
                    foreach (var lp in lobbyPlayers)
                    {
                        if (lp.Object != null && lp.Object.IsValid)
                        {
                            runner.Despawn(lp.Object);
                        }
                    }
                }

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
                    if (runner != null && (runner.GameMode == GameMode.Host || runner.GameMode == GameMode.Client || runner.GameMode == GameMode.Shared))
                    {
                        UIManager.Instance.ShowScreen<RoomWaitingScreen>();
                    }
                    else
                    {
                        UIManager.Instance.ShowScreen<MainMenuScreen>();
                    }
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
