using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class MapViewController
    {
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
            StopDelayedIconAnimations();
        }

        private static void StopViewAnimation(GameObject view)
        {
            if (!view) return;

            view.transform.DOKill();
            LiquidIconController iconController = view.GetComponent<LiquidIconController>();
            // Surface changes own the active map icon; old icon callbacks must not reopen a previous state.
            iconController?.CancelBobInteraction();
            iconController?.StopAllAnimations();

            LiquidMenuItem menuItem = view.GetComponent<LiquidMenuItem>();
            menuItem?.CancelExternalRuntimeControl(true);
        }

        private void HideNonTargetViewsImmediately(GameObject targetView)
        {
            HideViewImmediatelyIfNotTarget(viewSmall, targetView);
            HideViewImmediatelyIfNotTarget(viewMedium, targetView);
            HideViewImmediatelyIfNotTarget(viewFull, targetView);
            RefreshRuntimeSnapshot();
        }

        private static void HideViewImmediatelyIfNotTarget(GameObject view, GameObject targetView)
        {
            if (!view || view == targetView) return;

            StopViewAnimation(view);
            view.transform.localScale = Vector3.zero;
            view.SetActive(false);
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
            RefreshRuntimeSnapshot();
        }

        private void ForceCurrentStateConsistency()
        {
            ResolveTargetView(currentState, out GameObject targetView, out Vector3 targetScale);
            ForceOnlyTargetView(targetView, targetScale);
            settledState = currentState;
            ApplyConfigForState(currentState);
            HandleDelayedIcons(currentState);
            RefreshRuntimeSnapshot();
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
