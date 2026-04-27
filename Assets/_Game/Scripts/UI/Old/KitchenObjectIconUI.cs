
using UnityEngine;

public class KitchenObjectIconUI : MonoBehaviour, IIconUI
{
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
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
