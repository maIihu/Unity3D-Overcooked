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

        [Header("Loading UI")]
        [SerializeField] private GameObject loadingOverlayUI;
        [SerializeField] private TextMeshProUGUI loadingOverlayText;

        private List<GameObject> _instantiatedRoomItems = new List<GameObject>();
        private GameObject _loadingOverlay;

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

            if (emptyLobbyText != null)
            {
                emptyLobbyText.text = "Connecting to lobby...";
                emptyLobbyText.gameObject.SetActive(true);
            }
        }

        public override void Deactive()
        {
            base.Deactive();
            FusionNetworkRunner.OnSessionListChanged -= RefreshSessionList;
            ClearRoomList();
            HideLoadingOverlay();
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
                ShowLoadingOverlay("Creating Room...\nPlease wait.");
                FusionNetworkRunner.Instance.StartGameSession(GameMode.Host, roomName);
            }
        }

        private void OnJoinRoomClicked(string roomName)
        {
            if (FusionNetworkRunner.Instance != null)
            {
                Debug.Log($"[MultiplayerLobbyScreen] Joining session: {roomName}");
                ShowLoadingOverlay("Joining Room...\nPlease wait.");
                FusionNetworkRunner.Instance.StartGameSession(GameMode.Client, roomName);
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

            // Lọc bỏ các phòng ma (phòng rỗng có PlayerCount <= 0)
            List<SessionInfo> activeRooms = new List<SessionInfo>();
            if (sessionList != null)
            {
                foreach (var session in sessionList)
                {
                    if (session.IsVisible && session.PlayerCount > 0 && session.PlayerCount < session.MaxPlayers)
                    {
                        activeRooms.Add(session);
                    }
                }
            }

            bool hasRooms = activeRooms.Count > 0;
            if (emptyLobbyText != null)
            {
                emptyLobbyText.text = "No active rooms found. Create one to start!";
                emptyLobbyText.gameObject.SetActive(!hasRooms);
            }

            if (!hasRooms) return;

            foreach (var session in activeRooms)
            {
                GameObject itemObj = Instantiate(roomItemPrefab, roomListContainer);
                if (itemObj != null)
                {
                    itemObj.SetActive(true); // Đảm bảo clone được kích hoạt hiển thị
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

        private void ShowLoadingOverlay(string message)
        {
            if (loadingOverlayUI != null)
            {
                loadingOverlayUI.SetActive(true);
                if (loadingOverlayText != null)
                {
                    loadingOverlayText.text = message;
                }
                return;
            }

            // Fallback for when prefab/UI is not assigned
            if (_loadingOverlay == null)
            {
                _loadingOverlay = new GameObject("ConnectingOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                _loadingOverlay.transform.SetParent(transform, false);
                
                RectTransform rt = _loadingOverlay.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                
                Image img = _loadingOverlay.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0.75f);

                GameObject textObj = new GameObject("LoadingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(_loadingOverlay.transform, false);
                
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.sizeDelta = Vector2.zero;
                
                TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
                txt.text = message;
                txt.fontSize = 32;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                
                if (emptyLobbyText != null)
                {
                    txt.font = emptyLobbyText.font;
                }
            }
            else
            {
                var txt = _loadingOverlay.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = message;
            }
            
            _loadingOverlay.SetActive(true);
        }

        private void HideLoadingOverlay()
        {
            if (loadingOverlayUI != null)
            {
                loadingOverlayUI.SetActive(false);
            }
            
            if (_loadingOverlay != null)
            {
                _loadingOverlay.SetActive(false);
            }
        }

        protected override void OnScreenDestroyed()
        {
        }
    }
}
