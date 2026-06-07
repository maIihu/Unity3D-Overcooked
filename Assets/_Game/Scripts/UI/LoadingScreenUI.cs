using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using GameCore;

namespace _Game.Scripts.UI
{
    public class LoadingScreenUI : ScreenUI
    {
        [Header("UI References")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Settings")]
        [Tooltip("Speed of the slider filling animation")]
        [SerializeField] private float fillSpeed = 5f;

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

        public void TriggerLoad(string sceneName, System.Action onComplete = null)
        {
            StopAllCoroutines();
            StartCoroutine(LoadSceneAsyncCoroutine(sceneName, onComplete));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneToLoad, System.Action onComplete)
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

        protected override void OnScreenDestroyed()
        {
        }
    }
}
