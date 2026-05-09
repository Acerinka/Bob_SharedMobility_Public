using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class MapViewController : MonoBehaviour
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

        [Header("Transition Requests")]
        [Tooltip("When a new map state is requested mid-transition, retarget from the current visual pose instead of waiting for the old transition to finish.")]
        public bool retargetStateChangesWhileTransitioning = true;
        [Tooltip("Queue the latest requested map state while another map state transition is still playing. Used when retargeting is disabled.")]
        public bool queueStateChangesWhileTransitioning = true;

        [Header("Surface Ownership")]
        [Tooltip("Keep only the target map surface visible during state changes. This prevents the mini map and expanded map from animating at the same time.")]
        public bool enforceExclusiveSurfaceOwnership = true;

        [Header("Visibility")]
        public ViewStateConfig smallConfig = new ViewStateConfig { stateName = "Small State Config" };
        public ViewStateConfig mediumConfig = new ViewStateConfig { stateName = "Medium State Config" };
        public ViewStateConfig fullConfig = new ViewStateConfig { stateName = "Full State Config" };

        [Header("Visibility Timing")]
        [Tooltip("Switch state-owned UI fragments at the beginning of a map transition so labels and side panels stay in rhythm with the map surface.")]
        public bool syncConfigVisibilityWithTransition = true;
        [Min(0f)]
        [Tooltip("Inspector-tunable delay before state-owned UI fragments switch during a map transition. Increase it when labels feel too early; decrease it when they feel late.")]
        public float configVisibilityTransitionDelay = 0.25f;
        [Tooltip("Animate state-owned UI fragments instead of hard toggling them with SetActive.")]
        public bool animateConfigVisibility = true;
        [Min(0f)]
        [Tooltip("Fade/scale duration for Home/Work, distance, route card, and similar state-owned fragments.")]
        public float configVisibilityFadeDuration = 0.25f;
        [Range(0f, 1f)]
        [Tooltip("Scale used while a state-owned fragment is hidden. 0 is a full pop, 1 is fade-only.")]
        public float configHiddenScaleMultiplier = 0.92f;

        [Header("Delayed Icons")]
        public List<GameObject> delayedIcons;
        public float appearDelay = 3.0f;
        public float appearDuration = 1.0f;

        [Header("Runtime Snapshot (Read Only)")]
        [SerializeField] private ViewState settledState = ViewState.Small_Icon;
        [SerializeField] private string visibleSurfaceDebug = "";
        [SerializeField] private string queuedStateDebug = "<none>";

        private bool _isTransitioning;
        private bool _hasQueuedState;
        private ViewState _queuedState;
        private Sequence _transitionSequence;
        private Tween _delayedCallTween;
        private readonly MapFragmentVisibilityPresenter _fragmentVisibilityPresenter = new MapFragmentVisibilityPresenter();

        public bool IsTransitioning => _isTransitioning;
        public ViewState SettledState => settledState;
        public string VisibleSurfaceDebug => visibleSurfaceDebug;
        public string QueuedStateDebug => queuedStateDebug;

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

            _fragmentVisibilityPresenter.Cache(smallConfig, mediumConfig, fullConfig);
            ResetIconController(viewSmall, startScale);
            CloseSelectorMenuInstant();
            HideDelayedIconsInstant();
            UpdateColliderSize(ViewState.Small_Icon);
            _fragmentVisibilityPresenter.Apply(smallConfig, configHiddenScaleMultiplier);
            RefreshRuntimeSnapshot();
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
            if (_isTransitioning && !instant)
            {
                if (currentState == newState)
                {
                    return;
                }

                if (!retargetStateChangesWhileTransitioning && queueStateChangesWhileTransitioning)
                {
                    QueueStateChange(newState);
                    return;
                }

                ClearQueuedState();
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

            if (enforceExclusiveSurfaceOwnership)
            {
                HideNonTargetViewsImmediately(targetView);
            }

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
            AppendConfigVisibilitySync(sequence, newState, targetView);

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

            RefreshRuntimeSnapshot();
        }

        public void QueueStateChange(ViewState newState)
        {
            _queuedState = newState;
            _hasQueuedState = true;
            RefreshRuntimeSnapshot();
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

        [ContextMenu("Diagnostics/Validate Map Runtime State")]
        public void ValidateRuntimeState()
        {
            RefreshRuntimeSnapshot();

            int visibleSurfaceCount = CountVisibleSurfaces();
            if (enforceExclusiveSurfaceOwnership && visibleSurfaceCount > 1)
            {
                ProjectLog.Warning(
                    $"Map surface ownership is broken: visible={visibleSurfaceDebug}; requested={currentState}; settled={settledState}; transitioning={_isTransitioning}",
                    this);
            }

            if (_isTransitioning && _transitionSequence == null)
            {
                ProjectLog.Warning(
                    $"Map transition flag is set without an active transition sequence. requested={currentState}; visible={visibleSurfaceDebug}",
                    this);
            }
        }

    }
}
