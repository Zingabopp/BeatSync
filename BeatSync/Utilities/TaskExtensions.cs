using System.Threading.Tasks;

namespace BeatSync.Utilities
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Returns true if the task completed successfully. Using this because Task.IsCompletedSuccessfully isn't available in .net 4.5?
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static bool DidCompleteSuccessfully(this Task task)
        {
            return task.IsCompleted && !(task.IsFaulted || task.IsCanceled);
        }
    }
}
