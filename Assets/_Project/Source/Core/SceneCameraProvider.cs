using UnityEngine;

namespace Bob.SharedMobility
{
    public static class SceneCameraProvider
    {
        private const string MainCameraName = "Main Camera";
        private const string UICameraName = "UI Camera";

        private static bool _warnedMultipleMainCameras;

        public static bool TryGetMainCamera(out Camera camera, Object context = null)
        {
            if (TryFindNamedActiveCamera(MainCameraName, out camera))
            {
                WarnIfMultipleTaggedMainCameras(context);
                return true;
            }

            camera = Camera.main;
            if (camera != null)
            {
                WarnIfMultipleTaggedMainCameras(context);
                return true;
            }

            ProjectLog.Warning("MainCamera is missing or is not tagged correctly.", context);
            return false;
        }

        public static bool TryGetUICamera(out Camera camera, Object context = null)
        {
            if (TryFindNamedActiveCamera(UICameraName, out camera))
            {
                return true;
            }

            return TryGetMainCamera(out camera, context);
        }

        private static bool TryFindNamedActiveCamera(string cameraName, out Camera camera)
        {
            Camera[] cameras = Camera.allCameras;
            foreach (Camera candidate in cameras)
            {
                if (candidate != null
                    && candidate.isActiveAndEnabled
                    && candidate.gameObject.name == cameraName)
                {
                    camera = candidate;
                    return true;
                }
            }

            camera = null;
            return false;
        }

        private static void WarnIfMultipleTaggedMainCameras(Object context)
        {
            if (_warnedMultipleMainCameras) return;

            int taggedCount = 0;
            Camera[] cameras = Camera.allCameras;
            foreach (Camera candidate in cameras)
            {
                if (candidate != null
                    && candidate.isActiveAndEnabled
                    && candidate.CompareTag("MainCamera"))
                {
                    taggedCount++;
                }
            }

            if (taggedCount > 1)
            {
                _warnedMultipleMainCameras = true;
                ProjectLog.Warning("Multiple active cameras are tagged MainCamera. Prefer one tagged main camera and explicit UI cameras.", context);
            }
        }
    }
}
