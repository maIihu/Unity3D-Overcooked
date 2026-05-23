// NetworkInputHandler.cs — gắn vào cùng GameObject với FusionNetworkRunner

using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace _Game.Scripts.Gameplay
{
    public class NetworkInputHandler : MonoBehaviour, INetworkRunnerCallbacks
    {
        // Capture GetKeyDown ở Update level bằng cách tích lũy buttons

        private byte _pendingButtons;

        private void Update()
        {
            // Tích lũy GetKeyDown giữa các fixed ticks
            if (Input.GetKeyDown(KeyCode.Space)) _pendingButtons |= NetworkInputData.INTERACT;
            if (Input.GetKeyDown(KeyCode.R))     _pendingButtons |= NetworkInputData.ALTERNATE;
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData();

            float x = 0, y = 0;
            if (Input.GetKey(KeyCode.W)) y += 1;
            if (Input.GetKey(KeyCode.A)) x -= 1;
            if (Input.GetKey(KeyCode.S)) y -= 1;
            if (Input.GetKey(KeyCode.D)) x += 1;

            float mag = Mathf.Sqrt(x * x + y * y);
            if (mag > 0) { x /= mag; y /= mag; }

            data.MoveX = x;
            data.MoveY = y;
            data.Buttons = _pendingButtons;
            
            // Reset sau khi đã gửi để tránh fire nhiều lần
            _pendingButtons = 0;

            input.Set(data);
        }

        // --- Empty callbacks ---
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