using UnityEngine;

namespace GameCore
{
    public class LevelController : MonoBehaviour
    {
        public static LevelController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }
    }
}
