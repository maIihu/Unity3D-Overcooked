using System.Collections.Generic;

namespace GameCore.Network
{
    /// <summary>
    /// Static registry cho LobbyPlayer — tránh dùng FindObjectsOfType mỗi lần cần danh sách.
    /// LobbyPlayer tự Register/Unregister trong Spawned/Despawned.
    /// </summary>
    public static class LobbyPlayerRegistry
    {
        private static readonly List<LobbyPlayer> _all = new();

        /// <summary>Danh sách tất cả LobbyPlayer đang active trong scene.</summary>
        public static IReadOnlyList<LobbyPlayer> All => _all;

        public static void Register(LobbyPlayer lp)
        {
            if (!_all.Contains(lp))
                _all.Add(lp);
        }

        public static void Unregister(LobbyPlayer lp) => _all.Remove(lp);
    }
}
