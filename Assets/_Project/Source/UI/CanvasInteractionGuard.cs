using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    [DisallowMultipleComponent]
    public class CanvasInteractionGuard : MonoBehaviour
    {
        [Header("Diagnostics")]
        public bool debugClick = false;

        [Header("Bootstrap")]
        public bool autoAssignCamera = true;
        public bool autoDisableBlockingImages = true;
        [SerializeField] private bool ensurePointerRouting = true;

        private void Awake()
        {
            if (ensurePointerRouting)
            {
                ScenePointerRouting.Ensure();
            }
        }

        private void Start()
        {
            AssignWorldCameraIfNeeded();

            if (autoDisableBlockingImages)
            {
                FixRaycastTargets();
            }
        }

        private void Update()
        {
            if (debugClick && ProjectInput.WasPrimaryPointerPressed())
            {
                LogPointerRaycastStack();
            }
        }

        public void FixRaycastTargets()
        {
            int disabledImageCount = 0;
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                bool belongsToSelectable = image.GetComponentInParent<Selectable>() != null;
                bool hasPointerHandler = HasPointerHandler(image.gameObject);
                bool shouldReceiveRaycasts = belongsToSelectable || hasPointerHandler;

                if (image.raycastTarget != shouldReceiveRaycasts)
                {
                    if (!shouldReceiveRaycasts)
                    {
                        disabledImageCount++;
                    }

                    image.raycastTarget = shouldReceiveRaycasts;
                }
            }

            Text[] texts = GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (text.GetComponentInParent<Selectable>() != null)
                {
                    text.raycastTarget = false;
                }
            }

            if (disabledImageCount > 0)
            {
                ProjectLog.Info($"Disabled {disabledImageCount} decorative Image raycast targets on {name}.", this);
            }
        }

        private static bool HasPointerHandler(GameObject target)
        {
            return ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) != null
                || ExecuteEvents.GetEventHandler<IPointerDownHandler>(target) != null
                || ExecuteEvents.GetEventHandler<IPointerUpHandler>(target) != null
                || ExecuteEvents.GetEventHandler<IBeginDragHandler>(target) != null
                || ExecuteEvents.GetEventHandler<IDragHandler>(target) != null
                || ExecuteEvents.GetEventHandler<IEndDragHandler>(target) != null;
        }

        private void AssignWorldCameraIfNeeded()
        {
            if (!autoAssignCamera) return;

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null
                || canvas.renderMode != RenderMode.WorldSpace
                || canvas.worldCamera != null)
            {
                return;
            }

            if (SceneCameraProvider.TryGetUICamera(out Camera uiCamera, this))
            {
                canvas.worldCamera = uiCamera;
            }
        }

        private void LogPointerRaycastStack()
        {
            if (EventSystem.current == null)
            {
                ProjectLog.Warning("Cannot inspect pointer raycasts because no EventSystem is active.", this);
                return;
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = ProjectInput.PointerPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count == 0)
            {
                ProjectLog.Info("Pointer raycast stack is empty.", this);
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                RaycastResult result = results[i];
                string selectableMark = result.gameObject.GetComponentInParent<Selectable>() ? "Selectable" : "Blocking";
                ProjectLog.Info($"Pointer hit {i}: {selectableMark} | depth {result.depth} | {result.gameObject.name}", result.gameObject);
            }
        }
    }
}
