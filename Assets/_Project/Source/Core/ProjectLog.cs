using System.Diagnostics;
using UnityEngine;

namespace Bob.SharedMobility
{
    public static class ProjectLog
    {
        private const string Prefix = "[Bob.SharedMobility]";

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message, Object context = null)
        {
            Debug.Log($"{Prefix} {message}", context);
        }

        public static void Warning(string message, Object context = null)
        {
            Debug.LogWarning($"{Prefix} {message}", context);
        }

        public static void Error(string message, Object context = null)
        {
            Debug.LogError($"{Prefix} {message}", context);
        }
    }
}
