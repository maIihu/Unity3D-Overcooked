using System;
using DesignPattern;
using UnityEngine;
using _Game.Scripts.DesignPattern.Observer;

namespace GameCore
{
    public class CameraManager : Singleton<CameraManager>, IMessageHandle
    {
        [SerializeField] private Camera mainCamera;
        
        [Header("UI Camera")]
        public Camera UICamera;

        private void Awake()
        {
            Initialize(this);
            LinkCameraStack();
        }

        private void OnEnable()
        {
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnSetupCamera, this);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnSetupCamera, this);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            LinkCameraStack();
        }

        public void LinkCameraStack()
        {
            if (UICamera != null && mainCamera != null)
            {
                var cameraData = mainCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (cameraData != null && !cameraData.cameraStack.Contains(UICamera))
                {
                    cameraData.cameraStack.Add(UICamera);
                    Debug.Log("[CameraManager] Nối UICamera vào Base Camera thành công!");
                }
            }
        }

        public void Handle(Message message)
        {
            if (message.Type == ProjectMessageType.OnSetupCamera)
            {
                if (message.Data != null && message.Data.Length >= 2)
                {
                    Vector3 pos = (Vector3)message.Data[0];
                    Vector3 euler = (Vector3)message.Data[1];
                    if (mainCamera != null)
                    {
                        mainCamera.transform.position = pos;
                        mainCamera.transform.eulerAngles = euler;
                        
                        if (UICamera != null)
                        {
                            UICamera.transform.position = pos;
                            UICamera.transform.eulerAngles = euler;
                        }
                        
                        Debug.Log("[CameraManager] Đã setup Transform Camera thông qua Message!");
                    }
                }
            }
        }

        public Camera GetMainCam => mainCamera;
        public Camera GetUICam => UICamera;
    }
}