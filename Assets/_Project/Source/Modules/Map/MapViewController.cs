using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public class MapViewController : MonoBehaviour
    {
        public static MapViewController ActiveInstance { get; private set; }

        public enum ViewState
        {
            Small_Icon,
            Medium_Screen,
            Full_Screen
        }

        [System.Serializable]
        public struct ViewStateConfig
        {
            public string stateName;
            public List<GameObject> objectsToShow;
            public List<GameObject> objectsToHide;
        }

        [Header("State")]
        public ViewState currentState = ViewState.Small_Icon;

        [Header("Pointer Collider")]
        [Tooltip("Collider used by MapGestureInputController and the scene world pointer router.")]
        public BoxCollider targetCollider;
        public Vector3 smallColliderSize = new Vector3(0.2f, 0.2f, 0.1f);
        public Vector3 mediumColliderSize = new Vector3(1.5f, 1.0f, 0.1f);
        public Vector3 fullColliderSize = new Vector3(3.0f, 1.5f, 0.1f);

        [Header("Views")]
        public Vector3 startScale = Vector3.one;
        public float smallIconFixMultiplier = 1.0f;
        public GameObject viewSmall;
        public GameObject viewMedium;
        public Vector3 mediumTargetScale = Vector3.one;
        public GameObject viewFull;
        public Vector3 fullTargetScale = Vector3.one;

        [Header("Selector")]
        public LiquidMenuItem selectorMenu;
        public Vector3 selectorTargetScale = Vector3.one;

        [Header("Animation")]
        public float popDuration = 0.6f;
        public float retractDuration = 0.3f;
        [Range(0, 2)] public float elasticity = 1.0f;
        public Ease openEase = Ease.OutElastic;
        public Ease closeEase = Ease.InBack;

        [Header("Transition Queue")]
        [Tooltip("Queue the latest requested map state while another map state transition is still playing.")]
        public bool queueStateChangesWhileTransitioning = true;

        [Header("Visibility")]
        public ViewStateConfig smallConfig = new ViewStateConfig { stateName = "Small State Config" };
        public ViewStateConfig mediumConfig = new ViewStateConfig { stateName = "Medium State Config" };
        public ViewStateConfig fullConfig = new ViewStateConfig { stateName = "Full State Config" };

        [Header("Visibility Timing")]
        [Tooltip("Switch state-owned UI fragments at the beginning of a map transition so labels and side panels stay in rhythm with the map surface.")]
        public bool syncConfigVisibilityWithTransition = true;
        [Min(0f)]
        [Tooltip("Inspector-tunable delay before state-owned UI fragments switch during a map transition. Increase it when labels feel too early; decrease it when they feel late.")]
        public float configVisibilityTransitionDelay = 0.5f;

        [Header("Delayed Icons")]
        public List<GameObject> delayedIcons;
        public float appearDelay = 3.0f;
        public float appearDuration = 1.0f;

        private bool _isTransitioning;
        private bool _hasQueuedState;
        private ViewState _queuedState;
        private Sequence _transitionSequence;
        private Tween _delayedCallTween;

        public bool IsTransitioning => _isTransitioning;

        private void Awake()
        {
            if (ActiveInstance != null && ActiveInstance != this)
            {
                ProjectLog.Warning("Multiple MapViewController instances detected; the latest instance will be used.", this);
            }

            ActiveInstance = this;
        }

        private void OnDestroy()
        {
            _transitionSequence?.Kill();
            _delayedCallTween?.Kill();

            if (ActiveInstance == this)
            {
                ActiveInstance = null;
            }
        }

        private void Start()
        {
            if (viewSmall) startScale = viewSmall.transform.localScale;

            ForceInitView(viewSmall, true, startScale);
            ForceInitView(viewMedium, false, mediumTargetScale);
            ForceInitView(viewFull, false, fullTargetScale);

            ResetIconController(viewSmall, startScale);
            CloseSelectorMenuInstant();
            HideDelayedIconsInstant();
            UpdateColliderSize(ViewState.Small_Icon);
            ApplyConfig(smallConfig);
        }

        public void TriggerMediumView()
        {
            SwitchToState(ViewState.Medium_Screen);
        }

        public void TriggerFullView()
        {
            SwitchToState(ViewState.Full_Screen);
        }

        public void TriggerSmallView()
        {
            SwitchToState(ViewState.Small_Icon);
        }

        public void ToggleMediumView()
        {
            SwitchToState(currentState == ViewState.Medium_Screen
                ? ViewState.Small_Icon
                : ViewState.Medium_Screen);
        }

        public void ToggleFullView()
        {
            SwitchToState(currentState == ViewState.Full_Screen
                ? ViewState.Medium_Screen
                : ViewState.Full_Screen);
        }

        public void SwitchToState(ViewState newState, bool instant = false)
        {
            if (_isTransitioning && !instant && queueStateChangesWhileTransitioning)
            {
                if (currentState != newState)
                {
                    QueueStateChange(newState);
                }

                return;
            }

            if (!_isTransitioning && currentState == newState && !instant)
            {
                ForceCurrentStateConsistency();
                return;
            }

            currentState = newState;
            _isTransitioning = true;
            UpdateColliderSize(newState);

            ResolveTargetView(newState, out GameObject targetView, out Vector3 endScale);

            _transitionSequence?.Kill(false);
            _delayedCallTween?.Kill();
            StopViewAnimations();

            if (instant)
            {
                ClearQueuedState();
                ForceOnlyTargetView(targetView, endScale);
                CompleteTransition(newState, null);
                return;
            }

            Sequence sequence = DOTween.Sequence();
            _transitionSequence = sequence;

            AppendHideNonTargetViews(sequence, targetView);
            sequence.AppendCallback(() => PrepareTargetView(targetView, endScale));
            AppendShowTargetView(sequence, targetView, endScale);
            AppendConfigVisibilitySync(sequence, newState);

            sequence.OnKill(() =>
            {
                if (_transitionSequence == sequence)
                {
                    _transitionSequence = null;
                }
            });
            sequence.OnComplete(() =>
            {
                CompleteTransition(newState, sequence);
            });
        }

        public void QueueStateChange(ViewState newState)
        {
            _queuedState = newState;
            _hasQueuedState = true;
        }

        public void CycleNext()
        {
            if (currentState == ViewState.Small_Icon)
            {
                SwitchToState(ViewState.Medium_Screen);
            }
            else if (currentState == ViewState.Medium_Screen)
            {
                SwitchToState(ViewState.Full_Screen);
            }
            else if (viewFull)
            {
                viewFull.transform.DOKill(true);
                viewFull.transform.DOPunchRotation(new Vector3(0f, 0f, 2f), 0.3f, 10, 1f);
            }
        }

        public void CyclePrev()
        {
            if (currentState == ViewState.Full_Screen)
            {
                SwitchToState(ViewState.Medium_Screen);
                return;
            }

            SwitchToState(ViewState.Small_Icon);
        }

        public void OpenSelectorMenu()
        {
            if (!selectorMenu) return;

            selectorMenu.gameObject.SetActive(true);
            selectorMenu.transform.DOKill();
            selectorMenu.transform.localScale = Vector3.zero;
            selectorMenu.transform.DOScale(selectorTargetScale, popDuration).SetEase(openEase, elasticity);
            selectorMenu.OpenChildren();
        }

        public void CloseSelectorMenu()
        {
            if (!selectorMenu || !selectorMenu.gameObject.activeSelf) return;

            selectorMenu.transform.DOKill();
            selectorMenu.transform
                .DOScale(Vector3.zero, 0.2f)
                .OnComplete(() => selectorMenu.gameObject.SetActive(false));
        }

        private void ResolveTargetView(ViewState state, out GameObject targetView, out Vector3 targetScale)
        {
            switch (state)
            {
                case ViewState.Medium_Screen:
                    targetView = viewMedium;
                    targetScale = mediumTargetScale;
                    break;
                case ViewState.Full_Screen:
                    targetView = viewFull;
                    targetScale = fullTargetScale;
                    break;
                default:
                    targetView = viewSmall;
                    targetScale = startScale * smallIconFixMultiplier;
                    break;
            }
        }

        private void StopViewAnimations()
        {
            StopViewAnimation(viewSmall);
            StopViewAnimation(viewMedium);
            StopViewAnimation(viewFull);
        }

        private static void StopViewAnimation(GameObject view)
        {
            if (!view) return;

            view.transform.DOKill();
            view.GetComponent<LiquidIconController>()?.StopAllAnimations();
        }

        private void AppendConfigVisibilitySync(Sequence sequence, ViewState newState)
        {
            if (!syncConfigVisibilityWithTransition) return;

            sequence.InsertCallback(
                Mathf.Max(0f, configVisibilityTransitionDelay),
                () => ApplyConfigForState(newState));
        }

        private void AppendHideNonTargetViews(Sequence sequence, GameObject targetView)
        {
            AppendHideView(sequence, viewSmall, targetView);
            AppendHideView(sequence, viewMedium, targetView);
            AppendHideView(sequence, viewFull, targetView);

            sequence.AppendCallback(() =>
            {
                DeactivateIfNotTarget(viewSmall, targetView);
                DeactivateIfNotTarget(viewMedium, targetView);
                DeactivateIfNotTarget(viewFull, targetView);
            });
        }

        private void AppendHideView(Sequence sequence, GameObject view, GameObject targetView)
        {
            if (view && view != targetView && view.activeSelf)
            {
                sequence.Join(view.transform.DOScale(Vector3.zero, retractDuration).SetEase(closeEase));
            }
        }

        private void PrepareTargetView(GameObject targetView, Vector3 targetScale)
        {
            if (!targetView) return;

            ResetIconController(targetView, targetScale);
            targetView.SetActive(true);
            targetView.transform.DOKill();
            targetView.transform.localScale = Vector3.zero;
        }

        private void AppendShowTargetView(Sequence sequence, GameObject targetView, Vector3 targetScale)
        {
            if (!targetView) return;

            sequence.Append(targetView.transform.DOScale(targetScale, popDuration).SetEase(openEase, elasticity));
        }

        private void ForceOnlyTargetView(GameObject targetView, Vector3 targetScale)
        {
            ResetIconController(targetView, targetScale);
            ForceViewState(viewSmall, viewSmall == targetView, viewSmall == targetView ? targetScale : Vector3.zero);
            ForceViewState(viewMedium, viewMedium == targetView, viewMedium == targetView ? targetScale : Vector3.zero);
            ForceViewState(viewFull, viewFull == targetView, viewFull == targetView ? targetScale : Vector3.zero);
        }

        private void ForceCurrentStateConsistency()
        {
            ResolveTargetView(currentState, out GameObject targetView, out Vector3 targetScale);
            ForceOnlyTargetView(targetView, targetScale);
            ApplyConfigForState(currentState);
            HandleDelayedIcons(currentState);
        }

        private static void ForceViewState(GameObject view, bool isActive, Vector3 scale)
        {
            if (!view) return;

            view.transform.DOKill();
            view.SetActive(isActive);
            view.transform.localScale = isActive ? scale : Vector3.zero;
        }

        private static void DeactivateIfNotTarget(GameObject view, GameObject targetView)
        {
            if (view && view != targetView)
            {
                view.SetActive(false);
            }
        }

        private void CompleteTransition(ViewState newState, Sequence completedSequence)
        {
            if (completedSequence == null || _transitionSequence == completedSequence)
            {
                _transitionSequence = null;
            }

            _isTransitioning = false;
            ApplyConfigForState(newState);
            HandleDelayedIcons(newState);
            RunQueuedStateChange();
        }

        private void RunQueuedStateChange()
        {
            if (!_hasQueuedState) return;

            ViewState queuedState = _queuedState;
            ClearQueuedState();

            if (queuedState == currentState)
            {
                ForceCurrentStateConsistency();
                return;
            }

            SwitchToState(queuedState);
        }

        private void ClearQueuedState()
        {
            _hasQueuedState = false;
        }

        private static void ResetIconController(GameObject view, Vector3 scale)
        {
            if (!view) return;

            LiquidIconController iconController = view.GetComponent<LiquidIconController>();
            if (!iconController) return;

            iconController.ForceUpdateOriginalScale(scale);
            iconController.ResetState();
        }

        private void UpdateColliderSize(ViewState state)
        {
            if (targetCollider == null) return;

            switch (state)
            {
                case ViewState.Medium_Screen:
                    targetCollider.size = mediumColliderSize;
                    break;
                case ViewState.Full_Screen:
                    targetCollider.size = fullColliderSize;
                    break;
                default:
                    targetCollider.size = smallColliderSize;
                    break;
            }
        }

        private void ApplyConfigForState(ViewState state)
        {
            switch (state)
            {
                case ViewState.Medium_Screen:
                    ApplyConfig(mediumConfig);
                    break;
                case ViewState.Full_Screen:
                    ApplyConfig(fullConfig);
                    break;
                default:
                    ApplyConfig(smallConfig);
                    break;
            }
        }

        private static void ApplyConfig(ViewStateConfig config)
        {
            SetObjectsActive(config.objectsToHide, false);
            SetObjectsActive(config.objectsToShow, true);
        }

        private static void SetObjectsActive(List<GameObject> objects, bool isActive)
        {
            if (objects == null) return;

            foreach (GameObject target in objects)
            {
                if (target) target.SetActive(isActive);
            }
        }

        private void HandleDelayedIcons(ViewState newState)
        {
            _delayedCallTween?.Kill();

            if (newState == ViewState.Small_Icon)
            {
                HideDelayedIconsAnimated();
                return;
            }

            _delayedCallTween = DOVirtual.DelayedCall(appearDelay, ShowDelayedIcons);
        }

        private void ShowDelayedIcons()
        {
            if (delayedIcons == null) return;

            foreach (GameObject icon in delayedIcons)
            {
                if (!icon) continue;

                icon.SetActive(true);
                icon.transform.localScale = Vector3.zero;
                icon.transform.DOScale(Vector3.one, appearDuration).SetEase(Ease.OutSine);
            }
        }

        private void HideDelayedIconsAnimated()
        {
            if (delayedIcons == null) return;

            foreach (GameObject icon in delayedIcons)
            {
                if (!icon) continue;

                icon.transform.DOKill();
                icon.transform
                    .DOScale(Vector3.zero, 0.3f)
                    .OnComplete(() => icon.SetActive(false));
            }
        }

        private void HideDelayedIconsInstant()
        {
            if (delayedIcons == null) return;

            foreach (GameObject icon in delayedIcons)
            {
                if (!icon) continue;

                icon.transform.localScale = Vector3.zero;
                icon.SetActive(false);
            }
        }

        private void CloseSelectorMenuInstant()
        {
            if (!selectorMenu) return;

            selectorMenu.transform.localScale = Vector3.zero;
            selectorMenu.gameObject.SetActive(false);
        }

        private static void ForceInitView(GameObject view, bool active, Vector3 scale)
        {
            if (!view) return;

            view.SetActive(active);
            view.transform.localScale = active ? scale : Vector3.zero;

            if (active)
            {
                ResetIconController(view, scale);
            }
        }
    }
}
