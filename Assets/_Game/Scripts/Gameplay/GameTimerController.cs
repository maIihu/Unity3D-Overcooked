using Fusion;
using _Game.Scripts.DesignPattern.Observer;
using UnityEngine;

namespace GameCore
{
    public class GameTimerController : NetworkBehaviour
    {
        [SerializeField] private float gameDuration = 180f; // 3 minutes default

        [Networked] public float CurrentTime { get; set; }
        [Networked] public NetworkBool IsTimerRunning { get; set; }

        public override void Spawned()
        {
            Debug.Log($"[GameTimerController] Spawned! HasStateAuthority: {HasStateAuthority}");
            if (HasStateAuthority)
            {
                CurrentTime = gameDuration;
                IsTimerRunning = true;
                Debug.Log($"[GameTimerController] Timer started with duration: {CurrentTime}");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (!IsTimerRunning) return;

            CurrentTime -= Runner.DeltaTime;
            
            if (CurrentTime <= 0f)
            {
                CurrentTime = 0f;
                IsTimerRunning = false;
                
                Debug.Log("[GameTimerController] Timer finished! Broadcasting OnGameOver");
                // Trigger Game Over
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnGameOver));
            }
        }

        public override void Render()
        {
            // Debug.Log($"[GameTimerController] Render - CurrentTime: {CurrentTime}, IsTimerRunning: {IsTimerRunning}");
            // Update UI smoothly every frame
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnTimerTick, new object[] { CurrentTime }));
            
            // If timer stopped and reached 0, also broadcast GameOver to client side
            // Note: FixedUpdateNetwork might trigger OnGameOver on host, but we can also trigger it based on property for clients
            if (!IsTimerRunning && CurrentTime <= 0.01f)
            {
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnGameOver));
            }
        }
    }
}
