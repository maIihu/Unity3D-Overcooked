using System.Collections.Generic;

namespace GameCore.Network
{
    public static class LobbyPlayerRegistry
    {
        public class PlayerData
        {
            public Fusion.PlayerRef PlayerRef;
            public EPlayerColor Color;
        }

        private static readonly List<PlayerData> _all = new();

        public static IReadOnlyList<PlayerData> All => _all;

        public static void UpdatePlayer(Fusion.PlayerRef playerRef, EPlayerColor color)
        {
            var data = _all.Find(x => x.PlayerRef == playerRef);
            if (data == null)
            {
                data = new PlayerData { PlayerRef = playerRef };
                _all.Add(data);
            }
            data.Color = color;
        }

        public static void RemovePlayer(Fusion.PlayerRef playerRef)
        {
            _all.RemoveAll(x => x.PlayerRef == playerRef);
        }

        public static void Clear() => _all.Clear();
    }
}
