using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class MapViewController
    {
        public void OpenSelectorMenu()
        {
            if (!selectorMenu) return;

            selectorMenu.gameObject.SetActive(true);
            selectorMenu.transform.DOKill();
            selectorMenu.transform.localScale = Vector3.zero;
            selectorMenu.transform.DOScale(selectorTargetScale, popDuration).SetEase(openEase, elasticity);
            selectorMenu.OpenChildren();
        }

        public void CloseSelectorMenu()
        {
            if (!selectorMenu || !selectorMenu.gameObject.activeSelf) return;

            selectorMenu.transform.DOKill();
            selectorMenu.transform
                .DOScale(Vector3.zero, 0.2f)
                .OnComplete(() => selectorMenu.gameObject.SetActive(false));
        }

        private void HandleDelayedIcons(ViewState newState)
        {
            _delayedCallTween?.Kill();
            StopDelayedIconAnimations();

            if (newState == ViewState.Small_Icon)
            {
                HideDelayedIconsAnimated();
                return;
            }

            _delayedCallTween = DOVirtual.DelayedCall(appearDelay, ShowDelayedIcons);
        }

        private void ShowDelayedIcons()
        {
            if (delayedIcons == null) return;

            foreach (GameObject icon in delayedIcons)
            {
                if (!icon) continue;

                icon.SetActive(true);
                icon.transform.localScale = Vector3.zero;
                icon.transform.DOScale(Vector3.one, appearDuration).SetEase(Ease.OutSine);
            }
        }

        private void HideDelayedIconsAnimated()
        {
            if (delayedIcons == null) return;

            foreach (GameObject icon in delayedIcons)
            {
                if (!icon) continue;

                icon.transform.DOKill();
                icon.transform
                    .DOScale(Vector3.zero, 0.3f)
                    .OnComplete(() => icon.SetActive(false));
            }
        }

        private void HideDelayedIconsInstant()
        {
            StopDelayedIconAnimations();

            if (delayedIcons == null) return;

            foreach (GameObject icon in delayedIcons)
            {
                if (!icon) continue;

                icon.transform.localScale = Vector3.zero;
                icon.SetActive(false);
            }
        }

        private void StopDelayedIconAnimations()
        {
            if (delayedIcons == null) return;

            foreach (GameObject icon in delayedIcons)
            {
                if (!icon) continue;

                icon.transform.DOKill();
            }
        }

        private void CloseSelectorMenuInstant()
        {
            if (!selectorMenu) return;

            selectorMenu.transform.localScale = Vector3.zero;
            selectorMenu.gameObject.SetActive(false);
        }
    }
}
