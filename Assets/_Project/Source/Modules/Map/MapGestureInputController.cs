using UnityEngine;
using UnityEngine.EventSystems;

namespace Bob.SharedMobility
{
    public class MapGestureInputController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Target")]
        public MapViewController stateController;

        [Header("Gesture")]
        public float longPressTime = 0.5f;

        [Header("Pointer Routing")]
        [SerializeField] private bool ensurePointerRouting = true;

        private bool _isPressed;
        private bool _hasTriggeredLongPress;
        private float _pressTimer;

        private void Awake()
        {
            if (stateController == null)
            {
                stateController = GetComponentInParent<MapViewController>();
            }

            if (ensurePointerRouting)
            {
                SceneWorldPointerRouter.Ensure();
            }
        }

        private void Update()
        {
            if (!_isPressed || _hasTriggeredLongPress) return;

            _pressTimer += Time.deltaTime;
            if (_pressTimer >= longPressTime)
            {
                _hasTriggeredLongPress = true;
                stateController?.OpenSelectorMenu();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (stateController == null || IsSelectorMenuPointer(eventData.pointerEnter))
            {
                return;
            }

            _isPressed = true;
            _pressTimer = 0f;
            _hasTriggeredLongPress = false;
            eventData.Use();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed) return;

            bool shouldCycle = !_hasTriggeredLongPress;
            ResetGesture();
            eventData.Use();

            if (shouldCycle)
            {
                stateController?.CycleNext();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetGesture();
        }

        private bool IsSelectorMenuPointer(GameObject pointerTarget)
        {
            if (stateController == null || stateController.selectorMenu == null || pointerTarget == null)
            {
                return false;
            }

            Transform selectorTransform = stateController.selectorMenu.transform;
            Transform pointerTransform = pointerTarget.transform;
            return pointerTransform == selectorTransform || pointerTransform.IsChildOf(selectorTransform);
        }

        private void ResetGesture()
        {
            _isPressed = false;
            _pressTimer = 0f;
            _hasTriggeredLongPress = false;
        }
    }
}
