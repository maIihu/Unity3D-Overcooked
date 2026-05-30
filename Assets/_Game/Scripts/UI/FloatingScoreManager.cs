using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using _Game.Scripts.DesignPattern.Observer;

namespace GameCore.UI
{
    public class FloatingScoreManager : MonoBehaviour
    {
        [SerializeField] private GameObject scoreGO;
        [SerializeField] private TextMeshProUGUI scoreText;

        public void SpawnFloatingScore(int score, Vector3 position)
        {
            if (scoreGO == null || scoreText == null) return;

            Vector3 spawnWorldPos = position + Vector3.up * 1.5f;

            // Reset UI state
            scoreGO.transform.position = spawnWorldPos;
            scoreGO.transform.forward = Camera.main.transform.forward;
            scoreText.text = $"+{score}";
            
            Color c = scoreText.color;
            c.a = 1f;
            scoreText.color = c;
            
            scoreGO.SetActive(true);

            // Animate with DOTween
            scoreGO.transform.DOMoveY(spawnWorldPos.y + 1.5f, 1.5f).SetEase(Ease.OutCubic);
            scoreText.DOFade(0f, 1.5f).SetEase(Ease.InExpo).OnComplete(() =>
            {
                scoreGO.SetActive(false);
            });
        }
    }
}
