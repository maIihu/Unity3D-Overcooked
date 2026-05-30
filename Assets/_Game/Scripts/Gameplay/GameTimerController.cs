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
        private bool _hasFiredGameOver = false;

        public override void Spawned()
        {
            Debug.Log($"[GameTimerController] Spawned! HasStateAuthority: {HasStateAuthority}");
            _hasFiredGameOver = false;
            
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
                
                Debug.Log("[GameTimerController] Timer finished!");
                // Let Render() handle the UI broadcast so it runs on all clients predictably
            }
        }

        public override void Render()
        {
            // Update UI smoothly every frame
            MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnTimerTick, new object[] { CurrentTime }));
            
            // If timer stopped and reached 0, broadcast GameOver (only once per client/host)
            if (!IsTimerRunning && CurrentTime <= 0.01f && !_hasFiredGameOver)
            {
                _hasFiredGameOver = true;
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnGameOver));
            }
        }
    }
}
