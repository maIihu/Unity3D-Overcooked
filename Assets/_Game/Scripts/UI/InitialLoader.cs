using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using GameCore;
using DG.Tweening;

namespace _Game.Scripts.UI
{
    public class InitialLoader : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "MainMenuScene";
        [SerializeField] private TextMeshProUGUI text;

        [Header("Text Animation Settings")]
        [SerializeField] private float waveDuration = 1.5f;
        [SerializeField] private float waveHeight = 10f;
        [SerializeField] private float waveOffset = 0.5f;

        private void Start()
        {
            if (text != null)
            {
                text.text = "LOADING...";
            }

            StartAnimation();
            StartCoroutine(LoadSceneAsyncCoroutine());
        }

        private void OnEnable()
        {
            StartAnimation();
        }

        private void StartAnimation()
        {
            if (text == null) return;

            // Kill tween cũ trên object này để tránh chạy đè
            DOTween.Kill(this);

            // Dùng DOVirtual.Float của DOTween để chạy một giá trị từ 0 đến 1
            // LoopType.Yoyo giúp giá trị chạy tiến rồi chạy lùi (hiệu ứng đi ngược lại từ L)
            DOVirtual.Float(0f, 1f, waveDuration, (t) =>
            {
                UpdateTextMesh(t);
            })
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo)
            .SetTarget(this);
        }

        private void UpdateTextMesh(float t)
        {
            if (text == null) return;

            // Cập nhật lại mesh gốc trước khi cộng dồn offset
            text.ForceMeshUpdate();
            var textInfo = text.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                var verts = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
                
                // Hiệu ứng sóng: t chạy từ 0->1, nhân với 2*PI tạo thành 1 chu kỳ hình sin
                // i * waveOffset tạo ra độ lệch pha (chữ trước chữ sau)
                float offset = Mathf.Sin(t * Mathf.PI * 2f - i * waveOffset) * waveHeight;

                for (int j = 0; j < 4; j++)
                {
                    verts[charInfo.vertexIndex + j].y += offset;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                meshInfo.mesh.vertices = meshInfo.vertices;
                text.UpdateGeometry(meshInfo.mesh, i);
            }
        }

        private void OnDisable()
        {
            DOTween.Kill(this);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        private IEnumerator LoadSceneAsyncCoroutine()
        {
            // Small delay to let the screen render properly
            yield return new WaitForSeconds(1f);

            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
            
            if (operation == null)
            {
                Debug.LogError($"[InitialLoader] Failed to start async loading for scene: {targetSceneName}");
                yield break;
            }

            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                // AsyncOperation.progress ranges from 0 to 0.9. 0.9 means loading is complete.
                if (operation.progress >= 0.9f)
                {
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
