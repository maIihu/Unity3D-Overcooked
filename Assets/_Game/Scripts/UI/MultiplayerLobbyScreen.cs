using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Fusion;
using GameCore;
using GameCore.Network;

namespace _Game.Scripts.UI
{
    public class MultiplayerLobbyScreen : ScreenUI
    {
        [Header("Room Creation")]
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private Button createRoomButton;

        [Header("Room Browser")]
        [SerializeField] private RectTransform roomListContainer;
        [SerializeField] private GameObject roomItemPrefab;
        [SerializeField] private TextMeshProUGUI emptyLobbyText;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        private List<GameObject> _instantiatedRoomItems = new List<GameObject>();

        public override void Initialize(UIManager uiManager)
        {
            base.Initialize(uiManager);

            if (createRoomButton != null)
            {
                createRoomButton.onClick.RemoveAllListeners();
                createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        public override void Active()
        {
            base.Active();
            
            // Đăng ký nhận sự kiện cập nhật danh sách phòng
            FusionNetworkRunner.OnSessionListChanged += RefreshSessionList;

            // Kết nối vào sảnh để lấy danh sách phòng
            if (FusionNetworkRunner.Instance != null)
            {
                Debug.Log("[MultiplayerLobbyScreen] Connecting to lobby...");
                FusionNetworkRunner.Instance.JoinLobby();
            }

            if (roomNameInput != null)
            {
                roomNameInput.text = "";
            }

            RefreshSessionList(new List<SessionInfo>());
        }

        public override void Deactive()
        {
            base.Deactive();
            FusionNetworkRunner.OnSessionListChanged -= RefreshSessionList;
            ClearRoomList();
        }

        private void OnCreateRoomClicked()
        {
            string roomName = roomNameInput != null ? roomNameInput.text : "";
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogWarning("[MultiplayerLobbyScreen] Room name cannot be empty!");
                return;
            }

            if (FusionNetworkRunner.Instance != null)
            {
                Debug.Log($"[MultiplayerLobbyScreen] Hosting session: {roomName}");
                FusionNetworkRunner.Instance.StartGameSession(GameMode.Host, roomName);
                
                // Sẽ chuyển sang RoomWaitingScreen ở Phase 4, hiện tại Phase 3 vào thẳng trận đấu
                /*
                if (uiManager != null)
                {
                    uiManager.ShowScreen<RoomWaitingScreen>();
                }
                */
            }
        }

        private void OnJoinRoomClicked(string roomName)
        {
            if (FusionNetworkRunner.Instance != null)
            {
                Debug.Log($"[MultiplayerLobbyScreen] Joining session: {roomName}");
                FusionNetworkRunner.Instance.StartGameSession(GameMode.Client, roomName);

                // Sẽ chuyển sang RoomWaitingScreen ở Phase 4, hiện tại Phase 3 vào thẳng trận đấu
                /*
                if (uiManager != null)
                {
                    uiManager.ShowScreen<RoomWaitingScreen>();
                }
                */
            }
        }

        private void OnBackClicked()
        {
            if (FusionNetworkRunner.Instance != null)
            {
                FusionNetworkRunner.Instance.LeaveSession();
            }

            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.ResetMode();
            }

            if (uiManager != null)
            {
                uiManager.ShowScreen<MainMenuScreen>();
            }
        }

        private void RefreshSessionList(List<SessionInfo> sessionList)
        {
            ClearRoomList();

            bool hasRooms = sessionList != null && sessionList.Count > 0;
            if (emptyLobbyText != null)
            {
                emptyLobbyText.gameObject.SetActive(!hasRooms);
            }

            if (!hasRooms) return;

            foreach (var session in sessionList)
            {
                // Chỉ hiển thị phòng có thể tham gia
                if (!session.IsVisible || session.PlayerCount >= session.MaxPlayers) continue;

                GameObject itemObj = Instantiate(roomItemPrefab, roomListContainer);
                if (itemObj != null)
                {
                    _instantiatedRoomItems.Add(itemObj);
                    LobbyRoomItem roomItem = itemObj.GetComponent<LobbyRoomItem>();
                    if (roomItem != null)
                    {
                        roomItem.Setup(session, OnJoinRoomClicked);
                    }
                }
            }
        }

        private void ClearRoomList()
        {
            foreach (var item in _instantiatedRoomItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            _instantiatedRoomItems.Clear();
        }

        protected override void OnScreenDestroyed()
        {
        }
    }
}
