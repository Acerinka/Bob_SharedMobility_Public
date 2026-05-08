using UnityEngine;
using UnityEngine.EventSystems;

namespace Bob.SharedMobility
{
    public class MapViewModeButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Target")]
        public MapViewController stateController;
        public MapViewController.ViewState targetState;

        [Header("Optional Visual")]
        public Transform visualIcon;

        private void Awake()
        {
            ResolveControllerIfNeeded();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            eventData.Use();
            OnClick(stateController);
        }

        public void OnClick(MapViewController controller)
        {
            if (controller == null)
            {
                ResolveControllerIfNeeded();
                controller = stateController;
            }

            if (controller == null)
            {
                ProjectLog.Warning($"{name} cannot switch map mode because no MapViewController is assigned.", this);
                return;
            }

            controller.SwitchToState(targetState);
            controller.CloseSelectorMenu();
        }

        private void ResolveControllerIfNeeded()
        {
            if (stateController != null) return;

            stateController = GetComponentInParent<MapViewController>();
            if (stateController == null)
            {
                stateController = FindObjectOfType<MapViewController>();
            }
        }
    }
}
