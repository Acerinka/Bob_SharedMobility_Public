using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Bob.SharedMobility
{
    public class BobInteractionDirector : MonoBehaviour
    {
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

        private string _currentLocID = "";

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
                ProjectLog.Info($"Voice command matched target: {target.targetID}", this);
                GoToTarget(target);
                return;
            }

            if (text.Contains("reset") || text.Contains("cancel"))
            {
                ResetAll();
            }
        }

        public void GoToTarget(BobTarget target)
        {
            if (target == null || string.IsNullOrEmpty(target.targetID)) return;
            if (_currentLocID == target.targetID) return;

            _currentLocID = target.targetID;

            if (target.isRemoteTrigger)
            {
                TriggerRemoteTarget(target);
                return;
            }

            SilentResetEverything(target.targetID);

            if (target.targetObject == null) return;

            LiquidIconController iconController = target.targetObject.GetComponent<LiquidIconController>();
            DockButtonController dockButton = target.targetObject.GetComponent<DockButtonController>();

            Vector3 destination = ResolveDestination(target, dockButton);
            bool shouldVanish = iconController != null || dockButton != null;

            FlyBobSequence(destination, () =>
            {
                if (iconController != null)
                {
                    iconController.gameObject.SetActive(true);
                    iconController.OnBobEnter();
                }
                else if (dockButton != null)
                {
                    dockButton.ActivateByBob(target.targetLevel3Panel);
                }
            }, shouldVanish);
        }

        public void ReleaseBobFrom(Vector3 startPos)
        {
            if (!bob) return;

            _currentLocID = "";
            bob.transform.position = startPos;
            FlyBobSequence(bob.InitialPos, null, false);
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
                    ProjectLog.Info($"Debug shortcut selected target: {target.targetID}", this);
                    GoToTarget(target);
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

        private void TriggerRemoteTarget(BobTarget target)
        {
            float recommendedDelay = bob ? bob.PlayRemoteInteraction() : 0.5f;

            DOVirtual.DelayedCall(recommendedDelay, () =>
            {
                target.onRemoteEvent?.Invoke();
            });
        }

        private void SilentResetEverything(string ignoreTargetID = null)
        {
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

            if (bob) bob.transform.DOKill();
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
            if (!bob) return;

            Sequence sequence = DOTween.Sequence();
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
        }
    }
}
