using Fusion;
using _Game.Scripts.DesignPattern.Observer;
using UnityEngine;

namespace GameCore
{
    public class ScoreController : NetworkBehaviour, IMessageHandle
    {
        [SerializeField] private int pointsPerRecipe = 10;
        [SerializeField] private int penaltyPerWrongRecipe = 5;

        [Networked, OnChangedRender(nameof(OnScoreChangedCallback))]
        public int CurrentScore { get; set; }

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                CurrentScore = 0;
            }

            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRecipeSuccess, this);
            MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRejectRecipe, this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRecipeSuccess, this);
            MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRejectRecipe, this);
        }

        public void Handle(Message message)
        {
            if (!HasStateAuthority) return;

            switch (message.Type)
            {
                case ProjectMessageType.OnRecipeSuccess:
                    CurrentScore += pointsPerRecipe;
                    break;
                case ProjectMessageType.OnRejectRecipe:
                    CurrentScore -= penaltyPerWrongRecipe;
                    if (CurrentScore < 0) CurrentScore = 0;
                    break;
            }
        }

        private void OnScoreChangedCallback()
        {
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnScoreChanged, new object[] { CurrentScore }));
        }
    }
}
