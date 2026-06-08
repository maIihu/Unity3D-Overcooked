using UnityEngine;

namespace Counter
{
    [CreateAssetMenu(menuName = "Game/Counter/Stove Counter Config", fileName = "StoveCounterConfigSO")]
    public class StoveCounterConfigSO : ScriptableObject
    {
        [Tooltip("Thời gian cần thiết để nấu chín thức ăn (giây)")]
        public float fryingTimerMax = 4f;

        [Tooltip("Thời gian cần thiết để thức ăn bị cháy kể từ khi chín (giây)")]
        public float burningTimerMax = 5f;
    }
}
