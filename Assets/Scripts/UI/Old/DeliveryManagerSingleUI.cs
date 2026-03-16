using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryManagerSingleUI : MonoBehaviour
{
    [SerializeField] private Transform iconContainer;
    [SerializeField] private Transform iconTemplate;
    [SerializeField] private Image barImage;

    private float _countDownTimer;
    private float _countDownTimerMax;

    private void Update()
    {
        _countDownTimer -= Time.deltaTime;
        barImage.fillAmount = _countDownTimer / _countDownTimerMax;
        if (_countDownTimer <= 0f)
        {
            _countDownTimer = 0f;
            // TODO: xử lý hết thời gian (ví dụ: hủy đơn hàng)
        }

    }

    public void SetRecipe(RecipeSO recipeSO)
    {
        Debug.Log("Goi");
        foreach (Transform child in iconContainer)
            Destroy(child.gameObject);
        
        foreach (KitchenObjectSO kitchenObjectSO in recipeSO.kitchenObjectSOList)
        {
            Transform iconTransform = Instantiate(iconTemplate, iconContainer);
            iconTransform.GetComponent<Image>().sprite = kitchenObjectSO.sprite;
        }

        _countDownTimer = _countDownTimerMax = recipeSO.countDownTimer;
        
    }
}
