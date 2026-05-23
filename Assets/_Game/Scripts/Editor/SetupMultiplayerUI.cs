using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using _Game.Scripts.UI;

namespace GameCore.Editor
{
    public class SetupMultiplayerUI
    {
        [MenuItem("Overcooked/Setup Multiplayer UI")]
        public static void DoSetup()
        {
            // 1. Tìm UIManager
            UIManager uiManager = GameObject.FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("[SetupMultiplayerUI] UIManager not found in the active scene!");
                return;
            }

            // 2. Tìm ScreenHolder
            Transform screenHolder = uiManager.transform.Find("Canvas/ScreenHolder");
            if (screenHolder == null)
            {
                Debug.LogError("[SetupMultiplayerUI] Canvas/ScreenHolder not found under UIManager!");
                return;
            }

            // 3. Tìm MainMenuScreen làm mẫu
            Transform mainMenuScreenTransform = screenHolder.Find("MainMenuScreen");
            if (mainMenuScreenTransform == null)
            {
                Debug.LogError("[SetupMultiplayerUI] MainMenuScreen not found under ScreenHolder to use as template!");
                return;
            }

            Undo.RegisterCompleteObjectUndo(uiManager, "Setup Multiplayer UI");
            Undo.RegisterCompleteObjectUndo(screenHolder.gameObject, "Setup Multiplayer UI");

            // 4. Tạo MultiplayerLobbyScreen
            Transform lobbyScreenTransform = screenHolder.Find("MultiplayerLobbyScreen");
            if (lobbyScreenTransform != null)
            {
                Undo.DestroyObjectImmediate(lobbyScreenTransform.gameObject);
            }

            GameObject lobbyScreenObj = GameObject.Instantiate(mainMenuScreenTransform.gameObject, screenHolder);
            lobbyScreenObj.name = "MultiplayerLobbyScreen";
            Undo.RegisterCreatedObjectUndo(lobbyScreenObj, "Setup Multiplayer UI");

            // Xóa component MainMenuScreen và thêm component MultiplayerLobbyScreen
            MainMenuScreen oldMenuComp = lobbyScreenObj.GetComponent<MainMenuScreen>();
            if (oldMenuComp != null)
            {
                GameObject.DestroyImmediate(oldMenuComp);
            }
            MultiplayerLobbyScreen lobbyScreen = lobbyScreenObj.AddComponent<MultiplayerLobbyScreen>();

            // Ẩn screen lúc đầu
            lobbyScreenObj.SetActive(false);

            // Cấu hình các nút và text
            // Tiêu đề
            Transform titleTextTrans = lobbyScreenObj.transform.Find("Text (TMP)");
            if (titleTextTrans != null)
            {
                TextMeshProUGUI titleText = titleTextTrans.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = "MULTIPLAYER LOBBY";
                }
            }

            // Nút Chơi đơn -> Nút Tạo phòng
            Transform createBtnTrans = lobbyScreenObj.transform.Find("Button_SinglePlayer");
            if (createBtnTrans != null)
            {
                createBtnTrans.name = "Button_CreateRoom";
                createBtnTrans.GetComponentInChildren<TextMeshProUGUI>().text = "CREATE ROOM";
                RectTransform rt = createBtnTrans.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(600, 240);
            }

