using UnityEngine;

namespace CT4AR.Utils
{
    /// <summary>
    /// Global logger with log-level filtering for CT4AR.
    /// </summary>
    public static class CT4ARLogger
    {
        public static bool Verbose = false;

        public static void Log(string message)
        {
            Debug.Log($"[CT4AR] {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[CT4AR] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[CT4AR] {message}");
        }

        public static void LogVerbose(string message)
        {
            if (Verbose)
                Debug.Log($"[CT4AR][V] {message}");
        }
    }
}
