using UnityEngine;

namespace GameSound
{
    [CreateAssetMenu(fileName = "MusicSO", menuName = "Sound/MusicSO")]
    public class MusicSO : ScriptableObject
    {
        [Header("Background Music")]
        public AudioClip menuMusic;
        public AudioClip ingameMusic;
    }
}
