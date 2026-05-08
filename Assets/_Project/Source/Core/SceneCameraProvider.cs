using UnityEngine;

namespace Bob.SharedMobility
{
    public static class SceneCameraProvider
    {
        public static bool TryGetMainCamera(out Camera camera, Object context = null)
        {
            camera = Camera.main;
            if (camera != null) return true;

            ProjectLog.Warning("MainCamera is missing or is not tagged correctly.", context);
            return false;
        }
    }
}
