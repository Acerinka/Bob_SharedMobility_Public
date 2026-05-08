using System.Diagnostics;
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace Bob.SharedMobility
{
    public static class ProjectLog
    {
        private const string Prefix = "[Bob.SharedMobility]";

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message, UnityEngine.Object context = null)
        {
            UnityDebug.Log($"{Prefix} {message}", context);
        }

        public static void Warning(string message, UnityEngine.Object context = null)
        {
            UnityDebug.LogWarning($"{Prefix} {message}", context);
        }

        public static void Error(string message, UnityEngine.Object context = null)
        {
            UnityDebug.LogError($"{Prefix} {message}", context);
        }
    }
}
