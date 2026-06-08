using UnityEngine;

namespace Counter
{
    [CreateAssetMenu(menuName = "Game/Counter/Cutting Counter Config", fileName = "CuttingCounterConfigSO")]
    public class CuttingCounterConfigSO : ScriptableObject
    {
        [Tooltip("Thời gian cần thiết để thái xong nguyên liệu (giây)")]
        public float cuttingTime = 3f;
    }
}
