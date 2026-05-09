using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Bob.SharedMobility
{
    public class BobInteractionDirector : MonoBehaviour
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
            if (!enableDebugShortcuts) return;

            HandleDebugTargetShortcuts();

            if (ProjectInput.WasKeyPressed(KeyCode.R))
            {
                ResetAll();
            }
        }

        public void ProcessVoiceCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            BobTarget target = FindVoiceTarget(text);
            if (target != null)
            {
                if (GoToTarget(target))
                {
                    ProjectLog.Info($"Voice command matched target: {target.targetID}", this);
                }

                return;
            }

            if (text.Contains("reset") || text.Contains("cancel"))
            {
                ResetAll();
            }
        }

        public bool GoToTarget(BobTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.targetID)) return false;
            if (ShouldIgnoreDuplicateCommand(target)) return false;

            if (IsCommandInProgress())
            {
                return HandleCommandDuringActiveFlight(target);
            }

            ExecuteTargetCommand(target);
            return true;
        }

        private void ExecuteTargetCommand(BobTarget target)
        {
            _currentLocID = target.targetID;
            _lastAcceptedTargetID = target.targetID;
            _lastAcceptedCommandTime = Time.unscaledTime;

            if (target.isRemoteTrigger)
            {
                TriggerRemoteTarget(target);
                return;
            }

            SilentResetEverything(target.targetID);

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
            if (_activeInteractionToken != 0 && interactionToken != _activeInteractionToken) return;

            _activeInteractionToken = 0;
            _currentLocID = "";
            bob.transform.position = startPos;
            bob.lastVanishedPos = startPos;
            FlyBobSequence(bob.InitialPos, null, false, true);
        }

        public void ResetAll()
        {
            SilentResetEverything();
            if (bob) bob.ResetState();
        }

        private void HandleDebugTargetShortcuts()
        {
            if (registeredTargets == null) return;

            foreach (BobTarget target in registeredTargets)
            {
                if (target == null || target.debugKey == KeyCode.None) continue;

                if (ProjectInput.WasKeyPressed(target.debugKey))
                {
                    if (TryHandleDebugBackdoor(target))
                    {
                        ProjectLog.Info($"Debug shortcut selected target: {target.targetID}", this);
                        continue;
                    }

                    if (GoToTarget(target))
                    {
                        ProjectLog.Info($"Debug shortcut selected target: {target.targetID}", this);
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
                    if (!string.IsNullOrEmpty(keyword) && text.Contains(keyword))
                    {
                        return target;
                    }
                }
            }

            return null;
        }

        private bool TryHandleDebugBackdoor(BobTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.targetID)) return false;

            if (target.targetID == "Mapfull" && MapViewController.ActiveInstance)
            {
                MapViewController activeMapController = MapViewController.ActiveInstance;

                if (activeMapController.currentState == MapViewController.ViewState.Full_Screen)
                {
                    activeMapController.ToggleFullView();
                    ClearCurrentCommandLock();
                    return true;
                }

                return false;
            }

            if (target.targetObject == null) return false;

            MapViewController mapController = target.targetObject.GetComponentInParent<MapViewController>(true);
            if (!mapController) return false;

            if (target.targetID == "Map" || target.targetID == "map")
            {
                if (mapController.currentState == MapViewController.ViewState.Small_Icon)
                {
                    return false;
                }

                mapController.ToggleMediumView();
                ClearCurrentCommandLock();
                return true;
            }

            return false;
        }

        private void ClearCurrentCommandLock()
        {
            _currentLocID = "";
        }

        private void TriggerRemoteTarget(BobTarget target)
        {
            float recommendedDelay = bob ? bob.PlayRemoteInteraction() : 0.5f;

            _remoteEventTween?.Kill();
            _remoteEventTween = DOVirtual.DelayedCall(recommendedDelay, () =>
            {
                target.onRemoteEvent?.Invoke();
                ClearCurrentCommandLock();
                _remoteEventTween = null;
                RunQueuedTargetIfAny();
            }).SetId(BobFlightTweenId);
        }

        private void SilentResetEverything(string ignoreTargetID = null)
        {
            CancelActiveFlight(true);

            if (DockNavigationManager.Instance)
            {
                DockNavigationManager.Instance.CloseCurrentApp();
            }

            if (registeredTargets != null)
            {
                foreach (BobTarget target in registeredTargets)
                {
                    if (target == null) continue;
                    if (ignoreTargetID != null && target.targetID == ignoreTargetID) continue;
                    if (target.targetObject == null) continue;

                    target.targetObject.GetComponent<LiquidIconController>()?.ResetState();
                    target.targetObject.GetComponent<DockButtonController>()?.ResetState();
                }
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

        private bool ShouldIgnoreDuplicateCommand(BobTarget target)
        {
            if (_currentLocID == target.targetID) return true;

            bool insideCooldown = Time.unscaledTime - _lastAcceptedCommandTime < duplicateCommandCooldown;
            return insideCooldown && _lastAcceptedTargetID == target.targetID;
        }

        private bool HandleCommandDuringActiveFlight(BobTarget target)
        {
            if (!queueLatestCommandWhileFlying)
            {
                CancelActiveFlight(true);
                ExecuteTargetCommand(target);
                return true;
            }

            if (_queuedTarget != null && _queuedTarget.targetID == target.targetID)
            {
                return false;
            }

            _queuedTarget = target;
            ProjectLog.Info($"Queued Bob target until current motion completes: {target.targetID}", this);
            return false;
        }

        private bool IsCommandInProgress()
        {
            return (_flightSequence != null && _flightSequence.IsActive())
                || (_remoteEventTween != null && _remoteEventTween.IsActive())
                || _activeInteractionToken != 0;
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
            _queuedTarget = null;
            GoToTarget(queuedTarget);
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
