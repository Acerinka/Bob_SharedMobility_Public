using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Bob.SharedMobility
{
    public partial class BobInteractionDirector : MonoBehaviour
    {
        private const string BobFlightTweenId = "BobInteractionDirector.Flight";

        public static BobInteractionDirector Instance { get; private set; }

        [System.Serializable]
        public class BobTarget
        {
            [Header("Identity")]
            public string targetID;
            public List<string> keywords;
            public KeyCode debugKey = KeyCode.None;

            [Header("Mode")]
            public bool isRemoteTrigger = false;

            [Header("Target")]
            public Transform targetObject;
            public CanvasGroup targetLevel3Panel;

            [Header("Remote Event")]
            public UnityEvent onRemoteEvent;

            [Header("Offsets")]
            public float zOffset = 0f;
            public float yOffset = 0f;
        }

        [Header("References")]
        public BobController bob;

        [Header("Targets")]
        public List<BobTarget> registeredTargets;

        [Header("Flight")]
        public float flyDuration = 0.6f;
        public float appearDuration = 0.3f;
        public float waitTimeBeforeFly = 0f;

        [Header("Diagnostics")]
        public bool enableDebugShortcuts = true;

        [Header("Command Scheduling")]
        [Tooltip("Repeated commands inside this window are ignored to avoid restarting Bob's flight every frame.")]
        public float duplicateCommandCooldown = 0.2f;
        [Tooltip("When Bob is already flying, keep the latest different command and run it after the current flight lands.")]
        public bool queueLatestCommandWhileFlying = true;
        [Tooltip("Let code-owned runtime commands, such as full-map expansion, cancel the active Bob/icon interaction instead of waiting behind it.")]
        public bool interruptActiveInteractionForRuntimeMapCommands = true;
        [Tooltip("Remote Bob targets still own the workspace. Keep this enabled so targets such as full-screen map close dock panels before their remote event fires.")]
        public bool resetSceneBeforeRemoteTriggers = true;
        [Tooltip("Treat an already-open Bob dock-target shortcut as a close/toggle command instead of flying Bob to the same target again.")]
        public bool suppressAlreadyOpenDockTargets = true;
        [Min(0f)]
        [Tooltip("Extra time after Bob's own landing recovery before queued commands are released.")]
        public float landingCompletionBuffer = 0.02f;

        [Header("Runtime Snapshot (Read Only)")]
        [SerializeField] private BobCommandPhase commandPhase = BobCommandPhase.Idle;
        [SerializeField] private BobCommandResult lastCommandResult = BobCommandResult.Accepted;
        [SerializeField] private string activeTargetId = "";
        [SerializeField] private string queuedTargetId = "";
        [SerializeField] private int activeInteractionTokenDebug;
        [SerializeField] private bool hasFlightSequence;
        [SerializeField] private bool hasRemoteEventDelay;
        [SerializeField] private bool bobReportsFlying;
        [SerializeField] private bool bobActiveInHierarchy;

        private string _currentLocID = "";
        private string _lastAcceptedTargetID = "";
        private float _lastAcceptedCommandTime = -999f;
        private Sequence _flightSequence;
        private Tween _remoteEventTween;
        private BobTarget _queuedTarget;
        private int _nextInteractionToken = 1;
        private int _activeInteractionToken;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                ProjectLog.Warning("Multiple BobInteractionDirector instances detected; the latest instance will be used.", this);
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            RefreshRuntimeSnapshot();

            if (!enableDebugShortcuts) return;

            HandleDebugTargetShortcuts();

            if (ProjectInput.WasKeyPressed(KeyCode.R))
            {
                ResetAll();
            }

            RefreshRuntimeSnapshot();
        }

        public void ProcessVoiceCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            BobTarget target = FindVoiceTarget(text);
            if (target != null)
            {
                BobCommandResult result = RequestTarget(target);
                ProjectLog.Info($"Voice command matched target '{target.targetID}' with result: {result}", this);

                return;
            }

            if (ContainsCommand(text, "reset") || ContainsCommand(text, "cancel"))
            {
                ResetAll();
            }
        }

        public bool GoToTarget(BobTarget target)
        {
            return RequestTarget(target).WasExecutedImmediately();
        }

        public BobCommandResult RequestTarget(BobTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.targetID))
            {
                return SetLastCommandResult(BobCommandResult.IgnoredInvalidTarget);
            }

            if (TryToggleAlreadyOpenDockTarget(target, out BobCommandResult dockToggleResult))
            {
                return SetLastCommandResult(dockToggleResult);
            }

            if (ShouldIgnoreAlreadyOpenMapTarget(target))
            {
                return SetLastCommandResult(BobCommandResult.IgnoredAlreadyOpen);
            }

            if (ShouldIgnoreDuplicateCommand(target))
            {
                return SetLastCommandResult(BobCommandResult.IgnoredDuplicate);
            }

            if (IsCommandInProgress())
            {
                return HandleCommandDuringActiveFlight(target);
            }

            ClearQueuedTarget();
            ExecuteTargetCommand(target);
            return SetLastCommandResult(BobCommandResult.Accepted);
        }

        private void ExecuteTargetCommand(BobTarget target, bool preserveBobPositionOnReset = false)
        {
            _currentLocID = target.targetID;
            _lastAcceptedTargetID = target.targetID;
            _lastAcceptedCommandTime = Time.unscaledTime;

            if (TryExecuteRuntimeMapCommand(target, preserveBobPositionOnReset))
            {
                return;
            }

            if (target.isRemoteTrigger)
            {
                if (resetSceneBeforeRemoteTriggers)
                {
                    SilentResetEverything(target.targetID, preserveBobPositionOnReset);
                }

                TriggerRemoteTarget(target);
                return;
            }

            SilentResetEverything(target.targetID, true);

            if (target.targetObject == null)
            {
                ProjectLog.Warning($"Bob target '{target.targetID}' has no targetObject assigned.", this);
                _currentLocID = "";
                return;
            }

            LiquidIconController iconController = target.targetObject.GetComponent<LiquidIconController>();
            DockButtonController dockButton = target.targetObject.GetComponent<DockButtonController>();
            iconController?.CancelBobInteraction();
            dockButton?.CancelBobInteraction();

            Vector3 destination = ResolveDestination(target, dockButton);
            bool shouldVanish = iconController != null || dockButton != null;
            bool waitsForReturn = shouldVanish && ShouldWaitForBobReturn(iconController, dockButton);

            FlyBobSequence(destination, () =>
            {
                int interactionToken = waitsForReturn ? BeginTargetInteraction(target.targetID) : 0;

                if (iconController != null)
                {
                    iconController.gameObject.SetActive(true);
                    iconController.OnBobEnter(interactionToken);
                }
                else if (dockButton != null)
                {
                    dockButton.ActivateByBob(target.targetLevel3Panel, interactionToken);
                }
            }, shouldVanish, !waitsForReturn);
        }

        public void ReleaseBobFrom(Vector3 startPos)
        {
            ReleaseBobFrom(startPos, 0);
        }

        public void ReleaseBobFrom(Vector3 startPos, int interactionToken)
        {
            if (!bob) return;
            if (!CanReleaseBob(interactionToken))
            {
                ProjectLog.Info(
                    $"Ignored stale Bob release request: token={interactionToken}; active={_activeInteractionToken}",
                    this);
                return;
            }

            _activeInteractionToken = 0;
            _currentLocID = "";
            bob.transform.position = startPos;
            bob.lastVanishedPos = startPos;
            FlyBobSequence(bob.InitialPos, null, false, true);
        }

        public void ResetAll()
        {
            ClearQueuedTarget();
            ClearCurrentCommandLock();
            SilentResetEverything();
            if (bob) bob.ResetState();
        }

    }
}
