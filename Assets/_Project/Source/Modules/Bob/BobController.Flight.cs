using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class BobController
    {
        public void PrepareForFlight()
        {
            KillMotionTweens();

            _isFlying = true;
            _isAvoiding = false;
            _isInReminderMode = false;
            floatSpeed = _defaultFloatSpeed;
            floatAmplitude = _defaultFloatAmplitude;
            _burstYOffset = 0f;

            transform.position = lastVanishedPos;
            transform.localScale = Vector3.zero;
            transform.localRotation = _initialRot;
            SetBlendShapeWeight(0f);
            bodyOnlyVolume = 0;
            gameObject.SetActive(true);
        }

        public Tween AppearAnim(float duration)
        {
            return transform.DOScale(_initialScale, duration).SetEase(Ease.OutBack);
        }

        public void StartFlyingShape()
        {
            DOTween.Kill(FlightShapeTweenId);

            ForEachActiveBodyMesh(mesh =>
            {
                DOTween.To(
                    () => mesh.GetBlendShapeWeight(stretchBlendShapeIndex),
                    x => mesh.SetBlendShapeWeight(stretchBlendShapeIndex, x),
                    100f,
                    stretchDuration).SetId(FlightShapeTweenId);
            });
        }

        public void StartReturnShape(float totalFlightTime)
        {
            DOTween.Kill(FlightShapeTweenId);

            ForEachActiveBodyMesh(mesh =>
            {
                Sequence sequence = DOTween.Sequence();
                sequence.SetId(FlightShapeTweenId);
                sequence.Append(DOTween.To(
                    () => mesh.GetBlendShapeWeight(stretchBlendShapeIndex),
                    x => mesh.SetBlendShapeWeight(stretchBlendShapeIndex, x),
                    100f,
                    totalFlightTime * 0.4f).SetEase(Ease.OutQuad));
                sequence.Append(DOTween.To(
                    () => mesh.GetBlendShapeWeight(stretchBlendShapeIndex),
                    x => mesh.SetBlendShapeWeight(stretchBlendShapeIndex, x),
                    0f,
                    totalFlightTime * 0.6f).SetEase(Ease.InQuad));
            });
        }

        public void ArriveAndVanish(Vector3 vanishPos)
        {
            lastVanishedPos = vanishPos;
            _isFlying = false;
            gameObject.SetActive(false);
            transform.localScale = _initialScale;
            SetBlendShapeWeight(0);
        }

        public void ArriveAndStay(Vector3 stayPos)
        {
            lastVanishedPos = stayPos;
            transform.position = stayPos;
            gameObject.SetActive(true);
            transform.localScale = _initialScale;
            SetBlendShapeWeight(0);

            Sequence landSeq = DOTween.Sequence();
            Vector3 squashScale = new Vector3(
                _initialScale.x * landSquashXZ,
                _initialScale.y * landSquashY,
                _initialScale.z * landSquashXZ);

            landSeq.Append(transform.DOScale(squashScale, landSquashTime).SetEase(Ease.OutQuad));
            landSeq.Append(transform.DOScale(_initialScale, landRecoverTime).SetEase(Ease.OutElastic));

            if (landSlideDist > 0)
            {
                Vector3 forwardDir = transform.forward;
                Sequence slideSeq = DOTween.Sequence();
                slideSeq.Append(transform.DOMove(stayPos + forwardDir * landSlideDist, landRecoverTime * 0.3f).SetEase(Ease.OutQuad));
                slideSeq.Append(transform.DOMove(stayPos, landRecoverTime * 0.7f).SetEase(Ease.OutBack));
                landSeq.Join(slideSeq);
            }

            if (landEnergyPulse > 0)
            {
                bodyOnlyVolume = 0;

                Sequence lightSeq = DOTween.Sequence();
                lightSeq.Append(DOTween.To(() => bodyOnlyVolume, x => bodyOnlyVolume = x, landEnergyPulse, landSquashTime));
                lightSeq.Append(DOTween.To(() => bodyOnlyVolume, x => bodyOnlyVolume = x, 0f, landRecoverTime).SetEase(Ease.OutSine));
                landSeq.Join(lightSeq);
            }

            landSeq.OnComplete(() => _isFlying = false);
        }

        public void DodgeDown()
        {
            if (_isFlying) return;

            _restoreTimer?.Kill();
            _restoreTimer = null;

            if (_isAvoiding) return;

            _isAvoiding = true;
            transform.DOKill();
            transform.DOLocalMoveY(_initialPos.y - avoidDistance, moveDuration).SetEase(Ease.OutBack);
        }

        public void ReturnToIdleDelayed()
        {
            if (_isFlying) return;

            _restoreTimer?.Kill();
            _restoreTimer = DOVirtual.DelayedCall(restoreDelay, () =>
            {
                _isAvoiding = true;
                transform.DOKill();
                transform
                    .DOLocalMove(_initialPos, moveDuration)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        lastVanishedPos = HomeWorldPosition;
                        _isAvoiding = false;
                        _restoreTimer = null;
                    });
            });
        }

        public void CancelTransientMotion(bool preserveCurrentWorldPosition)
        {
            CancelTransientMotion(preserveCurrentWorldPosition, preserveCurrentWorldPosition);
        }

        public void CancelTransientMotion(bool preserveCurrentWorldPosition, bool updateMotionAnchor)
        {
            KillMotionTweens();

            if (preserveCurrentWorldPosition && updateMotionAnchor && gameObject.activeInHierarchy)
            {
                lastVanishedPos = transform.position;
            }

            _isFlying = false;
            _isAvoiding = false;
            _isInReminderMode = false;
            _burstYOffset = 0f;
            floatSpeed = _defaultFloatSpeed > 0 ? _defaultFloatSpeed : floatSpeed;
            floatAmplitude = _defaultFloatAmplitude > 0 ? _defaultFloatAmplitude : floatAmplitude;
            SetBlendShapeWeight(0);
        }

        public void RestoreHomeAnchor(bool moveVisibleBob)
        {
            lastVanishedPos = HomeWorldPosition;

            if (!moveVisibleBob || !gameObject.activeInHierarchy) return;

            transform.position = lastVanishedPos;
            transform.localRotation = _initialRot;
        }

        public void PrepareForRemoteInteractionAtHome()
        {
            KillMotionTweens();

            _isFlying = false;
            _isAvoiding = false;
            _isInReminderMode = false;
            _burstYOffset = 0f;
            floatSpeed = _defaultFloatSpeed > 0 ? _defaultFloatSpeed : floatSpeed;
            floatAmplitude = _defaultFloatAmplitude > 0 ? _defaultFloatAmplitude : floatAmplitude;

            lastVanishedPos = HomeWorldPosition;
            transform.position = lastVanishedPos;
            transform.localRotation = _initialRot;
            transform.localScale = _initialScale;
            gameObject.SetActive(true);

            if (lightBulbIcon)
            {
                lightBulbIcon.SetActive(false);
            }

            ChangeSkin(currentSkinIndex);
            SetBlendShapeWeight(0);
        }
    }
}
