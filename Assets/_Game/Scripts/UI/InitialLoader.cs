using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using GameCore;

namespace _Game.Scripts.UI
{
    public class InitialLoader : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Settings")]
        [Tooltip("Speed of the slider filling animation")]
        [SerializeField] private float fillSpeed = 5f;

        [SerializeField] private string targetSceneName = "MainMenuScene";

        private void Start()
        {
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }

            if (progressText != null)
            {
                progressText.text = "0%";
            }

            StartCoroutine(LoadSceneAsyncCoroutine());
        }

        private IEnumerator LoadSceneAsyncCoroutine()
        {
            // Small delay to let the screen render properly
            yield return new WaitForSeconds(0.5f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
            
            if (operation == null)
            {
                Debug.LogError($"[InitialLoader] Failed to start async loading for scene: {targetSceneName}");
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

                // If the slider is close to 100%, allow scene activation
                if (progressBar.value >= 0.99f)
                {
                    progressBar.value = 1f;
                    if (progressText != null)
                    {
                        progressText.text = "100%";
                    }
                    InitProject();
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        private void InitProject(){
            UIManager.Instance.InitUI();
            GameManager.Instance.InitGame();
        }
    }
}
