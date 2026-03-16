using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreBoardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    private int _score;
    private void Start()
    {
        DeliveryManager.Instance.OnRecipeSuccess += DeliveryManager_OnRecipeSuccess;
        DeliveryManager.Instance.OnRecipeFailed += DeliveryManager_OnRecipeFailed;
        
    }

    private void DeliveryManager_OnRecipeFailed(object sender, EventArgs e)
    {
        _score--;
        scoreText.text = _score.ToString();
    }

    private void DeliveryManager_OnRecipeSuccess(object sender, EventArgs e)
    {
        _score++;
        scoreText.text = _score.ToString();
    }
    
    
}
