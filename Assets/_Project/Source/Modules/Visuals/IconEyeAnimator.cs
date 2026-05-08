using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public class IconEyeAnimator : MonoBehaviour
    {
        [Header("Eyes")]
        public Transform leftEye;
        public Transform rightEye;

        [Header("Blink")]
        public float minBlinkInterval = 2.0f;
        public float maxBlinkInterval = 5.0f;
        public float blinkDuration = 0.15f;

        [Header("Look Around")]
        public float lookRadius = 0.03f;
        public float moveDuration = 0.2f;
        public float minLookInterval = 0.5f;
        public float maxLookInterval = 2.5f;
        [Range(0, 1)] public float centerChance = 0.4f;

        private float _nextBlinkTime;
        private float _nextLookTime;
        private Vector3 _leftEyeStartPos;
        private Vector3 _rightEyeStartPos;
        private Vector3 _leftEyeInitScale;
        private Vector3 _rightEyeInitScale;

        private void Awake()
        {
            CacheInitialState();
        }

        private void OnEnable()
        {
            RestoreEyes();
            ResetBlinkTimer();
            ResetLookTimer();
        }

        private void OnDisable()
        {
            leftEye?.DOKill();
            rightEye?.DOKill();
        }

        private void Update()
        {
            HandleBlinking();
            HandleRandomLook();
        }

        private void CacheInitialState()
        {
            if (leftEye)
            {
                _leftEyeStartPos = leftEye.localPosition;
                _leftEyeInitScale = leftEye.localScale;
            }

            if (rightEye)
            {
                _rightEyeStartPos = rightEye.localPosition;
                _rightEyeInitScale = rightEye.localScale;
            }
        }

        private void RestoreEyes()
        {
            if (leftEye) leftEye.localScale = _leftEyeInitScale;
            if (rightEye) rightEye.localScale = _rightEyeInitScale;
        }

        private void HandleBlinking()
        {
            if (Time.time < _nextBlinkTime) return;

            Blink();
            ResetBlinkTimer();
        }

        private void Blink()
        {
            BlinkEye(leftEye, _leftEyeInitScale);
            BlinkEye(rightEye, _rightEyeInitScale);
        }

        private void BlinkEye(Transform eye, Vector3 initialScale)
        {
            if (!eye) return;

            float targetY = initialScale.y * 0.1f;
            eye.DOScaleY(targetY, blinkDuration)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => eye.localScale = initialScale);
        }

        private void ResetBlinkTimer()
        {
            _nextBlinkTime = Time.time + Random.Range(minBlinkInterval, maxBlinkInterval);
        }

        private void HandleRandomLook()
        {
            if (Time.time < _nextLookTime) return;

            PickNewLookTarget();
            ResetLookTimer();
        }

        private void PickNewLookTarget()
        {
            if (!leftEye || !rightEye) return;

            Vector3 targetOffset = Vector3.zero;
            if (Random.value >= centerChance)
            {
                Vector2 randomOffset = Random.insideUnitCircle * lookRadius;
                targetOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
            }

            leftEye.DOLocalMove(_leftEyeStartPos + targetOffset, moveDuration).SetEase(Ease.OutQuad);
            rightEye.DOLocalMove(_rightEyeStartPos + targetOffset, moveDuration).SetEase(Ease.OutQuad);
        }

        private void ResetLookTimer()
        {
            _nextLookTime = Time.time + Random.Range(minLookInterval, maxLookInterval);
        }
    }
}
