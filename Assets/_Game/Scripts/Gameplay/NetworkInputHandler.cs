// NetworkInputHandler.cs — gắn vào cùng GameObject với FusionNetworkRunner

using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace _Game.Scripts.Gameplay
{
    public class NetworkInputHandler : MonoBehaviour, INetworkRunnerCallbacks
    {
        private byte _pendingButtons;
        private float _lastX;
        private float _lastY;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) _pendingButtons |= NetworkInputData.INTERACT;
            if (Input.GetKeyDown(KeyCode.R))     _pendingButtons |= NetworkInputData.ALTERNATE;

            _lastX = Input.GetAxisRaw("Horizontal");
            _lastY = Input.GetAxisRaw("Vertical");
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData();

            float x = _lastX;
            float y = _lastY;

            float mag = Mathf.Sqrt(x * x + y * y);
            if (mag > 0f) { x /= mag; y /= mag; }

            data.MoveX = (sbyte)Mathf.RoundToInt(x);
            data.MoveY = (sbyte)Mathf.RoundToInt(y);
            data.Buttons = _pendingButtons;

            _pendingButtons = 0;

            input.Set(data);
        }

        public void OnPlayerJoined(NetworkRunner r, PlayerRef p) { }
        public void OnPlayerLeft(NetworkRunner r, PlayerRef p) { }
        public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput i) { }
        public void OnShutdown(NetworkRunner r, ShutdownReason s) { }
        public void OnConnectedToServer(NetworkRunner r) { }
        public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) { }
        public void OnConnectFailed(NetworkRunner r, NetAddress addr, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr msg) { }
        public void OnSessionListUpdated(NetworkRunner r, System.Collections.Generic.List<SessionInfo> list) { }
        public void OnCustomAuthenticationResponse(NetworkRunner r, System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner r, HostMigrationToken token) { }
        public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float progress) { }
        public void OnSceneLoadDone(NetworkRunner r) { }
        public void OnSceneLoadStart(NetworkRunner r) { }
        public void OnObjectExitAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
        public void OnObjectEnterAOI(NetworkRunner r, NetworkObject o, PlayerRef p) { }
    }
}