using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Fusion;

namespace _Game.Scripts.UI
{
    public class LobbyRoomItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button joinButton;

        public void Setup(SessionInfo session, Action<string> onJoinClick)
        {
            if (roomNameText != null)
            {
                roomNameText.text = session.Name;
            }

            if (playerCountText != null)
            {
                playerCountText.text = $"{session.PlayerCount}/{session.MaxPlayers}";
            }

            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(() => onJoinClick?.Invoke(session.Name));
            }
        }
    }
}
