using Fusion;
using _Game.Scripts.DesignPattern.Observer;
using UnityEngine;

namespace GameCore
{

    public class GameTimerController : NetworkBehaviour
    {
        [SerializeField] private float gameDuration = 180f; 

        [Networked] public float CurrentTime { get; set; }
        [Networked] public NetworkBool IsTimerRunning { get; set; }
        private bool _hasFiredGameOver = false;

        private bool _isOffline;
        private float _offlineCurrentTime;
        private bool _offlineIsRunning;

        private int _lastRenderedSeconds = -1;

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

        public void StartOfflineTimer()
        {
            _isOffline = true;
            _offlineCurrentTime = gameDuration;
            _offlineIsRunning = true;
            _hasFiredGameOver = false;
            _lastRenderedSeconds = -1;
            Debug.Log($"[GameTimerController] Offline timer started with duration: {gameDuration}");
        }

        // ── Online path ──────────────────────────────────────

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            if (!IsTimerRunning) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != EGameState.Play) return;

            CurrentTime -= Runner.DeltaTime;
            
            if (CurrentTime <= 0f)
            {
                CurrentTime = 0f;
                IsTimerRunning = false;
                
                Debug.Log("[GameTimerController] Timer finished!");
            }
        }

        public override void Render()
        {
            int currentSeconds = Mathf.FloorToInt(CurrentTime);
            if (currentSeconds != _lastRenderedSeconds)
            {
                _lastRenderedSeconds = currentSeconds;
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnTimerTick, new object[] { CurrentTime }));
            }
            
            if (!IsTimerRunning && CurrentTime <= 0.01f && !_hasFiredGameOver)
            {
                _hasFiredGameOver = true;
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnGameOver));
            }
        }

        // ── Offline path ─────────────────────────────────────

        private void Update()
        {
            if (!_isOffline) return;
            if (!_offlineIsRunning) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != EGameState.Play) return;

            _offlineCurrentTime -= Time.deltaTime;

            if (_offlineCurrentTime <= 0f)
            {
                _offlineCurrentTime = 0f;
                _offlineIsRunning = false;
                Debug.Log("[GameTimerController] Offline timer finished!");
            }

            int currentSeconds = Mathf.FloorToInt(_offlineCurrentTime);
            if (currentSeconds != _lastRenderedSeconds)
            {
                _lastRenderedSeconds = currentSeconds;
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnTimerTick, new object[] { _offlineCurrentTime }));
            }

            if (!_offlineIsRunning && _offlineCurrentTime <= 0.01f && !_hasFiredGameOver)
            {
                _hasFiredGameOver = true;
                MessageManager.Instance.SendMessage(new Message(ProjectMessageType.OnGameOver));
            }
        }
    }
}
