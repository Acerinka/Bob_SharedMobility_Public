using System.Collections.Generic;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class BobInteractionDirector
    {
        [ContextMenu("Diagnostics/Log Registered Targets")]
        public void LogRegisteredTargets()
        {
            if (registeredTargets == null || registeredTargets.Count == 0)
            {
                ProjectLog.Info("Bob target registry is empty.", this);
                return;
            }

            foreach (BobTarget target in registeredTargets)
            {
                if (target == null) continue;

                string mode = target.isRemoteTrigger ? "remote" : "direct";
                string objectName = target.targetObject ? target.targetObject.name : "<none>";
                ProjectLog.Info(
                    $"Bob target '{target.targetID}': key={target.debugKey}; mode={mode}; target={objectName}",
                    this);
            }
        }

        [ContextMenu("Diagnostics/Validate Registered Targets")]
        public void ValidateRegisteredTargets()
        {
            if (registeredTargets == null || registeredTargets.Count == 0)
            {
                ProjectLog.Warning("BobInteractionDirector has no registered targets.", this);
                return;
            }

            HashSet<string> targetIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            HashSet<KeyCode> debugKeys = new HashSet<KeyCode>();

            foreach (BobTarget target in registeredTargets)
            {
                if (target == null)
                {
                    ProjectLog.Warning("Bob target registry contains a null entry.", this);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(target.targetID))
                {
                    ProjectLog.Warning("Bob target registry contains an entry with no targetID.", this);
                }
                else if (!targetIds.Add(target.targetID))
                {
                    ProjectLog.Warning($"Bob target registry contains duplicate targetID '{target.targetID}'.", this);
                }

                if (!target.isRemoteTrigger && target.targetObject == null)
                {
                    ProjectLog.Warning($"Bob target '{target.targetID}' is direct but has no targetObject.", this);
                }

                if (target.isRemoteTrigger
                    && !IsCodeOwnedRuntimeTarget(target)
                    && (target.onRemoteEvent == null || target.onRemoteEvent.GetPersistentEventCount() == 0))
                {
                    ProjectLog.Warning($"Bob target '{target.targetID}' is remote but has no persistent remote event.", this);
                }

                if (target.debugKey == KeyCode.None) continue;

                if (!debugKeys.Add(target.debugKey))
                {
                    ProjectLog.Warning($"Bob target shortcut '{target.debugKey}' is assigned more than once.", this);
                }
            }
        }

        [ContextMenu("Diagnostics/Log Command State")]
        public void LogCommandState()
        {
            RefreshRuntimeSnapshot();
            ProjectLog.Info(
                $"Bob command state: phase={commandPhase}; lastResult={lastCommandResult}; active={activeTargetId}; queued={queuedTargetId}; token={activeInteractionTokenDebug}; flight={hasFlightSequence}; remoteDelay={hasRemoteEventDelay}; bobFlying={bobReportsFlying}; bobActive={bobActiveInHierarchy}",
                this);
        }

        private void HandleDebugTargetShortcuts()
        {
            if (registeredTargets == null) return;

            foreach (BobTarget target in registeredTargets)
            {
                if (target == null || target.debugKey == KeyCode.None) continue;

                if (ProjectInput.WasKeyPressed(target.debugKey))
                {
                    if (TryHandleDebugBackdoor(target, out BobCommandResult backdoorResult))
                    {
                        ProjectLog.Info($"Debug shortcut selected target '{target.targetID}' with result: {backdoorResult}", this);
                        continue;
                    }

                    BobCommandResult result = RequestTarget(target);
                    if (result.WasAccepted())
                    {
                        ProjectLog.Info($"Debug shortcut selected target '{target.targetID}' with result: {result}", this);
                    }
                }
            }
        }

        private BobTarget FindVoiceTarget(string text)
        {
            if (registeredTargets == null) return null;

            foreach (BobTarget target in registeredTargets)
            {
                if (target == null || target.keywords == null) continue;

                foreach (string keyword in target.keywords)
                {
                    if (ContainsCommand(text, keyword))
                    {
                        return target;
                    }
                }
            }

            return null;
        }

        private static bool ContainsCommand(string text, string command)
        {
            return !string.IsNullOrEmpty(command)
                && text.IndexOf(command, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TryHandleDebugBackdoor(BobTarget target, out BobCommandResult result)
        {
            result = BobCommandResult.IgnoredInvalidTarget;

            if (target == null || string.IsNullOrEmpty(target.targetID)) return false;
            if (IsCommandInProgress()) return false;

            ClearQueuedTarget();

            if (BobTargetIds.IsMapFull(target.targetID) && MapViewController.ActiveInstance)
            {
                MapViewController activeMapController = MapViewController.ActiveInstance;

                if (IsMapStateSettled(
                    activeMapController,
                    MapViewController.ViewState.Full_Screen))
                {
                    SilentResetEverything(target.targetID, false);
                    activeMapController.ToggleFullView();
                    ClearCurrentCommandLock();
                    result = SetLastCommandResult(BobCommandResult.Accepted);
                    return true;
                }

                return false;
            }

            if (target.targetObject == null) return false;

            MapViewController mapController = target.targetObject.GetComponentInParent<MapViewController>(true);
            if (!mapController) return false;

            if (BobTargetIds.IsMap(target.targetID))
            {
                if (mapController.currentState == MapViewController.ViewState.Small_Icon)
                {
                    return false;
                }

                if (IsMapStateSettled(
                    mapController,
                    MapViewController.ViewState.Medium_Screen))
                {
                    SilentResetEverything(target.targetID, false);
                    mapController.ToggleMediumView();
                    ClearCurrentCommandLock();
                    result = SetLastCommandResult(BobCommandResult.Accepted);
                    return true;
                }

                SilentResetEverything(target.targetID, false);
                mapController.TriggerMediumView();
                ClearCurrentCommandLock();
                result = SetLastCommandResult(BobCommandResult.Accepted);
                return true;
            }

            return false;
        }
    }
}