            // Nút Chơi mạng -> Thay bằng Ô Nhập Tên Phòng (Input Field)
            Transform multiBtnTrans = lobbyScreenObj.transform.Find("Button_Multiplayer");
            GameObject inputFieldObj = null;
            if (multiBtnTrans != null)
            {
                RectTransform multiRT = multiBtnTrans.GetComponent<RectTransform>();
                Vector2 size = multiRT.sizeDelta;
                Vector2 pos = new Vector2(600, 360);

                inputFieldObj = new GameObject("RoomNameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
                inputFieldObj.transform.SetParent(lobbyScreenObj.transform, false);
                RectTransform inputRT = inputFieldObj.GetComponent<RectTransform>();
                inputRT.anchorMin = multiRT.anchorMin;
                inputRT.anchorMax = multiRT.anchorMax;
                inputRT.pivot = multiRT.pivot;
                inputRT.sizeDelta = size;
                inputRT.anchoredPosition = pos;

                Image inputImg = inputFieldObj.GetComponent<Image>();
                inputImg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

                GameObject textAreasObj = new GameObject("TextArea", typeof(RectTransform));
                textAreasObj.transform.SetParent(inputFieldObj.transform, false);
                RectTransform textAreasRT = textAreasObj.GetComponent<RectTransform>();
                textAreasRT.anchorMin = Vector2.zero;
                textAreasRT.anchorMax = Vector2.one;
                textAreasRT.sizeDelta = Vector2.zero;

                GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                placeholderObj.transform.SetParent(textAreasObj.transform, false);
                RectTransform placeholderRT = placeholderObj.GetComponent<RectTransform>();
                placeholderRT.anchorMin = Vector2.zero;
                placeholderRT.anchorMax = Vector2.one;
                placeholderRT.sizeDelta = new Vector2(-20, -10);
                TextMeshProUGUI placeholderText = placeholderObj.GetComponent<TextMeshProUGUI>();
                placeholderText.text = "Enter Room Name...";
                placeholderText.fontStyle = FontStyles.Italic;
                placeholderText.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
                placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
                placeholderText.fontSize = 28;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(textAreasObj.transform, false);
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.sizeDelta = new Vector2(-20, -10);
                TextMeshProUGUI textComp = textObj.GetComponent<TextMeshProUGUI>();
                textComp.text = "";
                textComp.color = Color.white;
                textComp.alignment = TextAlignmentOptions.MidlineLeft;
                textComp.fontSize = 28;

                TMP_InputField inputField = inputFieldObj.GetComponent<TMP_InputField>();
                inputField.placeholder = placeholderText;
                inputField.textComponent = textComp;
                inputField.textViewport = textAreasRT;

                Undo.DestroyObjectImmediate(multiBtnTrans.gameObject);
            }

            // Nút Thoát -> Nút Quay lại
            Transform backBtnTrans = lobbyScreenObj.transform.Find("Button_Quit");
            if (backBtnTrans != null)
            {
                backBtnTrans.name = "Button_Back";
                backBtnTrans.GetComponentInChildren<TextMeshProUGUI>().text = "BACK";
                RectTransform rt = backBtnTrans.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-600, 120);
            }

