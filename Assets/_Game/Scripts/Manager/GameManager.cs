using System;
using DesignPattern;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Central manager — the ONLY Singleton in the gameplay layer.
    /// All sub-controllers are referenced here and accessed via
    /// GameManager.Instance.DeliveryController / GameManager.Instance.LevelController.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        [Header("Controllers")]
        [SerializeField] private DeliveryController deliveryController;
        [SerializeField] private LevelController levelController;

        public string levelData; // temp

        // ── Public Accessors ───────────────────────────────────
        public DeliveryController DeliveryController => deliveryController;
        public LevelController LevelController => levelController;

        private void Awake()
        {
            Initialize(this);
        }

        private void Start()
        {
            levelController.LoadLevel(levelData);
        }

        protected override void OnRegistration()
        {
            base.OnRegistration();
        }
    }
}
