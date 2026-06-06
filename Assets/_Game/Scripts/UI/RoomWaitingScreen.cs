using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Fusion;
using GameCore;
using GameCore.Network;
using _Game.Scripts.Gameplay;
using _Game.Scripts.Utilities;

namespace _Game.Scripts.UI
{
    public class RoomWaitingScreen : ScreenUI
    {
        [Header("Room Info")]
        [SerializeField] private TextMeshProUGUI roomNameText;

        [Header("Player List")]
        [SerializeField] private RectTransform playerListContainer;
        [SerializeField] private GameObject playerItemPrefab;

        [Header("Buttons")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button changeColorButton;

        private List<GameObject> _instantiatedPlayerItems = new List<GameObject>();
        private float _refreshTimer = 0f;
        private const float REFRESH_INTERVAL = 0.2f;

        public override void Initialize(UIManager uiManager)
        {
            base.Initialize(uiManager);

            if (readyButton != null)
            {
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(OnReadyClicked);
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                leaveButton.onClick.AddListener(OnLeaveClicked);
            }

            if (changeColorButton != null)
            {
                changeColorButton.onClick.RemoveAllListeners();
                changeColorButton.onClick.AddListener(OnChangeColorClicked);
            }
        }

        public override void Active()
        {
            base.Active();
            
            if (FusionNetworkRunner.Instance != null && FusionNetworkRunner.Instance.Runner != null)
            {
                var runner = FusionNetworkRunner.Instance.Runner;
                if (roomNameText != null)
                {
                    roomNameText.text = $"Room: {runner.SessionInfo.Name}";
                }
            }

            UpdateButtonsState();
            RefreshPlayerList();
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;

            _refreshTimer += Time.deltaTime;
            if (_refreshTimer >= REFRESH_INTERVAL)
            {
                _refreshTimer = 0f;
                // Only refresh UI states, not instantiate/destroy
                RefreshPlayerList();
                UpdateButtonsState();
            }
        }

        private void OnReadyClicked()
        {
            if (FusionNetworkRunner.Instance == null || FusionNetworkRunner.Instance.Runner == null) return;

            var runner = FusionNetworkRunner.Instance.Runner;
            var localPlayerObj = runner.GetPlayerObject(runner.LocalPlayer);
            if (localPlayerObj != null)
            {
                var playerComp = localPlayerObj.GetComponent<LobbyPlayer>();
                if (playerComp != null)
                {
                    playerComp.ToggleReady();
                }
            }
        }

        private void OnStartGameClicked()
        {
            if (FusionNetworkRunner.Instance == null || FusionNetworkRunner.Instance.Runner == null) return;

            var runner = FusionNetworkRunner.Instance.Runner;
            if (runner.IsServer)
            {
                Debug.Log("[RoomWaitingScreen] All players ready. Host is loading GameScene...");
                // Load GameScene (index 1) qua mạng
                runner.LoadScene(SceneRef.FromIndex(1), UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }

        private void OnChangeColorClicked()
        {
            if (FusionNetworkRunner.Instance == null || FusionNetworkRunner.Instance.Runner == null) return;

            var runner = FusionNetworkRunner.Instance.Runner;
            var localPlayerObj = runner.GetPlayerObject(runner.LocalPlayer);
            if (localPlayerObj != null)
            {
                var playerComp = localPlayerObj.GetComponent<LobbyPlayer>();
                if (playerComp != null)
                {
                    playerComp.CycleColor();
                }
            }
        }

        private void OnLeaveClicked()
        {
            if (FusionNetworkRunner.Instance != null)
            {
                FusionNetworkRunner.Instance.LeaveSession().FireAndForget();
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

        private void RefreshPlayerList()
        {
            if (FusionNetworkRunner.Instance == null || FusionNetworkRunner.Instance.Runner == null) return;

            var runner = FusionNetworkRunner.Instance.Runner;
            int activeIndex = 0;

            foreach (var playerRef in runner.ActivePlayers)
            {
                var playerObj = runner.GetPlayerObject(playerRef);
                string playerName = $"Player {playerRef.PlayerId}";
                bool isReady = false;
                bool isHost = false;
                Color playerColor = Color.white;

                if (playerObj != null)
                {
                    var playerComp = playerObj.GetComponent<LobbyPlayer>();
                    if (playerComp != null)
                    {
                        isReady = playerComp.IsReady;
                        playerColor = Player.GetColorByEnum(playerComp.PlayerColor);
                    }
                    
                    isHost = runner.IsServer && (playerRef == runner.LocalPlayer);
                }
                else
                {
                    playerName += " (Spawning...)";
                }

                if (playerRef == runner.LocalPlayer)
                    playerName += " (You)";

                if (isHost)
                    playerName += " [Host]";

                if (activeIndex >= _instantiatedPlayerItems.Count)
                {
                    GameObject itemObj = Instantiate(playerItemPrefab, playerListContainer);
                    _instantiatedPlayerItems.Add(itemObj);
                }

                GameObject currentItem = _instantiatedPlayerItems[activeIndex];
                currentItem.SetActive(true);
                var playerItem = currentItem.GetComponent<RoomWaitingPlayerItem>();
                if (playerItem != null)
                {
                    playerItem.Setup(playerName, isReady, playerColor);
                }
                
                activeIndex++;
            }

            for (int i = activeIndex; i < _instantiatedPlayerItems.Count; i++)
            {
                _instantiatedPlayerItems[i].SetActive(false);
            }
        }

        private void UpdateButtonsState()
        {
            if (FusionNetworkRunner.Instance == null || FusionNetworkRunner.Instance.Runner == null) return;

            var runner = FusionNetworkRunner.Instance.Runner;
            bool isHost = runner.IsServer;

            // Cập nhật hiển thị nút Start Game (chỉ Host thấy)
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(isHost);

                // Chỉ cho phép Host nhấn Start Game khi tất cả người chơi đã Ready
                bool allReady = true;
                int playerCount = 0;
                foreach (var playerRef in runner.ActivePlayers)
                {
                    playerCount++;
                    var playerObj = runner.GetPlayerObject(playerRef);
                    if (playerObj != null)
                    {
                        var playerComp = playerObj.GetComponent<LobbyPlayer>();
                        if (playerComp != null)
                        {
                            if (!playerComp.IsReady) allReady = false;
                        }
                        else
                        {
                            allReady = false;
                        }
                    }
                    else
                    {
                        allReady = false;
                    }
                }

                startGameButton.interactable = allReady && playerCount > 0;
            }

            // Cập nhật text của nút Ready
            if (readyButtonText != null)
            {
                var localPlayerObj = runner.GetPlayerObject(runner.LocalPlayer);
                if (localPlayerObj != null)
                {
                    var playerComp = localPlayerObj.GetComponent<LobbyPlayer>();
                    if (playerComp != null)
                    {
                        readyButtonText.text = playerComp.IsReady ? "NOT READY" : "READY";
                    }
                }
            }
        }

        private void ClearPlayerList()
        {
            foreach (var item in _instantiatedPlayerItems)
            {
                if (item != null)
                {
                    item.SetActive(false);
                }
            }
        }

        protected override void OnScreenDestroyed()
        {
        }
    }
}
