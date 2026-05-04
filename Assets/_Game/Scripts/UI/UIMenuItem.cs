using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.UI
{
    public class UIMenuItem : MonoBehaviour
    {
        [SerializeField] private Image iconImg;
        [SerializeField] private List<Image> ingredientsImg;
        [SerializeField] private Image timerImg;

        private Tween timerTween;

        private readonly Color startColor = Color.green;
        private readonly Color endColor = Color.red;

        public void SetImage(Sprite icon, Sprite ingredient)
        {
            iconImg.sprite = icon;

            foreach (var img in ingredientsImg)
            {
                img.sprite = ingredient;
            }
        }

        public void Initialize(float timeRemaining)
        {
            StartTimer(timeRemaining);
        }

        public void Active()
        {
            timerImg.color = startColor;
        }

        private void StartTimer(float duration)
        {
            timerTween?.Kill();

            timerImg.fillAmount = 1f;
            timerImg.color = startColor;

            timerTween = timerImg
                .DOFillAmount(0f, duration)
                .SetEase(Ease.Linear);

            DOTween.To(
                    () => timerImg.color,
                    x => timerImg.color = x,
                    endColor,
                    duration
                )
                .SetEase(Ease.Linear);
        }

        public void PlaySuccessAnimation(Action onComplete)
        {
            timerTween?.Kill();

            // Punch scale để tạo cảm giác "pop"
            transform
                .DOPunchScale(Vector3.one * 0.4f, 0.3f, 6, 0.5f)
                .OnComplete(() =>
                {
                    // Fade out toàn bộ CanvasGroup hoặc scale về 0
                    transform
                        .DOScale(0f, 0.25f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            transform.localScale = Vector3.one; // reset cho lần dùng sau
                            onComplete?.Invoke();
                        });
                });
        }

        private void OnDisable()
        {
            timerTween?.Kill();
        }

        private void OnDestroy()
        {
            timerTween?.Kill();
        }
    }
}