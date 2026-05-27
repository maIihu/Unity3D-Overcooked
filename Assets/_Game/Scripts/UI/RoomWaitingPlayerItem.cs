using UnityEngine;
using TMPro;

namespace _Game.Scripts.UI
{
    public class RoomWaitingPlayerItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI readyStatusText;

        public void Setup(string playerName, bool isReady, Color playerColor)
        {
            if (playerNameText != null)
            {
                playerNameText.text = playerName;
                playerNameText.color = playerColor;
            }

            if (readyStatusText != null)
            {
                readyStatusText.text = isReady ? "READY" : "NOT READY";
                readyStatusText.color = isReady ? Color.green : Color.red;
            }
        }
    }
}
