using Fusion;
using _Game.Scripts.DesignPattern.Observer;
using UnityEngine;

namespace GameCore
{
    public class ScoreController : NetworkBehaviour, IMessageHandle
    {
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

            // Trigger visual update for clients joining mid-game
            OnScoreChangedCallback();
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
                    int scoreAdded = (int)message.Data[1];
                    CurrentScore += scoreAdded;
                    break;
                case ProjectMessageType.OnRejectRecipe:
                    // No penalty for wrong/late delivery as requested
                    break;
            }
        }

        private void OnScoreChangedCallback()
        {
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnScoreChanged, new object[] { CurrentScore }));
        }
    }
}
