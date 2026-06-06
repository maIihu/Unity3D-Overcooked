using Fusion;
using UnityEngine;

namespace GameCore.Network
{
    public enum EPlayerColor
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Purple = 4,
        Orange = 5
    }
    
    public class LobbyPlayer : NetworkBehaviour
    {
        [Networked] public NetworkBool IsReady { get; set; }
        [Networked] public EPlayerColor PlayerColor { get; set; }

        public override void Spawned()
        {
            // Đăng ký vào registry để FusionNetworkRunner có thể truy cập mà không cần FindObjectsOfType
            LobbyPlayerRegistry.Register(this);

            if (Object.HasInputAuthority)
            {
                Debug.Log($"[LobbyPlayer] Spawned for local player {Object.InputAuthority}");
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            LobbyPlayerRegistry.Unregister(this);
        }

        public void ToggleReady()
        {
            if (!HasInputAuthority) return;
            RPC_SetReady(!IsReady);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SetReady(NetworkBool ready)
        {
            IsReady = ready;
        }

        public void CycleColor()
        {
            if (!HasInputAuthority) return;
            RPC_ChangeColor((EPlayerColor)(((int)PlayerColor + 1) % 6));
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_ChangeColor(EPlayerColor color)
        {
            PlayerColor = color;
        }
    }
}
