using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class BobController
    {
        public void TriggerAction(BobActionType action)
        {
            DOTween.Kill(BobActionTweenId);

            switch (action)
            {
                case BobActionType.JumpHigh:
                    Sequence jumpSeq = DOTween.Sequence();
                    jumpSeq.SetId(BobActionTweenId);
                    jumpSeq.Append(DOTween.To(
                        () => _burstYOffset,
                        x => _burstYOffset = x,
                        actionSettings.jumpHeight,
                        actionSettings.jumpUpTime).SetEase(Ease.OutQuad));
                    jumpSeq.Append(DOTween.To(
                        () => _burstYOffset,
                        x => _burstYOffset = x,
                        0f,
                        actionSettings.jumpDownTime).SetEase(Ease.OutBounce));
                    break;

                case BobActionType.PulseLight:
                    Sequence pulseSeq = DOTween.Sequence();
                    pulseSeq.SetId(BobActionTweenId);
                    pulseSeq.Append(DOTween.To(
                        () => bodyOnlyVolume,
                        x => bodyOnlyVolume = x,
                        actionSettings.pulseIntensity,
                        actionSettings.pulseInTime));
                    pulseSeq.Append(DOTween.To(
                        () => bodyOnlyVolume,
                        x => bodyOnlyVolume = x,
                        0f,
                        actionSettings.pulseOutTime));
                    break;

                case BobActionType.SpinFast:
                    transform
                        .DORotate(new Vector3(0, 360, 0), actionSettings.spinDuration, RotateMode.LocalAxisAdd)
                        .SetEase(Ease.InOutBack)
                        .SetId(BobActionTweenId);
                    break;
            }
        }

        public void EnterReminderState()
        {
            DOTween.Kill(BobActionTweenId);

            _isInReminderMode = true;
            _isFlying = false;
            _burstYOffset = 0f;

            ChangeSkin(reminderSkinIndex);

            if (lightBulbIcon)
            {
                lightBulbIcon.SetActive(true);
                lightBulbIcon.transform.localScale = Vector3.zero;
                lightBulbIcon.transform
                    .DOScale(Vector3.one, reminderAnim.totalDuration * 0.5f)
                    .SetEase(Ease.OutBack)
                    .SetId(BobActionTweenId);
            }

            float upTime = reminderAnim.totalDuration * 0.4f;
            float downTime = reminderAnim.totalDuration * 0.6f;

            Sequence seq = DOTween.Sequence();
            seq.SetId(BobActionTweenId);
            seq.Append(DOTween.To(
                () => _burstYOffset,
                x => _burstYOffset = x,
                reminderAnim.jumpHeight,
                upTime).SetEase(Ease.OutQuad));
            seq.Join(transform
                .DOLocalRotate(new Vector3(0, reminderAnim.rotationAngle, 0), reminderAnim.totalDuration, RotateMode.LocalAxisAdd)
                .SetEase(reminderAnim.rotationEase));
            seq.Append(DOTween.To(
                () => _burstYOffset,
                x => _burstYOffset = x,
                0f,
                downTime).SetEase(Ease.OutBounce));
        }

        public void ExitReminderState()
        {
            if (lightBulbIcon)
            {
                lightBulbIcon.transform
                    .DOScale(0, 0.2f)
                    .SetId(BobActionTweenId)
                    .OnComplete(() => lightBulbIcon.SetActive(false));
            }

            ChangeSkin(0);
            _isInReminderMode = false;
        }

        public float PlayRemoteInteraction()
        {
            if (_isFlying) return 0f;

            DOTween.Kill(RemoteFloatTweenId);

            float originalBodyVol = bodyOnlyVolume;

            DOTween.To(() => bodyOnlyVolume, x => bodyOnlyVolume = x, 1.0f, remoteState.duration * 0.3f)
                .SetId(RemoteFloatTweenId)
                .OnComplete(() =>
                {
                    DOTween.To(
                        () => bodyOnlyVolume,
                        x => bodyOnlyVolume = x,
                        originalBodyVol,
                        remoteState.duration * 0.7f).SetId(RemoteFloatTweenId);
                });

            DOTween.To(() => floatSpeed, x => floatSpeed = x, remoteState.burstFloatSpeed, 0.2f)
                .SetEase(Ease.OutQuad)
                .SetId(RemoteFloatTweenId);

            DOTween.To(() => _burstYOffset, x => _burstYOffset = x, remoteState.burstLiftAmount, remoteState.duration * 0.4f)
                .SetEase(Ease.OutQuad)
                .SetId(RemoteFloatTweenId)
                .OnComplete(() =>
                {
                    DOTween.To(
                        () => _burstYOffset,
                        x => _burstYOffset = x,
                        0f,
                        remoteState.duration * 0.6f)
                        .SetEase(Ease.InOutSine)
                        .SetId(RemoteFloatTweenId);
                });

            DOVirtual.DelayedCall(remoteState.duration, () =>
            {
                DOTween.To(() => floatSpeed, x => floatSpeed = x, _defaultFloatSpeed, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetId(RemoteFloatTweenId);
            }).SetId(RemoteFloatTweenId);

            return remoteState.optimalTriggerDelay;
        }
    }
}
