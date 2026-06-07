using UnityEngine;

namespace _Game.Scripts.UI
{
    /// <summary>
    /// Gắn script này vào các Canvas hoặc UI Icon (World Space) để nó luôn nhìn thẳng vào Camera (Billboard).
    /// </summary>
    public class LookAtCamera : MonoBehaviour
    {
        public enum LookAtMode
        {
            LookAt,                  // Nhìn trực tiếp vào Camera (có thể bị ngược chữ UI)
            LookAtInverted,          // Nhìn ngược lại Camera (Dành cho Canvas để chữ không bị ngược)
            CameraForward,           // Xoay trục theo hướng nhìn của Camera
            CameraForwardInverted    // Xoay trục ngược lại hướng nhìn của Camera
        }

        [SerializeField] private LookAtMode mode = LookAtMode.LookAtInverted;
        
        // Caching camera để tối ưu nếu cần thiết, hoặc lấy trực tiếp Camera.main
        private Camera _mainCamera;

        private void Start()
        {
            // Ưu tiên sử dụng UICamera vì đây là giao diện UI
            _mainCamera = GameCore.CameraManager.Instance?.GetUICam;
            if (_mainCamera == null)
            {
                _mainCamera = GameCore.CameraManager.Instance?.GetMainCam;
            }
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            // Nếu chưa tìm thấy Camera thì thử tìm lại
            if (_mainCamera == null)
            {
                _mainCamera = GameCore.CameraManager.Instance?.GetUICam;
                if (_mainCamera == null) _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            switch (mode)
            {
                case LookAtMode.LookAt:
                    transform.LookAt(_mainCamera.transform);
                    break;
                case LookAtMode.LookAtInverted:
                    Vector3 dirFromCamera = transform.position - _mainCamera.transform.position;
                    transform.LookAt(transform.position + dirFromCamera);
                    break;
                case LookAtMode.CameraForward:
                    transform.forward = _mainCamera.transform.forward;
                    break;
                case LookAtMode.CameraForwardInverted:
                    transform.forward = -_mainCamera.transform.forward;
                    break;
            }
        }
    }
}
