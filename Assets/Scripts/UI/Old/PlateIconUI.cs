using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlateIconUI : MonoBehaviour, IIconUI
{
    [SerializeField] private Transform iconTemplate;
    [SerializeField] private PlateKitchenObject plateKitchenObject;
    
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    public void UpdateVisualIcon()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        foreach (var kitchenObjectSO in plateKitchenObject.GetKitchenObjectSOs())
        {
            Transform icon = Instantiate(iconTemplate, transform);
            icon.GetComponentInChildren<PlateIconSingleUI>().SetKitchenImage(kitchenObjectSO);
        }
    }

    private void LateUpdate()
    {
        LookAtCamera();
    }

    public void LookAtCamera()
    {
        transform.forward = _mainCamera.transform.forward;
    }
}