            // 5. Tạo Scroll View hiển thị danh sách phòng
            GameObject scrollViewObj = new GameObject("RoomBrowser", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollViewObj.transform.SetParent(lobbyScreenObj.transform, false);
            RectTransform scrollRT = scrollViewObj.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRT.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRT.pivot = new Vector2(0.5f, 0.5f);
            scrollRT.sizeDelta = new Vector2(600, 500);
            scrollRT.anchoredPosition = new Vector2(300, 60);

            Image scrollImg = scrollViewObj.GetComponent<Image>();
            scrollImg.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(scrollViewObj.transform, false);
            RectTransform viewRT = viewportObj.GetComponent<RectTransform>();
            viewRT.anchorMin = Vector2.zero;
            viewRT.anchorMax = Vector2.one;
            viewRT.sizeDelta = Vector2.zero;
            viewportObj.GetComponent<Mask>().showMaskGraphic = false;
            viewportObj.GetComponent<Image>().color = Color.white;

            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRT = contentObj.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = scrollViewObj.GetComponent<ScrollRect>();
            scrollRect.viewport = viewRT;
            scrollRect.content = contentRT;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject emptyTextObj = new GameObject("EmptyLobbyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            emptyTextObj.transform.SetParent(scrollViewObj.transform, false);
            RectTransform emptyRT = emptyTextObj.GetComponent<RectTransform>();
            emptyRT.anchorMin = Vector2.zero;
            emptyRT.anchorMax = Vector2.one;
            emptyRT.sizeDelta = Vector2.zero;
            TextMeshProUGUI emptyText = emptyTextObj.GetComponent<TextMeshProUGUI>();
            emptyText.text = "No active rooms found.\nCreate one on the left!";
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.fontSize = 24;
            emptyText.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);

            // 6. Tạo Item mẫu cho từng phòng trong sảnh (LobbyRoomItemTemplate)
            GameObject itemTemplate = new GameObject("LobbyRoomItemTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LobbyRoomItem));
            itemTemplate.transform.SetParent(lobbyScreenObj.transform, false);
            itemTemplate.SetActive(false); // Ẩn template

            RectTransform itemRT = itemTemplate.GetComponent<RectTransform>();
            itemRT.sizeDelta = new Vector2(580, 80);

            Image itemImg = itemTemplate.GetComponent<Image>();
            itemImg.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);

            HorizontalLayoutGroup hlg = itemTemplate.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(15, 15, 10, 10);
            hlg.spacing = 15;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            GameObject nameTextObj = new GameObject("RoomName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            nameTextObj.transform.SetParent(itemTemplate.transform, false);
            TextMeshProUGUI nameText = nameTextObj.GetComponent<TextMeshProUGUI>();
            nameText.text = "Room Name";
            nameText.fontSize = 24;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject countTextObj = new GameObject("PlayerCount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            countTextObj.transform.SetParent(itemTemplate.transform, false);
            TextMeshProUGUI countText = countTextObj.GetComponent<TextMeshProUGUI>();
            countText.text = "1/4";
            countText.fontSize = 24;
            countText.alignment = TextAlignmentOptions.MidlineRight;
            RectTransform countRT = countTextObj.GetComponent<RectTransform>();
            countRT.sizeDelta = new Vector2(100, 60);

            // Nút Join (Nhân bản từ nút Back)
            GameObject joinBtnObj = GameObject.Instantiate(backBtnTrans.gameObject, itemTemplate.transform);
            joinBtnObj.name = "Button_Join";
            RectTransform joinRT = joinBtnObj.GetComponent<RectTransform>();
            joinRT.sizeDelta = new Vector2(120, 50);
            joinBtnObj.GetComponentInChildren<TextMeshProUGUI>().text = "JOIN";
            joinBtnObj.GetComponentInChildren<TextMeshProUGUI>().fontSize = 20;

            LobbyRoomItem roomItem = itemTemplate.GetComponent<LobbyRoomItem>();
            SerializedObject itemSO = new SerializedObject(roomItem);
            itemSO.FindProperty("roomNameText").objectReferenceValue = nameText;
            itemSO.FindProperty("playerCountText").objectReferenceValue = countText;
            itemSO.FindProperty("joinButton").objectReferenceValue = joinBtnObj.GetComponent<Button>();
            itemSO.ApplyModifiedProperties();

            // Link references cho MultiplayerLobbyScreen
            SerializedObject lobbySO = new SerializedObject(lobbyScreen);
            lobbySO.FindProperty("roomNameInput").objectReferenceValue = inputFieldObj.GetComponent<TMP_InputField>();
            lobbySO.FindProperty("createRoomButton").objectReferenceValue = createBtnTrans.GetComponent<Button>();
            lobbySO.FindProperty("backButton").objectReferenceValue = backBtnTrans.GetComponent<Button>();
            lobbySO.FindProperty("roomListContainer").objectReferenceValue = contentRT;
            lobbySO.FindProperty("roomItemPrefab").objectReferenceValue = itemTemplate;
            lobbySO.FindProperty("emptyLobbyText").objectReferenceValue = emptyText;
            lobbySO.ApplyModifiedProperties();


            // 7. Tạo RoomWaitingScreen (Tạm thời bỏ qua ở Phase 3 theo yêu cầu của User)
            /*
            Transform waitingScreenTransform = screenHolder.Find("RoomWaitingScreen");
            if (waitingScreenTransform != null)
            {
                Undo.DestroyObjectImmediate(waitingScreenTransform.gameObject);
            }

            GameObject waitingScreenObj = GameObject.Instantiate(lobbyScreenObj, screenHolder);
            waitingScreenObj.name = "RoomWaitingScreen";
            Undo.RegisterCreatedObjectUndo(waitingScreenObj, "Setup Multiplayer UI");

            MultiplayerLobbyScreen oldLobbyComp = waitingScreenObj.GetComponent<MultiplayerLobbyScreen>();
            if (oldLobbyComp != null)
            {
                GameObject.DestroyImmediate(oldLobbyComp);
            }
            RoomWaitingScreen waitingScreen = waitingScreenObj.AddComponent<RoomWaitingScreen>();

            waitingScreenObj.SetActive(false);

            // Thay đổi text tiêu đề thành ROOM LOBBY
            Transform waitTitleTextTrans = waitingScreenObj.transform.Find("Text (TMP)");
            if (waitTitleTextTrans != null)
            {
                TextMeshProUGUI titleText = waitTitleTextTrans.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    titleText.text = "ROOM LOBBY";
                }
            }

            // Label hiển thị tên phòng (thay thế ô Input nhập tên phòng)
            Transform waitInputFieldTrans = waitingScreenObj.transform.Find("RoomNameInput");
            TextMeshProUGUI waitingRoomNameText = null;
            if (waitInputFieldTrans != null)
            {
                GameObject.DestroyImmediate(waitInputFieldTrans.gameObject);

                GameObject textLabelObj = new GameObject("RoomNameLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textLabelObj.transform.SetParent(waitingScreenObj.transform, false);
                RectTransform labelRT = textLabelObj.GetComponent<RectTransform>();
                labelRT.anchorMin = new Vector2(0.5f, 0.5f);
                labelRT.anchorMax = new Vector2(0.5f, 0.5f);
                labelRT.pivot = new Vector2(0.5f, 0.5f);
                labelRT.sizeDelta = new Vector2(400, 80);
                labelRT.anchoredPosition = new Vector2(600, 360);

                waitingRoomNameText = textLabelObj.GetComponent<TextMeshProUGUI>();
                waitingRoomNameText.text = "Room: -";
                waitingRoomNameText.fontSize = 32;
                waitingRoomNameText.alignment = TextAlignmentOptions.Center;
            }

            // Sắp xếp lại các nút
            // Nút Tạo phòng -> Nút Sẵn sàng (Ready)
            Transform readyBtnTrans = waitingScreenObj.transform.Find("Button_CreateRoom");
            if (readyBtnTrans != null)
            {
                readyBtnTrans.name = "Button_Ready";
                readyBtnTrans.GetComponentInChildren<TextMeshProUGUI>().text = "READY";
                RectTransform rt = readyBtnTrans.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(600, 240);
            }

            // Nút Back -> Nút Thoát (Leave)
            Transform leaveBtnTrans = waitingScreenObj.transform.Find("Button_Back");
            if (leaveBtnTrans != null)
            {
                leaveBtnTrans.name = "Button_Leave";
                leaveBtnTrans.GetComponentInChildren<TextMeshProUGUI>().text = "LEAVE";
                RectTransform rt = leaveBtnTrans.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-600, 120);
            }

            // Thêm nút Bắt đầu (Start Game) cho Chủ phòng (Host) ở góc phải bên dưới nút Ready
            GameObject startGameBtnObj = GameObject.Instantiate(readyBtnTrans.gameObject, waitingScreenObj.transform);
            startGameBtnObj.name = "Button_StartGame";
            RectTransform startRT = startGameBtnObj.GetComponent<RectTransform>();
            startRT.anchoredPosition = new Vector2(600, 120);
            startGameBtnObj.GetComponentInChildren<TextMeshProUGUI>().text = "START GAME";

            // Sửa tên Scroll View thành PlayerList
            Transform roomBrowserTrans = waitingScreenObj.transform.Find("RoomBrowser");
            Transform playerContentRT = null;
            if (roomBrowserTrans != null)
            {
                roomBrowserTrans.name = "PlayerList";
                playerContentRT = roomBrowserTrans.Find("Viewport/Content");
                
                Transform emptyLobbyTextTrans = roomBrowserTrans.Find("EmptyLobbyText");
                if (emptyLobbyTextTrans != null)
                {
                    emptyLobbyTextTrans.name = "EmptyPlayerListText";
                    TextMeshProUGUI playerEmptyText = emptyLobbyTextTrans.GetComponent<TextMeshProUGUI>();
                    playerEmptyText.text = "Waiting for players...";
                }
            }

            // Sửa Item mẫu từ Phòng (LobbyRoomItemTemplate) thành Player (RoomWaitingPlayerItemTemplate)
            Transform itemTemplateTrans = waitingScreenObj.transform.Find("LobbyRoomItemTemplate");
            GameObject playerItemTemplate = null;
            if (itemTemplateTrans != null)
            {
                playerItemTemplate = itemTemplateTrans.gameObject;
                playerItemTemplate.name = "RoomWaitingPlayerItemTemplate";

                LobbyRoomItem oldItemComp = playerItemTemplate.GetComponent<LobbyRoomItem>();
                if (oldItemComp != null)
                {
                    GameObject.DestroyImmediate(oldItemComp);
                }
                RoomWaitingPlayerItem playerItem = playerItemTemplate.AddComponent<RoomWaitingPlayerItem>();

                // Xóa nút Join trên dòng tên người chơi
                Transform btnJoinTrans = playerItemTemplate.transform.Find("Button_Join");
                if (btnJoinTrans != null)
                {
                    GameObject.DestroyImmediate(btnJoinTrans.gameObject);
                }

                // Sửa RoomName thành PlayerName
                Transform plNameTrans = playerItemTemplate.transform.Find("RoomName");
                if (plNameTrans != null)
                {
                    plNameTrans.name = "PlayerName";
                }

                // Sửa PlayerCount thành ReadyStatus hiển thị trạng thái sẵn sàng
                Transform statusTrans = playerItemTemplate.transform.Find("PlayerCount");
                if (statusTrans != null)
                {
                    statusTrans.name = "ReadyStatus";
                    RectTransform rt = statusTrans.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(200, 60);
                }

                SerializedObject pItemSO = new SerializedObject(playerItem);
                pItemSO.FindProperty("playerNameText").objectReferenceValue = plNameTrans.GetComponent<TextMeshProUGUI>();
                pItemSO.FindProperty("readyStatusText").objectReferenceValue = statusTrans.GetComponent<TextMeshProUGUI>();
                pItemSO.ApplyModifiedProperties();
            }

            // Link references cho RoomWaitingScreen
            SerializedObject waitingSO = new SerializedObject(waitingScreen);
            waitingSO.FindProperty("roomNameText").objectReferenceValue = waitingRoomNameText;
            waitingSO.FindProperty("readyButton").objectReferenceValue = readyBtnTrans.GetComponent<Button>();
            waitingSO.FindProperty("readyButtonText").objectReferenceValue = readyBtnTrans.GetComponentInChildren<TextMeshProUGUI>();
            waitingSO.FindProperty("startGameButton").objectReferenceValue = startGameBtnObj.GetComponent<Button>();
            waitingSO.FindProperty("leaveButton").objectReferenceValue = leaveBtnTrans.GetComponent<Button>();
            waitingSO.FindProperty("playerListContainer").objectReferenceValue = playerContentRT;
            waitingSO.FindProperty("playerItemPrefab").objectReferenceValue = playerItemTemplate;
            waitingSO.ApplyModifiedProperties();
            */


            // 8. Đăng ký các Screen mới vào danh sách Screens của UIManager (Chỉ đăng ký MainMenu, Gameplay và Lobby)
            SerializedObject uiSO = new SerializedObject(uiManager);
            SerializedProperty listProp = uiSO.FindProperty("listScreen");
            
            // Xóa sạch để build lại danh sách mới sạch sẽ, không null
            listProp.ClearArray();
            
            listProp.InsertArrayElementAtIndex(0);
            listProp.GetArrayElementAtIndex(0).objectReferenceValue = mainMenuScreenTransform.GetComponent<MainMenuScreen>();
            
            listProp.InsertArrayElementAtIndex(1);
            listProp.GetArrayElementAtIndex(1).objectReferenceValue = screenHolder.Find("GameplayScreen").GetComponent<GameplayScreen>();

            listProp.InsertArrayElementAtIndex(2);
            listProp.GetArrayElementAtIndex(2).objectReferenceValue = lobbyScreen;

            uiSO.ApplyModifiedProperties();

            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(screenHolder.gameObject);
            
            Debug.Log("🎉 [SetupMultiplayerUI] Successfully generated and configured MultiplayerLobbyScreen in the UIManager hierarchy! (Phase 3 Complete)");
        }
    }
}
