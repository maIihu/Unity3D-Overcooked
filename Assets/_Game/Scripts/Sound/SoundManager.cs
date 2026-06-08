using System.Collections.Generic;
using UnityEngine;
using DesignPattern;
using _Game.Scripts.DesignPattern.Observer;

namespace GameSound
{
    public enum SoundLoopType
    {
        StoveSizzle,
        StoveWarning
    }

    public class SoundManager : Singleton<SoundManager>, IMessageHandle
    {
        [Header("Audio References")]
        [SerializeField] private AudioClipRefesSO audioClipRefesSO;
        [SerializeField] private float defaultVolume = 1f;

        private Dictionary<string, AudioSource> _activeLoops = new Dictionary<string, AudioSource>();

        protected void Awake()
        {
            Initialize(this);
        }

        private void OnEnable()
        {
            if (MessageManager.Instance != null)
            {
                MessageManager.Instance.AddSubscriber(ProjectMessageType.OnChop, this);
                MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRecipeSuccess, this);
                MessageManager.Instance.AddSubscriber(ProjectMessageType.OnRejectRecipe, this);
                MessageManager.Instance.AddSubscriber(ProjectMessageType.OnPickupObject, this);
                MessageManager.Instance.AddSubscriber(ProjectMessageType.OnDropObject, this);
                MessageManager.Instance.AddSubscriber(ProjectMessageType.OnFootstep, this);
                MessageManager.Instance.AddSubscriber(ProjectMessageType.OnStoveSizzle, this);
                MessageManager.Instance.AddSubscriber(ProjectMessageType.OnStoveWarning, this);
            }
        }

        private void OnDisable()
        {
            if (MessageManager.Instance != null)
            {
                MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnChop, this);
                MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRecipeSuccess, this);
                MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnRejectRecipe, this);
                MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnPickupObject, this);
                MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnDropObject, this);
                MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnFootstep, this);
                MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnStoveSizzle, this);
                MessageManager.Instance.RemoveSubscriber(ProjectMessageType.OnStoveWarning, this);
            }
        }

        public void Handle(Message message)
        {
            if (audioClipRefesSO == null)
            {
                Debug.LogWarning("[SoundManager] AudioClipRefesSO is null!");
                return;
            }

            Vector3 spawnPosition = Vector3.zero;
            if (message.Data != null && message.Data.Length > 0 && message.Data[0] is Vector3 pos)
            {
                spawnPosition = pos;
            }

            if (spawnPosition == Vector3.zero && Camera.main != null)
            {
                spawnPosition = Camera.main.transform.position;
            }

            switch (message.Type)
            {
                case ProjectMessageType.OnChop:
                    PlayRandomClipAtPoint(audioClipRefesSO.chop, spawnPosition);
                    break;

                case ProjectMessageType.OnRecipeSuccess:
                    PlayRandomClipAtPoint(audioClipRefesSO.deliverySuccess, spawnPosition);
                    break;

                case ProjectMessageType.OnRejectRecipe:
                    PlayRandomClipAtPoint(audioClipRefesSO.deliveryFail, spawnPosition);
                    break;

                case ProjectMessageType.OnPickupObject:
                    PlayRandomClipAtPoint(audioClipRefesSO.objectPickup, spawnPosition);
                    break;

                case ProjectMessageType.OnDropObject:
                    PlayRandomClipAtPoint(audioClipRefesSO.objectDrop, spawnPosition);
                    break;

                case ProjectMessageType.OnFootstep:
                    PlayRandomClipAtPoint(audioClipRefesSO.footstep, spawnPosition, 0.4f);
                    break;

                case ProjectMessageType.OnStoveSizzle:
                    if (message.Data != null && message.Data.Length > 1 && message.Data[1] is bool isSizzleActive && message.Data[0] is string sizzleKey)
                    {
                        HandleLoopSound(sizzleKey, audioClipRefesSO.stoveSizzle, spawnPosition, isSizzleActive);
                    }
                    break;

                case ProjectMessageType.OnStoveWarning:
                    if (message.Data != null && message.Data.Length > 1 && message.Data[1] is bool isWarningActive && message.Data[0] is string warningKey)
                    {
                        AudioClip warningClip = GetFirstClip(audioClipRefesSO.warning);
                        HandleLoopSound(warningKey, warningClip, spawnPosition, isWarningActive);
                    }
                    break;
            }
        }

        private void PlayRandomClipAtPoint(AudioClip[] clips, Vector3 position, float volumeMultiplier = 1f)
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning("[SoundManager] Attempted to play from an empty or null AudioClip array.");
                return;
            }

            int index = Random.Range(0, clips.Length);
            AudioClip clip = clips[index];
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, defaultVolume * volumeMultiplier);
            }
            else
            {
                Debug.LogWarning($"[SoundManager] AudioClip at index {index} is null!");
            }
        }

        private AudioClip GetFirstClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[0];
        }

        private void HandleLoopSound(string key, AudioClip clip, Vector3 position, bool start)
        {
            if (start)
            {
                if (_activeLoops.ContainsKey(key)) return;

                if (clip == null)
                {
                    Debug.LogWarning($"[SoundManager] Loop AudioClip for key {key} is null!");
                    return;
                }

                GameObject go = new GameObject($"LoopSound_{key}");
                go.transform.position = position;
                AudioSource source = go.AddComponent<AudioSource>();
                source.clip = clip;
                source.loop = true;
                source.spatialBlend = 1f; // 3D sound
                source.volume = defaultVolume;
                source.Play();

                _activeLoops[key] = source;
            }
            else
            {
                if (_activeLoops.TryGetValue(key, out AudioSource source))
                {
                    if (source != null)
                    {
                        source.Stop();
                        Destroy(source.gameObject);
                    }
                    _activeLoops.Remove(key);
                }
            }
        }
    }
}
