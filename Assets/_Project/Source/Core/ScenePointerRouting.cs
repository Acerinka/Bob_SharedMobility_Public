using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Bob.SharedMobility
{
    public static class ScenePointerRouting
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAfterSceneLoad()
        {
            Ensure();
        }

        public static void Ensure()
        {
            EnsureEventSystem();
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = Object.FindObjectOfType<EventSystem>(true);
            }

            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            EventSystem.current = eventSystem;

            if (eventSystem.GetComponent<BaseInputModule>() == null)
            {
#if ENABLE_INPUT_SYSTEM
                InputSystemUIInputModule inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
#elif ENABLE_LEGACY_INPUT_MANAGER
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#else
                ProjectLog.Warning("EventSystem has no input module. Enable Unity Input System or legacy input handling.", eventSystem);
#endif
            }
        }

        [System.Obsolete("Use SceneWorldPointerRouter.Ensure() for world collider input so UI remains the first-class pointer route.")]
        public static void EnsurePhysicsRaycasters()
        {
            Camera[] cameras = Camera.allCameras;
            bool hasCandidate = false;

            foreach (Camera candidate in cameras)
            {
                if (!IsPointerCamera(candidate)) continue;

                hasCandidate = true;
                EnsurePhysicsRaycaster(candidate);
            }

            if (hasCandidate)
            {
                return;
            }

            if (SceneCameraProvider.TryGetMainCamera(out Camera mainCamera))
            {
                EnsurePhysicsRaycaster(mainCamera);
            }
        }

        private static bool IsPointerCamera(Camera camera)
        {
            if (camera == null || !camera.isActiveAndEnabled) return false;

            string cameraName = camera.gameObject.name;
            return camera.CompareTag("MainCamera")
                || cameraName == "Main Camera"
                || cameraName == "UI Camera";
        }

        private static void EnsurePhysicsRaycaster(Camera camera)
        {
            if (camera.GetComponent<PhysicsRaycaster>() == null)
            {
                camera.gameObject.AddComponent<PhysicsRaycaster>();
            }
        }
    }
}
