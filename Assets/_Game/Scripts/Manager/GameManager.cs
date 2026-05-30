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
        
        private Counter.DeliveryCounter _deliveryCounter;

        private void Awake()
        {
            Initialize(this);
        }

        private void OnEnable()
        {
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnLoadLevel, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRecipeSuccess, this);
        }

        private void OnDisable()
        {
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnLoadLevel, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRecipeSuccess, this);
        }

        public void Handle(Message message)
        {
            switch (message.Type)
            {
                case ProjectMessageType.OnLoadLevel:
                    LoadLevel();
                    break;
                case ProjectMessageType.OnRecipeSuccess:
                    if (message.Data.Length > 1 && _deliveryCounter != null && UIManager.Instance != null && UIManager.Instance.floatingScoreManager != null)
                    {
                        int scoreAdded = (int)message.Data[1];
                        UIManager.Instance.floatingScoreManager.SpawnFloatingScore(scoreAdded, _deliveryCounter.transform.position);
                    }
                    break;
            }
        }

        private void LoadLevel()
        {
            levelController.LoadLevel(levelData);
            _deliveryCounter = levelController.GetDeliveryCounter();
            deliveryController.StartSpawning();
        }

        protected override void OnRegistration()
        {
            base.OnRegistration();
        }
    }
}

