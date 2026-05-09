using System.Collections.Generic;
using UnityEngine;

namespace Bob.SharedMobility
{
    public sealed partial class RuntimeDiagnosticsHub
    {
        [ContextMenu("Resolve Scene References")]
        public void ResolveReferences()
        {
            navigationService = ResolveSceneReference(navigationService);
            mapController = ResolveSceneReference(mapController);
            bobInteractionDirector = ResolveSceneReference(bobInteractionDirector);
            bobController = ResolveSceneReference(bobController);
            laneAssistScenario = ResolveSceneReference(laneAssistScenario);
            voiceCommandRecognizer = ResolveSceneReference(voiceCommandRecognizer);
            gamepadInputDebugger = ResolveSceneReference(gamepadInputDebugger);
        }

        [ContextMenu("Backdoor/Log Registry")]
        public void LogBackdoorRegistry()
        {
            if (backdoorControls == null)
            {
                ProjectLog.Info("Backdoor registry is empty.", this);
                return;
            }

            foreach (BackdoorControl backdoor in backdoorControls)
            {
                if (backdoor == null) continue;

                string enabledText = backdoor.enabledInDevelopment ? "enabled in Development" : "disabled by default";
                ProjectLog.Info(
                    $"{backdoor.kind}: {backdoor.label} ({enabledText}); owner={backdoor.owner}; key={backdoor.keyboardShortcut}; gamepad={backdoor.gamepadShortcut}",
                    this);
            }

            if (bobInteractionDirector)
            {
                bobInteractionDirector.LogRegisteredTargets();
            }
        }

        [ContextMenu("Diagnostics/Validate Runtime Wiring")]
        public void ValidateRuntimeWiring()
        {
            ResolveReferences();
            ValidateBackdoorRegistry();

            WarnIfMissing("AppNavigationService", navigationService);
            WarnIfMissing("MapViewController", mapController);
            WarnIfMissing("BobInteractionDirector", bobInteractionDirector);
            WarnIfMissing("BobController", bobController);
            WarnIfMissing("VoiceCommandRecognizer", voiceCommandRecognizer);

            if (bobInteractionDirector)
            {
                bobInteractionDirector.ValidateRegisteredTargets();
            }

            if (mapController)
            {
                mapController.ValidateRuntimeState();
            }
        }

        [ContextMenu("Diagnostics/Log Runtime State")]
        public void LogRuntimeState()
        {
            ResolveReferences();

            if (navigationService)
            {
                ProjectLog.Info(
                    $"Navigation state: screen={navigationService.CurrentScreen}; layer={navigationService.CurrentLayer}; modal={navigationService.CurrentModal}; dock={FormatObjectName(navigationService.CurrentDockPanel)}; subPanel={FormatObjectName(navigationService.CurrentSubPanel)}; blocksWorldInput={navigationService.BlocksWorldInput}",
                    this);
            }

            if (mapController)
            {
                ProjectLog.Info(
                    $"Map state: requested={mapController.currentState}; settled={mapController.SettledState}; transitioning={mapController.IsTransitioning}; visible={mapController.VisibleSurfaceDebug}; queued={mapController.QueuedStateDebug}",
                    this);
            }

            if (bobInteractionDirector)
            {
                bobInteractionDirector.LogCommandState();
            }
        }

        private void ValidateBackdoorRegistry()
        {
            if (backdoorControls == null || backdoorControls.Count == 0)
            {
                ProjectLog.Warning("RuntimeDiagnosticsHub backdoor registry is empty.", this);
                return;
            }

            HashSet<BackdoorKind> registeredKinds = new HashSet<BackdoorKind>();

            foreach (BackdoorControl backdoor in backdoorControls)
            {
                if (backdoor == null)
                {
                    ProjectLog.Warning("RuntimeDiagnosticsHub backdoor registry contains a null entry.", this);
                    continue;
                }

                if (!registeredKinds.Add(backdoor.kind))
                {
                    ProjectLog.Warning($"Backdoor kind '{backdoor.kind}' is registered more than once.", this);
                }

                if (string.IsNullOrWhiteSpace(backdoor.label))
                {
                    ProjectLog.Warning($"Backdoor kind '{backdoor.kind}' has no label.", this);
                }

                if (string.IsNullOrWhiteSpace(backdoor.owner))
                {
                    ProjectLog.Warning($"Backdoor kind '{backdoor.kind}' has no owner.", this);
                }
            }
        }

        private void WarnIfMissing(string label, UnityEngine.Object sceneReference)
        {
            if (!sceneReference)
            {
                ProjectLog.Warning($"{label} reference is missing from RuntimeDiagnosticsHub.", this);
            }
        }

        private static string FormatObjectName(UnityEngine.Object target)
        {
            return target ? target.name : "<none>";
        }

        private static T ResolveSceneReference<T>(T current) where T : UnityEngine.Object
        {
            return current != null ? current : FindObjectOfType<T>(true);
        }
    }
}
