using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Bob.SharedMobility
{
    public class LiquidIconController : MonoBehaviour
    {
        [System.Serializable]
        public struct IconStateData
        {
            public float borderThickness;
            public float contentScale;
            public float flowSpeed;
            public float surfSpeed;
            [Range(0, 1)] public float isActive;
        }

        [Header("References")]
        public Renderer iconRenderer;
        public GameObject iconFakeEyes;

        [Header("Scale")]
        public float visualCorrection = 1.0f;
        public bool isSmallMapIcon = false;

        [Header("Animation")]
        public float burstPeakMultiplier = 1.15f;

        [Header("Interaction")]
        public float stayDuration = 2.0f;
        public float transitionDuration = 0.5f;
        public bool shouldBobReturn = true;

        [Header("States")]
        public IconStateData initialState;
        public IconStateData activeState;
        public IconStateData impactState;
        public IconStateData burstState;

        [Header("Events")]
        public UnityEvent onInteractionComplete;

        [HideInInspector] public bool blockNextResetScale = false;

        private Material _material;
        private Vector3 _originalScale;
        private Tweener _breathingTween;
        private bool _handoffControl;

        private static readonly int ThicknessID = Shader.PropertyToID("_BorderThickness");
        private static readonly int ScaleID = Shader.PropertyToID("_ContentScale");
        private static readonly int FlowSpeedID = Shader.PropertyToID("_FlowSpeed");
        private static readonly int SurfSpeedID = Shader.PropertyToID("_SurfSpeed");
        private static readonly int IsActiveID = Shader.PropertyToID("_IsActive");

        private void Awake()
        {
            if (iconRenderer)
            {
                _material = iconRenderer.material;
            }

            _originalScale = transform.localScale.magnitude > 0.01f
                ? transform.localScale * visualCorrection
                : Vector3.one * visualCorrection;

            transform.localScale = _originalScale;

            if (iconFakeEyes)
            {
                iconFakeEyes.SetActive(false);
            }

            ApplyStateImmediate(initialState);
        }

        public void ForceUpdateOriginalScale(Vector3 externalScale)
        {
            if (isSmallMapIcon) return;

            _originalScale = externalScale * visualCorrection;
        }

        public void PlaySpawnAnimation(float duration)
        {
            if (!_material) return;

            StopAllAnimations();

            _material.SetFloat(ThicknessID, 0.5f);
            _material.SetFloat(FlowSpeedID, 2.0f);

            float targetThickness = initialState.borderThickness > 0f
                ? initialState.borderThickness
                : 0.05f;

            _material.DOFloat(targetThickness, ThicknessID, duration).SetEase(Ease.OutExpo);
            _material.DOFloat(initialState.flowSpeed, FlowSpeedID, duration);

            transform.localScale = Vector3.zero;
            transform.DOScale(_originalScale, duration).SetEase(Ease.OutBack);
        }

        public void PlayEnterSequence(float impactDur = 0.15f, float burstDur = 0.8f)
        {
            blockNextResetScale = true;
            if (_originalScale.magnitude < 0.01f)
            {
                _originalScale = Vector3.one;
            }

            Sequence sequence = DOTween.Sequence();
            sequence.AppendCallback(() => PlayImpact(impactDur));
            sequence.AppendInterval(impactDur);
            sequence.AppendCallback(() => PlayBurst(burstDur));
        }

        public void OnBobEnter()
        {
            PlayEnterSequence(0.2f, 0.6f);
        }

        public void ApplyStateImmediate(IconStateData data)
        {
            if (!_material) return;

            _material.SetFloat(ThicknessID, data.borderThickness);
            _material.SetFloat(ScaleID, data.contentScale);
            _material.SetFloat(FlowSpeedID, data.flowSpeed);
            _material.SetFloat(SurfSpeedID, data.surfSpeed);
            _material.SetFloat(IsActiveID, data.isActive);

            if (!_handoffControl && !blockNextResetScale && _originalScale.magnitude > 0.01f)
            {
                transform.localScale = _originalScale;
            }
        }

        public void StopAllAnimations()
        {
            _breathingTween?.Kill();
            transform.DOKill();

            if (_material)
            {
                _material.DOKill();
            }
        }

        public void ResetState()
        {
            StopAllAnimations();
            _handoffControl = false;

            if (iconFakeEyes)
            {
                iconFakeEyes.SetActive(false);
            }

            ApplyStateImmediate(initialState);

            if (blockNextResetScale)
            {
                blockNextResetScale = false;
                return;
            }

            if (isSmallMapIcon && transform.localScale.magnitude > 0.1f)
            {
                return;
            }

            Vector3 targetScale = _originalScale.magnitude > 0.01f ? _originalScale : Vector3.one;
            transform.DOScale(targetScale, 0.3f);
        }

        private void PlayImpact(float duration)
        {
            StopAllAnimations();
            transform.DOScale(_originalScale * 0.85f, duration).SetEase(Ease.OutQuad);
            ApplyStateTween(impactState, duration);
        }

        private void PlayBurst(float duration)
        {
            blockNextResetScale = false;
            ShowFakeEyes(duration);
            ApplyStateTween(burstState, duration);

            transform
                .DOScale(_originalScale * burstPeakMultiplier, duration * 0.4f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    transform
                        .DOScale(_originalScale, duration * 0.6f)
                        .SetEase(Ease.InOutSine)
                        .OnComplete(CompleteInteraction);
                });
        }

        private void CompleteInteraction()
        {
            StartBreathingLoop();

            bool hasEvent = onInteractionComplete != null
                && onInteractionComplete.GetPersistentEventCount() > 0;

            if (hasEvent)
            {
                _handoffControl = true;
                onInteractionComplete.Invoke();

                if (shouldBobReturn && BobInteractionDirector.Instance)
                {
                    BobInteractionDirector.Instance.ReleaseBobFrom(transform.position);
                }

                return;
            }

            float waitTime = stayDuration > 0f ? stayDuration : 0.5f;
            DOVirtual.DelayedCall(waitTime, OnBobLeave);
        }

        private void OnBobLeave()
        {
            if (_handoffControl) return;

            StopAllAnimations();
            transform.DOScale(_originalScale, transitionDuration).SetEase(Ease.OutQuad);
            ApplyStateTween(initialState, transitionDuration);
            HideFakeEyes();

            DOVirtual.DelayedCall(transitionDuration + 0.1f, () =>
            {
                ApplyStateImmediate(initialState);
            });

            if (shouldBobReturn && BobInteractionDirector.Instance)
            {
                BobInteractionDirector.Instance.ReleaseBobFrom(transform.position);
            }
        }

        private void ShowFakeEyes(float duration)
        {
            if (!iconFakeEyes) return;

            iconFakeEyes.SetActive(true);
            iconFakeEyes.transform.localScale = Vector3.zero;
            iconFakeEyes.transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
        }

        private void HideFakeEyes()
        {
            if (!iconFakeEyes || !iconFakeEyes.activeSelf) return;

            iconFakeEyes.transform.DOKill();
            iconFakeEyes.transform
                .DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => iconFakeEyes.SetActive(false));
        }

        private void ApplyStateTween(IconStateData target, float duration)
        {
            if (!_material) return;

            _material.DOFloat(target.borderThickness, ThicknessID, duration);
            _material.DOFloat(target.contentScale, ScaleID, duration);
            _material.DOFloat(target.isActive, IsActiveID, duration);
            _material.SetFloat(FlowSpeedID, target.flowSpeed);
            _material.SetFloat(SurfSpeedID, target.surfSpeed);
        }

        private void StartBreathingLoop()
        {
            _breathingTween?.Kill();
            _breathingTween = transform
                .DOScale(_originalScale * 1.05f, 1.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }
}
