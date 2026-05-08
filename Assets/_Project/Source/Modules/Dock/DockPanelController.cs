using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public class DockPanelController : MonoBehaviour
    {
        [Header("Owner")]
        public DockButtonController myDockItem;

        [Header("Bob Interaction")]
        public bool causesBobAvoid = true;

        [Header("Panels")]
        public CanvasGroup myLevel2Panel;

        [Header("Animation")]
        public float fadeDuration = 0.3f;

        private CanvasGroup _currentActiveLevel3;

        private void Start()
        {
            HidePanelImmediate(myLevel2Panel);
        }

        public void OpenLevel2Menu()
        {
            if (_currentActiveLevel3 != null)
            {
                HidePanelImmediate(_currentActiveLevel3);
                _currentActiveLevel3 = null;
            }

            OpenPanel(myLevel2Panel);
        }

        public void OpenSpecificLevel3(CanvasGroup targetSubPanel)
        {
            if (targetSubPanel == null) return;

            _currentActiveLevel3 = targetSubPanel;
            ClosePanel(myLevel2Panel);
            OpenPanel(targetSubPanel);
        }

        public void BackToLevel2()
        {
            if (_currentActiveLevel3 != null)
            {
                ClosePanel(_currentActiveLevel3);
                _currentActiveLevel3 = null;
            }

            OpenPanel(myLevel2Panel);
        }

        public void CloseEntireApp()
        {
            ClosePanel(myLevel2Panel);

            if (_currentActiveLevel3 != null)
            {
                ClosePanel(_currentActiveLevel3);
                _currentActiveLevel3 = null;
            }

            if (myDockItem)
            {
                myDockItem.ResetState();
            }
        }

        public void OnCloseButtonClicked()
        {
            if (DockNavigationManager.Instance)
            {
                DockNavigationManager.Instance.CloseCurrentApp();
            }
        }

        private void OpenPanel(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.transform.DOKill();
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.transform.localScale = Vector3.one * 0.9f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            canvasGroup.DOFade(1f, fadeDuration);
            canvasGroup.transform.DOScale(1f, fadeDuration).SetEase(Ease.OutBack);
        }

        private void ClosePanel(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.transform.DOKill();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            canvasGroup
                .DOFade(0f, fadeDuration * 0.8f)
                .OnComplete(() => canvasGroup.gameObject.SetActive(false));
        }

        private static void HidePanelImmediate(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) return;

            canvasGroup.DOKill();
            canvasGroup.transform.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.gameObject.SetActive(false);
        }
    }
}
