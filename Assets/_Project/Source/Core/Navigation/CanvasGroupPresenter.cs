using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public static class CanvasGroupPresenter
    {
        private const float HiddenAlpha = 0f;
        private const float VisibleAlpha = 1f;
        private const float DefaultStartScale = 0.92f;

        public static void Show(CanvasGroup canvasGroup, float duration, bool animateScale = true)
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.transform.DOKill();
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            duration = Mathf.Max(0f, duration);
            if (duration <= 0f)
            {
                canvasGroup.alpha = VisibleAlpha;
                if (animateScale)
                {
                    canvasGroup.transform.localScale = Vector3.one;
                }

                return;
            }

            canvasGroup.alpha = HiddenAlpha;
            if (animateScale)
            {
                canvasGroup.transform.localScale = Vector3.one * DefaultStartScale;
                canvasGroup.transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
            }

            canvasGroup.DOFade(VisibleAlpha, duration);
        }

        public static void Hide(CanvasGroup canvasGroup, float duration, bool deactivateOnHidden = true)
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.transform.DOKill();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            duration = Mathf.Max(0f, duration);
            if (duration <= 0f)
            {
                HideImmediate(canvasGroup, deactivateOnHidden);
                return;
            }

            canvasGroup
                .DOFade(HiddenAlpha, duration)
                .OnComplete(() =>
                {
                    if (canvasGroup != null && deactivateOnHidden)
                    {
                        canvasGroup.gameObject.SetActive(false);
                    }
                });
        }

        public static void HideImmediate(CanvasGroup canvasGroup, bool deactivateOnHidden = true)
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.transform.DOKill();
            canvasGroup.alpha = HiddenAlpha;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (deactivateOnHidden)
            {
                canvasGroup.gameObject.SetActive(false);
            }
        }

        public static bool IsVisibleAndBlocking(CanvasGroup canvasGroup)
        {
            return canvasGroup != null
                && canvasGroup.isActiveAndEnabled
                && canvasGroup.blocksRaycasts
                && canvasGroup.alpha > 0.01f;
        }
    }
}
