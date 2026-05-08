using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    [DefaultExecutionOrder(-120)]
    public sealed class SceneWorldPointerRouter : MonoBehaviour
    {
        private const string RouterName = "SceneWorldPointerRouter";

        private enum PressDispatchMode
        {
            None,
            PointerGesture,
            ClickOnly
        }

        [Header("World Raycast")]
        [SerializeField] private LayerMask worldLayerMask = Physics.DefaultRaycastLayers;
        [SerializeField] private float maxRaycastDistance = 1000f;

        [Header("UI Priority")]
        [SerializeField] private bool blockWorldWhenActionableUiHit = true;

        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>(16);

        private GameObject _pressedTarget;
        private GameObject _pressedHitObject;
        private PressDispatchMode _pressedMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAfterSceneLoad()
        {
            Ensure();
        }

        public static SceneWorldPointerRouter Ensure()
        {
            ScenePointerRouting.Ensure();

            SceneWorldPointerRouter existing = FindObjectOfType<SceneWorldPointerRouter>(true);
            if (existing != null)
            {
                return existing;
            }

            GameObject routerObject = new GameObject(RouterName);
            return routerObject.AddComponent<SceneWorldPointerRouter>();
        }

        private void Awake()
        {
            gameObject.name = RouterName;
            ScenePointerRouting.Ensure();
        }

        private void Update()
        {
            if (EventSystem.current == null)
            {
                ScenePointerRouting.Ensure();
            }

            Vector2 pointerPosition = ProjectInput.PointerPosition;

            if (_pressedTarget != null && ProjectInput.IsPrimaryPointerHeld())
            {
                HandlePressedPointerMove(pointerPosition);
            }

            if (ProjectInput.WasPrimaryPointerPressed())
            {
                HandlePointerDown(pointerPosition);
            }

            if (ProjectInput.WasPrimaryPointerReleased())
            {
                HandlePointerUp(pointerPosition);
            }
        }

        private void HandlePointerDown(Vector2 pointerPosition)
        {
            ClearPressedTarget();

            if (IsPointerBlockedByActionableUi(pointerPosition)) return;
            if (!TryGetWorldPointerHit(pointerPosition, out RaycastHit hit)) return;

            GameObject hitObject = hit.collider.gameObject;
            PointerEventData eventData = CreatePointerEventData(pointerPosition, hitObject, hit);
            GameObject downTarget = ExecuteEvents.ExecuteHierarchy(hitObject, eventData, ExecuteEvents.pointerDownHandler);

            if (downTarget != null && eventData.used)
            {
                SetPressedTarget(downTarget, hitObject, PressDispatchMode.PointerGesture);
                return;
            }

            GameObject clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);
            if (clickTarget != null)
            {
                SetPressedTarget(clickTarget, hitObject, PressDispatchMode.ClickOnly);
                return;
            }

            if (downTarget != null)
            {
                SetPressedTarget(downTarget, hitObject, PressDispatchMode.PointerGesture);
                return;
            }

            GameObject upTarget = ExecuteEvents.GetEventHandler<IPointerUpHandler>(hitObject);
            if (upTarget != null)
            {
                SetPressedTarget(upTarget, hitObject, PressDispatchMode.PointerGesture);
            }
        }

        private void HandlePointerUp(Vector2 pointerPosition)
        {
            if (_pressedTarget == null) return;

            if (IsPointerBlockedByActionableUi(pointerPosition)
                || !TryGetWorldPointerHit(pointerPosition, out RaycastHit hit))
            {
                CancelPressedTarget(pointerPosition);
                ClearPressedTarget();
                return;
            }

            GameObject hitObject = hit.collider.gameObject;
            GameObject releaseTarget = ResolveTargetForMode(hitObject, _pressedMode);
            PointerEventData eventData = CreatePointerEventData(pointerPosition, hitObject, hit);
            eventData.pointerPress = _pressedTarget;

            if (releaseTarget == _pressedTarget)
            {
                if (_pressedMode == PressDispatchMode.PointerGesture)
                {
                    ExecuteEvents.Execute(_pressedTarget, eventData, ExecuteEvents.pointerUpHandler);
                }

                ExecuteEvents.Execute(_pressedTarget, eventData, ExecuteEvents.pointerClickHandler);
            }
            else
            {
                CancelPressedTarget(pointerPosition, eventData);
            }

            ClearPressedTarget();
        }

        private void HandlePressedPointerMove(Vector2 pointerPosition)
        {
            if (_pressedTarget == null) return;

            if (IsPointerBlockedByActionableUi(pointerPosition)
                || !TryGetWorldPointerHit(pointerPosition, out RaycastHit hit))
            {
                CancelPressedTarget(pointerPosition);
                ClearPressedTarget();
                return;
            }

            GameObject releaseTarget = ResolveTargetForMode(hit.collider.gameObject, _pressedMode);
            if (releaseTarget == _pressedTarget) return;

            CancelPressedTarget(pointerPosition, CreatePointerEventData(pointerPosition, hit.collider.gameObject, hit));
            ClearPressedTarget();
        }

        private bool TryGetWorldPointerHit(Vector2 pointerPosition, out RaycastHit pointerHit)
        {
            pointerHit = default;

            if (!SceneCameraProvider.TryGetUICamera(out Camera pointerCamera, this))
            {
                return false;
            }

            Ray pointerRay = pointerCamera.ScreenPointToRay(pointerPosition);
            RaycastHit[] hits = Physics.RaycastAll(
                pointerRay,
                maxRaycastDistance,
                worldLayerMask,
                QueryTriggerInteraction.Collide);

            if (hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, CompareHitDistance);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                if (!HasWorldPointerTarget(hit.collider.gameObject)) continue;

                pointerHit = hit;
                return true;
            }

            return false;
        }

        private bool IsPointerBlockedByActionableUi(Vector2 pointerPosition)
        {
            if (!blockWorldWhenActionableUiHit) return false;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            PointerEventData eventData = new PointerEventData(eventSystem)
            {
                position = pointerPosition
            };

            _uiRaycastResults.Clear();
            eventSystem.RaycastAll(eventData, _uiRaycastResults);

            foreach (RaycastResult result in _uiRaycastResults)
            {
                GameObject target = result.gameObject;
                if (target == null) continue;
                if (result.module is PhysicsRaycaster) continue;

                if (IsActionableUi(target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsActionableUi(GameObject target)
        {
            if (target.GetComponentInParent<Selectable>() != null) return true;
            if (target.GetComponentInParent<ScrollRect>() != null) return true;

            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
            if (handler == null)
            {
                handler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(target);
            }

            if (handler == null)
            {
                handler = ExecuteEvents.GetEventHandler<IPointerUpHandler>(target);
            }

            return handler != null && handler.GetComponentInParent<Canvas>() != null;
        }

        private static bool HasWorldPointerTarget(GameObject target)
        {
            return ExecuteEvents.GetEventHandler<IPointerDownHandler>(target) != null
                || ExecuteEvents.GetEventHandler<IPointerUpHandler>(target) != null
                || ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) != null;
        }

        private static GameObject ResolveTargetForMode(GameObject hitObject, PressDispatchMode mode)
        {
            if (mode == PressDispatchMode.ClickOnly)
            {
                return ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);
            }

            GameObject target = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hitObject);
            if (target != null)
            {
                return target;
            }

            return ExecuteEvents.GetEventHandler<IPointerUpHandler>(hitObject);
        }

        private static PointerEventData CreatePointerEventData(Vector2 pointerPosition, GameObject hitObject, RaycastHit hit)
        {
            RaycastResult raycastResult = new RaycastResult
            {
                gameObject = hitObject,
                distance = hit.distance,
                worldPosition = hit.point,
                worldNormal = hit.normal,
                screenPosition = pointerPosition
            };

            return new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = pointerPosition,
                pressPosition = pointerPosition,
                pointerEnter = hitObject,
                pointerCurrentRaycast = raycastResult,
                pointerPressRaycast = raycastResult
            };
        }

        private PointerEventData CreatePointerEventData(Vector2 pointerPosition)
        {
            return new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = pointerPosition,
                pressPosition = pointerPosition,
                pointerEnter = _pressedHitObject
            };
        }

        private void CancelPressedTarget(Vector2 pointerPosition)
        {
            CancelPressedTarget(pointerPosition, CreatePointerEventData(pointerPosition));
        }

        private void CancelPressedTarget(Vector2 pointerPosition, PointerEventData eventData)
        {
            if (_pressedTarget == null || _pressedMode != PressDispatchMode.PointerGesture) return;

            eventData.position = pointerPosition;
            ExecuteEvents.Execute(_pressedTarget, eventData, ExecuteEvents.pointerExitHandler);
        }

        private void SetPressedTarget(GameObject target, GameObject hitObject, PressDispatchMode mode)
        {
            _pressedTarget = target;
            _pressedHitObject = hitObject;
            _pressedMode = mode;
        }

        private void ClearPressedTarget()
        {
            _pressedTarget = null;
            _pressedHitObject = null;
            _pressedMode = PressDispatchMode.None;
        }

        private static int CompareHitDistance(RaycastHit left, RaycastHit right)
        {
            return left.distance.CompareTo(right.distance);
        }
    }
}
