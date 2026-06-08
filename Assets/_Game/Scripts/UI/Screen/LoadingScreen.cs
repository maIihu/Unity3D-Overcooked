using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using GameCore;

namespace _Game.Scripts.UI
{
    public class LoadingScreen : ScreenUI
    {
        [Header("UI References")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Settings")]
        [Tooltip("Speed of the slider filling animation")]
        [SerializeField] private float fillSpeed = 5f;

        private Coroutine _fakeProgressCoroutine;

        public override void Active()
        {
            base.Active();
            
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }

            if (progressText != null)
            {
                progressText.text = "0%";
            }
        }

        // ── Standard Scene Loading (existing) ─────────────────────

        public void TriggerLoad(string sceneName, Action onComplete = null)
        {
            StopAllCoroutines();
            _fakeProgressCoroutine = null;
            StartCoroutine(LoadSceneAsyncCoroutine(sceneName, onComplete));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneToLoad, Action onComplete)
        {
            // Small delay to let the screen render properly
            yield return new WaitForSeconds(0.2f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
            
            if (operation == null)
            {
                Debug.LogError($"[LoadingScreenUI] Failed to start async loading for scene: {sceneToLoad}");
                yield break;
            }

            operation.allowSceneActivation = false;

            float targetProgress = 0f;

            while (!operation.isDone)
            {
                // AsyncOperation.progress ranges from 0 to 0.9. 0.9 means loading is complete.
                targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

                if (progressBar != null)
                {
                    // Smoothly interpolate the slider value
                    progressBar.value = Mathf.MoveTowards(progressBar.value, targetProgress, fillSpeed * Time.deltaTime);

                    if (progressText != null)
                    {
                        progressText.text = $"{Mathf.RoundToInt(progressBar.value * 100f)}%";
                    }
                }
                else
                {
                    progressBar.value = targetProgress;
                }

                // If the slider is fully loaded to 100%, allow scene activation
                if (Mathf.Approximately(progressBar.value, 1f))
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            // Once loaded, trigger the OnComplete callback
            if (onComplete != null)
            {
                onComplete.Invoke();
            }
        }

        // ── Fake Progress (for Fusion-managed scene loads) ────────

        /// <summary>
        /// Bắt đầu fake progress animation — chạy từ 0% lên ~90% theo 3 phase (nhanh → vừa → chậm).
        /// Dùng khi Fusion tự load scene và ta không có AsyncOperation.progress.
        /// Gọi CompleteProgress() khi level thực sự ready để chạy lên 100%.
        /// </summary>
        public void StartFakeProgress()
        {
            if (_fakeProgressCoroutine != null) StopCoroutine(_fakeProgressCoroutine);
            _fakeProgressCoroutine = StartCoroutine(FakeProgressCoroutine());
        }

        private IEnumerator FakeProgressCoroutine()
        {
            float current = 0f;
            UpdateProgressUI(current);

            // Phase 1: Nhanh lên 30%
            while (current < 0.29f)
            {
                current = Mathf.MoveTowards(current, 0.3f, fillSpeed * Time.deltaTime);
                UpdateProgressUI(current);
                yield return null;
            }

            // Phase 2: Vừa lên 60%
            while (current < 0.59f)
            {
                current = Mathf.MoveTowards(current, 0.6f, fillSpeed * 0.4f * Time.deltaTime);
                UpdateProgressUI(current);
                yield return null;
            }

            // Phase 3: Chậm lên 90% — dừng ở đây chờ CompleteProgress()
            while (current < 0.89f)
            {
                current = Mathf.MoveTowards(current, 0.9f, fillSpeed * 0.15f * Time.deltaTime);
                UpdateProgressUI(current);
                yield return null;
            }

            UpdateProgressUI(0.9f);
            // Coroutine kết thúc tự nhiên tại 90%, chờ CompleteProgress() gọi tiếp
        }

        /// <summary>
        /// Hoàn thành progress từ vị trí hiện tại lên 100%, sau đó gọi callback.
        /// </summary>
        public void CompleteProgress(Action onComplete = null)
        {
            if (_fakeProgressCoroutine != null) StopCoroutine(_fakeProgressCoroutine);
            _fakeProgressCoroutine = StartCoroutine(CompleteProgressCoroutine(onComplete));
        }

        private IEnumerator CompleteProgressCoroutine(Action onComplete)
        {
            float current = progressBar != null ? progressBar.value : 0f;

            // Animate lên 100%
            while (current < 0.99f)
            {
                current = Mathf.MoveTowards(current, 1f, fillSpeed * Time.deltaTime);
                UpdateProgressUI(current);
                yield return null;
            }
            UpdateProgressUI(1f);

            // Delay nhỏ để người chơi thấy 100% trước khi chuyển
            yield return new WaitForSeconds(0.3f);

            _fakeProgressCoroutine = null;
            onComplete?.Invoke();
        }

        private void UpdateProgressUI(float value)
        {
            if (progressBar != null) progressBar.value = value;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        protected override void OnScreenDestroyed()
        {
        }
    }
}
