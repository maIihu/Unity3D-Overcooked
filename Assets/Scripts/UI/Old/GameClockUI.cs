using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameClockUI : MonoBehaviour
{
    [SerializeField] private Image timerImage;

    private void Update()
    {
        if (LevelController.Instance != null)
        {
            timerImage.fillAmount = LevelController.Instance.GetPlayingTimerNormalized();
        }
    }
}
