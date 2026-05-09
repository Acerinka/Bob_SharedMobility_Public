using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class MapViewController
    {
        private void AppendConfigVisibilitySync(Sequence sequence, ViewState newState, GameObject targetView)
        {
            if (!syncConfigVisibilityWithTransition) return;

            ViewStateConfig targetConfig = GetConfigForState(newState);
            if (!animateConfigVisibility)
            {
                sequence.InsertCallback(
                    Mathf.Max(0f, configVisibilityTransitionDelay),
                    () => _fragmentVisibilityPresenter.Apply(targetConfig, configHiddenScaleMultiplier));
                return;
            }

            float hideDuration = HasActiveNonTargetView(targetView) ? Mathf.Max(0f, retractDuration) : 0f;
            float showTime = Mathf.Max(0f, Mathf.Max(configVisibilityTransitionDelay, hideDuration * 0.8f));
            float duration = Mathf.Max(0f, configVisibilityFadeDuration);

            _fragmentVisibilityPresenter.InsertTransitionTweens(
                sequence,
                targetConfig,
                showTime,
                duration,
                configHiddenScaleMultiplier);
        }

        private void CompleteTransition(ViewState newState, Sequence completedSequence)
        {
            if (completedSequence == null || _transitionSequence == completedSequence)
            {
                _transitionSequence = null;
            }

            _isTransitioning = false;
            settledState = newState;
            ApplyConfigForState(newState);
            HandleDelayedIcons(newState);
            RefreshRuntimeSnapshot();
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
            RefreshRuntimeSnapshot();
        }

        private void RefreshRuntimeSnapshot()
        {
            visibleSurfaceDebug = DescribeVisibleSurfaces();
            queuedStateDebug = _hasQueuedState ? _queuedState.ToString() : "<none>";
        }

        private string DescribeVisibleSurfaces()
        {
            string result = "";
            AppendVisibleSurface(ref result, viewSmall, "Small");
            AppendVisibleSurface(ref result, viewMedium, "Medium");
            AppendVisibleSurface(ref result, viewFull, "Full");

            return string.IsNullOrEmpty(result) ? "<none>" : result;
        }

        private int CountVisibleSurfaces()
        {
            int count = 0;
            if (viewSmall && viewSmall.activeSelf) count++;
            if (viewMedium && viewMedium.activeSelf) count++;
            if (viewFull && viewFull.activeSelf) count++;
            return count;
        }

        private static void AppendVisibleSurface(ref string result, GameObject view, string label)
        {
            if (!view || !view.activeSelf) return;

            result = string.IsNullOrEmpty(result) ? label : $"{result}+{label}";
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
            _fragmentVisibilityPresenter.Apply(GetConfigForState(state), configHiddenScaleMultiplier);
        }

        private ViewStateConfig GetConfigForState(ViewState state)
        {
            switch (state)
            {
                case ViewState.Medium_Screen:
                    return mediumConfig;
                case ViewState.Full_Screen:
                    return fullConfig;
                default:
                    return smallConfig;
            }
        }

        private bool HasActiveNonTargetView(GameObject targetView)
        {
            return IsActiveNonTargetView(viewSmall, targetView)
                || IsActiveNonTargetView(viewMedium, targetView)
                || IsActiveNonTargetView(viewFull, targetView);
        }

        private static bool IsActiveNonTargetView(GameObject view, GameObject targetView)
        {
            return view && view != targetView && view.activeSelf;
        }
    }
}
