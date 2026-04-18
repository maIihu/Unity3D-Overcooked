
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] private Image barImage;

    private void Start()
    {
        barImage.fillAmount = 0;
        Hide();
    }

    public void UpdateProgress(float progressNormalized)
    {
        barImage.fillAmount = progressNormalized;
        if (progressNormalized is 0 or >= 1f)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
