using System;
using DesignPattern;
using UnityEngine;

namespace GameCore
{
    public class CameraManager : Singleton<CameraManager>
    {
        [SerializeField] private Camera mainCamera;

        private void Awake()
        {
            Initialize(this);
        }

        public Camera GetMainCam => mainCamera;
    }
}