using System.Threading.Tasks;
using UnityEngine;

namespace _Game.Scripts.Utilities
{
    /// <summary>
    /// Extension methods cho async Task để dùng fire-and-forget an toàn.
    /// Dùng khi cần gọi async method từ void method (VD: Unity button callback)
    /// mà không cần await (nhưng vẫn muốn exception được log).
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Fire-and-forget: chạy Task mà không block, nhưng vẫn log exception nếu có.
        /// </summary>
        public static async void FireAndForget(this Task task)
        {
            try
            {
                await task;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
