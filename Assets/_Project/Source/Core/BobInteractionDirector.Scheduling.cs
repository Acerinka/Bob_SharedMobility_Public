using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class BobInteractionDirector
    {
        private bool ShouldIgnoreDuplicateCommand(BobTarget target)
        {
            if (BobTargetIds.Equals(_currentLocID, target.targetID)) return true;

            bool insideCooldown = Time.unscaledTime - _lastAcceptedCommandTime < duplicateCommandCooldown;
            return insideCooldown && BobTargetIds.Equals(_lastAcceptedTargetID, target.targetID);
        }

        private bool TryToggleAlreadyOpenDockTarget(BobTarget target, out BobCommandResult result)
        {
            result = BobCommandResult.IgnoredAlreadyOpen;

            if (!suppressAlreadyOpenDockTargets || target == null || target.targetObject == null) return false;

            DockButtonController dockButton = target.targetObject.GetComponent<DockButtonController>();
            if (!dockButton || !dockButton.myAppController) return false;

            CanvasGroup requestedSubPanel = target.targetLevel3Panel;

            AppNavigationService navigationService = AppNavigationService.Instance;
            if (navigationService)
            {
                if (navigationService.CurrentDockPanel != dockButton.myAppController
                    || navigationService.CurrentSubPanel != requestedSubPanel)
                {
                    return false;
                }

                navigationService.CloseCurrentScreen();
                ClearCurrentCommandLock();
                result = BobCommandResult.ToggledClosed;
                return true;
            }

            DockNavigationManager dockNavigationManager = DockNavigationManager.Instance;
            if (dockNavigationManager)
            {
                if (dockNavigationManager.CurrentActiveApp != dockButton.myAppController
                    || (requestedSubPanel != null && dockButton.myAppController.CurrentActiveLevel3 != requestedSubPanel))
                {
                    return false;
                }

                dockNavigationManager.CloseCurrentApp();
                ClearCurrentCommandLock();
                result = BobCommandResult.ToggledClosed;
                return true;
            }

            if (!dockButton.myAppController.IsOpen
                || (requestedSubPanel != null && dockButton.myAppController.CurrentActiveLevel3 != requestedSubPanel))
            {
                return false;
            }

            dockButton.myAppController.CloseEntireApp();
            ClearCurrentCommandLock();
            result = BobCommandResult.ToggledClosed;
            return true;
        }

        private bool ShouldIgnoreAlreadyOpenMapTarget(BobTarget target)
        {
            MapViewController mapController = ResolveMapController(target);
            if (!mapController) return false;

            if (BobTargetIds.IsMapFull(target.targetID))
            {
                return IsMapStateRequestedAndSettledOrTransitioning(
                    mapController,
                    MapViewController.ViewState.Full_Screen);
            }

            if (BobTargetIds.IsMap(target.targetID))
            {
                return IsMapStateRequestedAndSettledOrTransitioning(
                    mapController,
                    MapViewController.ViewState.Medium_Screen);
            }

            return false;
        }

        private static MapViewController ResolveMapController(BobTarget target)
        {
            if (MapViewController.ActiveInstance)
            {
                return MapViewController.ActiveInstance;
            }

            return target != null && target.targetObject
                ? target.targetObject.GetComponentInParent<MapViewController>(true)
                : null;
        }

        private static bool IsMapStateRequestedAndSettledOrTransitioning(
            MapViewController mapController,
            MapViewController.ViewState state)
        {
            return mapController.currentState == state
                && (mapController.IsTransitioning || mapController.SettledState == state);
        }

        private static bool IsMapStateSettled(
            MapViewController mapController,
            MapViewController.ViewState state)
        {
            return mapController.currentState == state
                && mapController.SettledState == state
                && !mapController.IsTransitioning;
        }

        private BobCommandResult HandleCommandDuringActiveFlight(BobTarget target)
        {
            if (ShouldInterruptForRuntimeMapCommand(target))
            {
                ClearQueuedTarget();
                ExecuteTargetCommand(target, true);
                ProjectLog.Info($"Interrupted current Bob interaction for runtime target: {target.targetID}", this);
                return SetLastCommandResult(BobCommandResult.Interrupted);
            }

            if (!queueLatestCommandWhileFlying)
            {
                CancelActiveFlight(true);
                ExecuteTargetCommand(target);
                return SetLastCommandResult(BobCommandResult.Accepted);
            }

            if (_queuedTarget != null && BobTargetIds.Equals(_queuedTarget.targetID, target.targetID))
            {
                return SetLastCommandResult(BobCommandResult.IgnoredDuplicateQueued);
            }

            _queuedTarget = target;
            ProjectLog.Info($"Queued Bob target until current motion completes: {target.targetID}", this);
            return SetLastCommandResult(BobCommandResult.Queued);
        }

        private bool ShouldInterruptForRuntimeMapCommand(BobTarget target)
        {
            return interruptActiveInteractionForRuntimeMapCommands
                && IsCodeOwnedRuntimeTarget(target);
        }

        private bool IsCommandInProgress()
        {
            return (_flightSequence != null && _flightSequence.IsActive())
                || (_remoteEventTween != null && _remoteEventTween.IsActive())
                || _activeInteractionToken != 0
                || (bob && bob.IsFlying);
        }

        private void RefreshRuntimeSnapshot()
        {
            hasFlightSequence = _flightSequence != null && _flightSequence.IsActive();
            hasRemoteEventDelay = _remoteEventTween != null && _remoteEventTween.IsActive();
            bobReportsFlying = bob && bob.IsFlying;
            bobActiveInHierarchy = bob && bob.gameObject.activeInHierarchy;

            commandPhase = ResolveCommandPhase();
            activeTargetId = string.IsNullOrEmpty(_currentLocID) ? "<none>" : _currentLocID;
            queuedTargetId = _queuedTarget != null ? _queuedTarget.targetID : "<none>";
            activeInteractionTokenDebug = _activeInteractionToken;
        }

        private BobCommandPhase ResolveCommandPhase()
        {
            if (hasRemoteEventDelay) return BobCommandPhase.RemoteDelay;
            if (hasFlightSequence) return BobCommandPhase.FlightSequence;
            if (_activeInteractionToken != 0) return BobCommandPhase.WaitingForFeatureReturn;
            if (bobReportsFlying) return BobCommandPhase.BobLandingRecovery;
            if (_queuedTarget != null) return BobCommandPhase.Queued;
            return BobCommandPhase.Idle;
        }

        private BobCommandResult SetLastCommandResult(BobCommandResult result)
        {
            lastCommandResult = result;
            return result;
        }

        private void CompleteFlight(Sequence completedSequence, bool runQueuedTargetOnComplete)
        {
            if (_flightSequence == completedSequence)
            {
                _flightSequence = null;
            }

            if (runQueuedTargetOnComplete)
            {
                RunQueuedTargetIfAny();
            }
        }

        private void RunQueuedTargetIfAny()
        {
            if (_queuedTarget == null || IsCommandInProgress()) return;

            BobTarget queuedTarget = _queuedTarget;
            ClearQueuedTarget();
            RequestTarget(queuedTarget);
        }

        private void ClearQueuedTarget()
        {
            _queuedTarget = null;
            RefreshRuntimeSnapshot();
        }

        private void CancelActiveFlight(bool preserveBobPosition)
        {
            _remoteEventTween?.Kill();
            _remoteEventTween = null;
            _activeInteractionToken = 0;

            if (_flightSequence != null && _flightSequence.IsActive())
            {
                _flightSequence.Kill(false);
                _flightSequence = null;
            }

            DOTween.Kill(BobFlightTweenId);

            if (bob)
            {
                bob.CancelTransientMotion(preserveBobPosition);
            }
        }
    }
}
