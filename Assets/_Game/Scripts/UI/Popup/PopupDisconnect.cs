using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using _Game.Scripts.Utilities;
using GameCore;

namespace _Game.Scripts.UI
{
    public class PopupDisconnect : PopupUI
    {
        private TextMeshProUGUI _messageText;
        private Button _okButton;

        public void SetMessage(string message)
        {
            EnsureUI();
            if (_messageText != null)
            {
                _messageText.text = message;
            }
        }

        private void EnsureUI()
        {
            if (_messageText != null) return;

            // Create main container overlay
            RectTransform containerRt = GetComponent<RectTransform>();
            if (containerRt == null)
            {
                containerRt = gameObject.AddComponent<RectTransform>();
            }
            containerRt.anchorMin = Vector2.zero;
            containerRt.anchorMax = Vector2.one;
            containerRt.sizeDelta = Vector2.zero;

            // Optional background panel overlay to dim the game
            GameObject dimPanel = new GameObject("DimPanel", typeof(RectTransform), typeof(Image));
            dimPanel.transform.SetParent(transform, false);
            RectTransform dimRt = dimPanel.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.sizeDelta = Vector2.zero;
            Image dimImg = dimPanel.GetComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.6f); // Semi-transparent black

            // Create background box
            GameObject bgGo = new GameObject("BackgroundBox", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(transform, false);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(500, 300);
            
            Image bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f); // Elegant dark color

            // Create title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(bgGo.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.anchoredPosition = new Vector2(0, -40);
            titleRt.sizeDelta = new Vector2(0, 50);
            
            TextMeshProUGUI titleTxt = titleGo.GetComponent<TextMeshProUGUI>();
            titleTxt.text = "THÔNG BÁO";
            titleTxt.fontSize = 28;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = new Color(1f, 0.3f, 0.3f); // Coral red
            titleTxt.fontStyle = FontStyles.Bold;

            // Create message body
            GameObject msgGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            msgGo.transform.SetParent(bgGo.transform, false);
            RectTransform msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0, 0.5f);
            msgRt.anchorMax = new Vector2(1, 0.5f);
            msgRt.anchoredPosition = new Vector2(0, 10);
            msgRt.sizeDelta = new Vector2(-40, 100);
            
            _messageText = msgGo.GetComponent<TextMeshProUGUI>();
            _messageText.text = "Bạn đã bị ngắt kết nối.";
            _messageText.fontSize = 18;
            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.color = Color.white;

            // Create button
            GameObject btnGo = new GameObject("OKButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(bgGo.transform, false);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0);
            btnRt.anchorMax = new Vector2(0.5f, 0);
            btnRt.anchoredPosition = new Vector2(0, 50);
            btnRt.sizeDelta = new Vector2(180, 45);
            
            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Red button

            _okButton = btnGo.GetComponent<Button>();
            
            // Button Text
            GameObject btnTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTxtGo.transform.SetParent(btnGo.transform, false);
            RectTransform btnTxtRt = btnTxtGo.GetComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI btnTxt = btnTxtGo.GetComponent<TextMeshProUGUI>();
            btnTxt.text = "QUAY VỀ MENU";
            btnTxt.fontSize = 16;
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.color = Color.white;
            btnTxt.fontStyle = FontStyles.Bold;

            mainPopUp = bgRt;
        }

        private void OnEnable()
        {
            EnsureUI();
            if (_okButton != null)
            {
                _okButton.onClick.AddListener(OnOkClicked);
            }
        }

        private void OnDisable()
        {
            if (_okButton != null)
            {
                _okButton.onClick.RemoveListener(OnOkClicked);
            }
        }

        private void OnOkClicked()
        {
            Hide();
            Loader.Load(Loader.Scene.MainMenuScene, () => UIManager.Instance.ShowScreen<MainMenuScreen>());
        }
    }
}
