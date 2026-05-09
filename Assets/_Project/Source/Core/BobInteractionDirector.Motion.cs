using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class BobInteractionDirector
    {
        private bool CanReleaseBob(int interactionToken)
        {
            if (interactionToken == 0)
            {
                return _activeInteractionToken == 0;
            }

            return _activeInteractionToken == interactionToken;
        }

        private bool TryExecuteRuntimeMapCommand(BobTarget target, bool preserveBobPositionOnReset)
        {
            if (!IsCodeOwnedRuntimeTarget(target)) return false;

            MapViewController mapController = MapViewController.ActiveInstance;
            if (!mapController) return false;

            if (resetSceneBeforeRemoteTriggers)
            {
                SilentResetEverything(target.targetID, preserveBobPositionOnReset);
            }

            mapController.TriggerFullView();
            TriggerRemoteTarget(target, null, false);
            return true;
        }

        private static bool IsCodeOwnedRuntimeTarget(BobTarget target)
        {
            return target != null && BobTargetIds.IsMapFull(target.targetID);
        }

        private void ClearCurrentCommandLock()
        {
            _currentLocID = "";
        }

        private void TriggerRemoteTarget(BobTarget target)
        {
            TriggerRemoteTarget(target, null);
        }

        private void TriggerRemoteTarget(BobTarget target, System.Action runtimeAction)
        {
            TriggerRemoteTarget(target, runtimeAction, true);
        }

        private void TriggerRemoteTarget(
            BobTarget target,
            System.Action runtimeAction,
            bool invokeActionAfterDelay)
        {
            float recommendedDelay = 0.5f;

            if (bob)
            {
                bob.PrepareForRemoteInteractionAtHome();
                recommendedDelay = bob.PlayRemoteInteraction();
                if (recommendedDelay <= 0f)
                {
                    recommendedDelay = bob.remoteState.optimalTriggerDelay > 0f
                        ? bob.remoteState.optimalTriggerDelay
                        : 0.5f;
                }
            }

            _remoteEventTween?.Kill();
            _remoteEventTween = DOVirtual.DelayedCall(recommendedDelay, () =>
            {
                if (invokeActionAfterDelay && runtimeAction != null)
                {
                    runtimeAction.Invoke();
                }
                else if (invokeActionAfterDelay)
                {
                    target.onRemoteEvent?.Invoke();
                }

                ClearCurrentCommandLock();
                _remoteEventTween = null;
                RunQueuedTargetIfAny();
            }).SetId(BobFlightTweenId);
        }

        private void SilentResetEverything(string ignoreTargetID = null, bool preserveBobPositionAsMotionAnchor = false)
        {
            CancelActiveFlight(preserveBobPositionAsMotionAnchor);

            CloseCurrentNavigationSurface();

            if (registeredTargets != null)
            {
                foreach (BobTarget target in registeredTargets)
                {
                    if (target == null) continue;
                    if (ignoreTargetID != null && BobTargetIds.Equals(target.targetID, ignoreTargetID)) continue;
                    if (target.targetObject == null) continue;

                    target.targetObject.GetComponent<LiquidIconController>()?.ResetState();
                    target.targetObject.GetComponent<DockButtonController>()?.ResetState();
                }
            }
        }

        private static void CloseCurrentNavigationSurface()
        {
            if (AppNavigationService.Instance)
            {
                AppNavigationService.Instance.CloseCurrentScreen();
                return;
            }

            if (DockNavigationManager.Instance)
            {
                DockNavigationManager.Instance.CloseCurrentApp();
            }
        }

        private static Vector3 ResolveDestination(BobTarget target, DockButtonController dockButton)
        {
            Vector3 destination = target.targetObject.position;
            if (dockButton != null && dockButton.liquidBase3D != null)
            {
                destination = dockButton.liquidBase3D.position;
            }

            destination.z += target.zOffset;
            destination.y += target.yOffset;
            return destination;
        }

        private void FlyBobSequence(Vector3 targetPos, System.Action onArrival, bool isVanishOnArrival)
        {
            FlyBobSequence(targetPos, onArrival, isVanishOnArrival, true);
        }

        private void FlyBobSequence(
            Vector3 targetPos,
            System.Action onArrival,
            bool isVanishOnArrival,
            bool runQueuedTargetOnComplete)
        {
            if (!bob) return;

            CancelActiveFlight(false);

            Sequence sequence = DOTween.Sequence();
            sequence.SetId(BobFlightTweenId);
            _flightSequence = sequence;

            sequence.AppendCallback(() => bob.PrepareForFlight());
            sequence.Append(bob.AppearAnim(appearDuration));

            if (waitTimeBeforeFly > 0f)
            {
                sequence.AppendInterval(waitTimeBeforeFly);
            }

            if (isVanishOnArrival)
            {
                sequence.AppendCallback(() => bob.StartFlyingShape());
                sequence.Append(bob.transform.DOMove(targetPos, flyDuration).SetEase(Ease.InBack));
            }
            else
            {
                sequence.AppendCallback(() => bob.StartReturnShape(flyDuration));
                sequence.Append(bob.transform.DOMove(targetPos, flyDuration).SetEase(Ease.InOutSine));
            }

            sequence.AppendCallback(() =>
            {
                if (isVanishOnArrival) bob.ArriveAndVanish(targetPos);
                else bob.ArriveAndStay(targetPos);

                onArrival?.Invoke();
            });

            if (!isVanishOnArrival && bob.LandingRecoveryDuration > 0f)
            {
                sequence.AppendInterval(bob.LandingRecoveryDuration + landingCompletionBuffer);
            }

            sequence.OnComplete(() =>
            {
                CompleteFlight(sequence, runQueuedTargetOnComplete);
            });

            sequence.OnKill(() =>
            {
                if (_flightSequence == sequence)
                {
                    _flightSequence = null;
                }
            });
        }

        private int BeginTargetInteraction(string targetID)
        {
            int token = _nextInteractionToken++;
            if (_nextInteractionToken == int.MaxValue)
            {
                _nextInteractionToken = 1;
            }

            _activeInteractionToken = token;
            ProjectLog.Info($"Bob interaction started: {targetID}", this);
            return token;
        }

        private static bool ShouldWaitForBobReturn(LiquidIconController iconController, DockButtonController dockButton)
        {
            if (iconController != null) return iconController.shouldBobReturn;
            if (dockButton != null) return dockButton.shouldBobReturn;
            return false;
        }
    }
}
