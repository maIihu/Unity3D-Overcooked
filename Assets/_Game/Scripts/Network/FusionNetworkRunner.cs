using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private GameObject _runnerGo;
        private NetworkInputHandler _inputHandler;

        private bool _isStarting;
        private bool _userInitiatedLeave = false;

        private const string SCENE_GAME = "GameScene";
        private const string SCENE_MAIN_MENU = "MainMenuScene";
        private const string SCENE_LOBBY = "LobbyScene";

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

        public async Task<bool> StartGameSession(GameMode mode, string sessionName)
        {
            while (_isStarting)
            {
                await Task.Delay(100);
            }
            
            _isStarting = true;
            _userInitiatedLeave = false;
            try
            {
                // Don't shutdown if we are reusing the Lobby Runner, to ensure we stay on the same Region
                if (_runner == null || _runnerGo == null || mode == GameMode.Single)
                {
                    await ShutdownExistingRunner();

                    _runnerGo = new GameObject("FusionNetworkRunner_Instance");
                    DontDestroyOnLoad(_runnerGo);

                    _runner = _runnerGo.AddComponent<NetworkRunner>();
                    var physics = _runnerGo.AddComponent<Fusion.Addons.Physics.RunnerSimulatePhysics3D>();
                    physics.ClientPhysicsSimulation = Fusion.Addons.Physics.ClientPhysicsSimulation.SimulateForward;
                    _runner.ProvideInput = true;
                    _runner.AddCallbacks(this);
                }

                _inputHandler = _runnerGo.GetComponent<NetworkInputHandler>();
                if (_inputHandler == null)
                {
                    _inputHandler = _runnerGo.AddComponent<NetworkInputHandler>();
                    Debug.Log("[FusionNetworkRunner] InputHandler added to runner GameObject.");
                }
                _runner.AddCallbacks(_inputHandler);

                int sceneIndex = (mode == GameMode.Single)
                    ? GameCore.Loader.GetSceneIndex("GameScene")
                    : GameCore.Loader.GetSceneIndex("MainMenuScene");

                var sceneManager = _runnerGo.GetComponent<NetworkSceneManagerDefault>();
                if (sceneManager == null)
                {
                    sceneManager = _runnerGo.AddComponent<NetworkSceneManagerDefault>();
                }

                var args = new StartGameArgs()
                {
                    GameMode = mode,
                    SessionName  = sessionName,
                    SceneManager = sceneManager,
                };

                if (mode == GameMode.Host || mode == GameMode.Server || mode == GameMode.Single)
                {
                    args.Scene = SceneRef.FromIndex(sceneIndex);
                }

                var result = await _runner.StartGame(args);

                if (result.Ok)
                {
                    Debug.Log($"Started Fusion in {mode} mode.");
                    return true;
                }
                else
                {
                    Debug.LogError($"Failed to start Fusion: {result.ShutdownReason}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception during StartGame: {ex}");
                return false;
            }
            finally
            {
                _isStarting = false;
            }
        }

        public static event Action<List<SessionInfo>> OnSessionListChanged;

        public async Task JoinLobby()
        {
            if (_isStarting) return;
            _isStarting = true;
            _userInitiatedLeave = false;
            try
            {
                await ShutdownExistingRunner();

                _runnerGo = new GameObject("FusionNetworkRunner_Instance");
                DontDestroyOnLoad(_runnerGo);

                _runner = _runnerGo.AddComponent<NetworkRunner>();
                var physics = _runnerGo.AddComponent<Fusion.Addons.Physics.RunnerSimulatePhysics3D>();
                physics.ClientPhysicsSimulation = Fusion.Addons.Physics.ClientPhysicsSimulation.SimulateForward;
                _runner.ProvideInput = true;
                _runner.AddCallbacks(this);

                var result = await _runner.JoinSessionLobby(SessionLobby.ClientServer);

                if (result.Ok)
                    Debug.Log("Joined Fusion Lobby successfully.");
                else
                    Debug.LogError($"Failed to join Fusion Lobby: {result.ShutdownReason}");
            }
            finally
            {
                _isStarting = false;
            }
        }

        public async Task LeaveSession()
        {
            if (_isStarting) return;
            _isStarting = true;
            try
            {
                _userInitiatedLeave = true;
                await ShutdownExistingRunner();
                _inputHandler = null;
            }
            finally
            {
                _isStarting = false;
            }
        }

        private async Task ShutdownExistingRunner()
        {
            if (_runner == null) return;

            Debug.Log("[FusionNetworkRunner] Shutting down active runner.");
            var oldRunner = _runner;
            _runner = null;
            GameCore.Network.LobbyPlayerRegistry.Clear();
            await oldRunner.Shutdown();

            if (_runnerGo != null)
            {
                Destroy(_runnerGo);  
                _runnerGo = null;
            }
            await Task.Delay(300);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer) return;

            if (runner.GameMode == GameMode.Single || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == SCENE_GAME)
            {
                Debug.Log($"[FusionNetworkRunner] Player {player.PlayerId} joined during gameplay. Skipping LobbyPlayer spawn.");
            }
            else
            {
                NetworkObject lobbyPlayerObject = runner.Spawn(lobbyPlayerPrefab, Vector3.zero, Quaternion.identity, player);
                runner.SetPlayerObject(player, lobbyPlayerObject);
                runner.MakeDontDestroyOnLoad(lobbyPlayerObject.gameObject);
                Debug.Log($"[FusionNetworkRunner] Host spawned lobby player for player {player.PlayerId}");
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer) return;

            NetworkObject playerObject = runner.GetPlayerObject(player);
            if (playerObject != null)
            {
                runner.Despawn(playerObject);
                Debug.Log($"Host despawned player object for player {player.PlayerId}");
            }
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[FusionNetworkRunner] OnSceneLoadDone: {sceneName}");

            if      (sceneName == SCENE_GAME)      HandleGameSceneLoaded(runner);
            else if (sceneName == SCENE_MAIN_MENU) HandleMenuSceneLoaded(runner);
            else if (sceneName == SCENE_LOBBY)     HandleMenuSceneLoaded(runner);
            else    Debug.Log($"[FusionNetworkRunner] Unhandled scene: {sceneName}");
        }

        private void HandleGameSceneLoaded(NetworkRunner runner)
        {
            StartCoroutine(GameSceneLoadedCoroutine(runner));
        }

        private IEnumerator GameSceneLoadedCoroutine(NetworkRunner runner)
        {
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnLoadLevel));

            yield return new WaitForSeconds(1f);

            if (runner != null && runner.IsServer)
            {
                SpawnGameplayPlayersFromLobby(runner);
            }

            yield return new WaitForSeconds(0.5f);

            var loadingScreen = UIManager.Instance?.GetScreen<LoadingScreen>();
            if (loadingScreen != null && loadingScreen.gameObject.activeSelf)
            {
                bool completed = false;
                loadingScreen.CompleteProgress(() => completed = true);
                yield return new WaitUntil(() => completed);
                UIManager.Instance?.ShowScreen<GameplayScreen>();
            }
            else
            {
                UIManager.Instance?.ShowScreen<GameplayScreen>();
            }
        }

        private void HandleMenuSceneLoaded(NetworkRunner runner)
        {
            if (UIManager.Instance == null) return;

            if (runner != null && (runner.GameMode == GameMode.Host ||
                                   runner.GameMode == GameMode.Client ||
                                   runner.GameMode == GameMode.Shared))
            {
                UIManager.Instance.ShowScreen<RoomWaitingScreen>();
            }
            else
            {
                UIManager.Instance.ShowScreen<MainMenuScreen>();
            }
        }

        private void SpawnGameplayPlayersFromLobby(NetworkRunner runner)
        {
            var lobbyPlayers = GameCore.Network.LobbyPlayerRegistry.All;
            var playerColors = new Dictionary<PlayerRef, EPlayerColor>();
            
            foreach (var p in lobbyPlayers)
            {
                if (p != null)
                {
                    playerColors[p.PlayerRef] = p.Color;
                }
            }

            foreach (var playerRef in runner.ActivePlayers)
            {
                Vector3 spawnPosition = new Vector3((playerRef.PlayerId % 4) * 2f, 1, 0);

                if (runner.GameMode == GameMode.Single)
                {
                    if (GameManager.Instance?.LocalPlayerPrefab != null)
                    {
                        Instantiate(GameManager.Instance.LocalPlayerPrefab, spawnPosition, Quaternion.identity);
                        Debug.Log("[FusionNetworkRunner] Spawned PlayerLocal for Singleplayer.");
                    }
                    continue;
                }

                EPlayerColor selectedColor = playerColors.TryGetValue(playerRef, out EPlayerColor lpColor)
                    ? lpColor
                    : EPlayerColor.Red;

                NetworkObject playerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, playerRef,
                    (r, obj) =>
                    {
                        var playerComp = obj.GetComponent<Player>();
                        if (playerComp != null) playerComp.PlayerColor = selectedColor;
                    });

                runner.SetPlayerObject(playerRef, playerObject);
                Debug.Log($"[FusionNetworkRunner] Spawned gameplay player {playerRef.PlayerId} with color {selectedColor}");
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (_userInitiatedLeave || shutdownReason == ShutdownReason.Ok)
            {
                _userInitiatedLeave = false;
                return;
            }

            string msg = "Phòng chơi đã bị đóng hoặc chủ phòng đã thoát game.";
            if (shutdownReason == ShutdownReason.HostMigration)
            {
                msg = "Chủ phòng đã thoát game. Đang chuyển về Menu.";
            }

            HandleDisconnect(msg);
        }

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            HandleDisconnect($"Mất kết nối với máy chủ. Lý do: {reason}");
        }

        private void HandleDisconnect(string message)
        {
            Debug.LogWarning($"[FusionNetworkRunner] Disconnected: {message}");

            if (_runner != null)
            {
                _ = ShutdownExistingRunner();
            }

            if (UIManager.Instance != null)
            {
                var popup = UIManager.Instance.GetPopup<_Game.Scripts.UI.PopupDisconnect>();
                if (popup != null)
                {
                    popup.SetMessage(message);
                    UIManager.Instance.ShowPopup<_Game.Scripts.UI.PopupDisconnect>();
                }
            }
        }

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
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}
