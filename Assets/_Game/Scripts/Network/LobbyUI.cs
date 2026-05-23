using UnityEngine;

namespace GameCore.Network
{
    public class LobbyUI : MonoBehaviour
    {
        private FusionNetworkRunner _networkRunner;
        private string _roomName = "OvercookedRoom";

        private void Awake()
        {
            _networkRunner = GetComponent<FusionNetworkRunner>();
            if (_networkRunner == null)
            {
                _networkRunner = FindObjectOfType<FusionNetworkRunner>();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 150));
            
            GUILayout.Label("Room Name:");
            _roomName = GUILayout.TextField(_roomName);

            if (GUILayout.Button("Join / Start Shared Game", GUILayout.Height(50)))
            {
                if (_networkRunner != null)
                {
                    _networkRunner.StartGameSession(Fusion.GameMode.Shared, _roomName);
                    // Hide UI while loading
                    this.enabled = false; 
                }
            }

            GUILayout.EndArea();
        }
    }
}
