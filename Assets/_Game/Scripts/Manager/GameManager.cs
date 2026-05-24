using System;
using _Game.Scripts.DesignPattern.Observer;
using DesignPattern;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Central manager — the ONLY Singleton in the gameplay layer.
    /// All sub-controllers are referenced here and accessed via
    /// GameManager.Instance.DeliveryController / GameManager.Instance.LevelController.
    /// </summary>
    public class GameManager : Singleton<GameManager>, IMessageHandle
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

        private void OnEnable()
        {
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnLoadLevel, this);
        }

        private void OnDisable()
        {
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnLoadLevel, this);
        }

        public void Handle(Message message)
        {
            switch (message.Type)
            {
                case ProjectMessageType.OnLoadLevel:
                    LoadLevel();
                    break;
            }
        }

        private void LoadLevel()
        {
            levelController.LoadLevel(levelData);
            deliveryController.StartSpawning();
        }

        protected override void OnRegistration()
        {
            base.OnRegistration();
        }
    }
}

