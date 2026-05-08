using UnityEngine;

namespace Bob.SharedMobility
{
    public class DockPanelController : MonoBehaviour
    {
        [Header("Navigation Identity")]
        [SerializeField] private AppScreenId screenId = AppScreenId.None;
        [SerializeField] private AppNavigationLayer navigationLayer = AppNavigationLayer.DockPanel;

        [Header("Owner")]
        public DockButtonController myDockItem;

        [Header("Bob Interaction")]
        public bool causesBobAvoid = true;

        [Header("Panels")]
        public CanvasGroup myLevel2Panel;

        [Header("Animation")]
        public float fadeDuration = 0.3f;

        private CanvasGroup _currentActiveLevel3;

        public AppScreenId ScreenId => screenId;
        public AppNavigationLayer NavigationLayer => navigationLayer;
        public CanvasGroup CurrentActiveLevel3 => _currentActiveLevel3;
        public bool IsOpen { get; private set; }

        private void Start()
        {
            if (AppNavigationService.Instance)
            {
                AppNavigationService.Instance.RegisterDockPanel(this);
            }

            CanvasGroupPresenter.HideImmediate(myLevel2Panel);
        }

        public void OpenLevel2Menu()
        {
            if (_currentActiveLevel3 != null)
            {
                CanvasGroupPresenter.HideImmediate(_currentActiveLevel3);
                _currentActiveLevel3 = null;
            }

            OpenPanel(myLevel2Panel);
            IsOpen = true;
            AppNavigationService.Instance?.NotifyDockPanelOpened(this, null);
        }

        public void OpenSpecificLevel3(CanvasGroup targetSubPanel)
        {
            if (targetSubPanel == null) return;

            if (_currentActiveLevel3 != null && _currentActiveLevel3 != targetSubPanel)
            {
                CanvasGroupPresenter.HideImmediate(_currentActiveLevel3);
            }

            _currentActiveLevel3 = targetSubPanel;
            ClosePanel(myLevel2Panel);
            OpenPanel(targetSubPanel);
            IsOpen = true;
            AppNavigationService.Instance?.NotifyDockPanelOpened(this, targetSubPanel);
        }

        public void BackToLevel2()
        {
            if (_currentActiveLevel3 != null)
            {
                ClosePanel(_currentActiveLevel3);
                _currentActiveLevel3 = null;
            }

            OpenPanel(myLevel2Panel);
            IsOpen = true;
            AppNavigationService.Instance?.NotifyDockPanelOpened(this, null);
        }

        public void CloseEntireApp()
        {
            ClosePanel(myLevel2Panel);

            if (_currentActiveLevel3 != null)
            {
                ClosePanel(_currentActiveLevel3);
                _currentActiveLevel3 = null;
            }

            IsOpen = false;
            AppNavigationService.Instance?.NotifyDockPanelClosed(this);

            if (myDockItem)
            {
                myDockItem.ResetState();
            }
        }

        public void OnCloseButtonClicked()
        {
            if (AppNavigationService.Instance)
            {
                AppNavigationService.Instance.CloseCurrentScreen();
            }
            else if (DockNavigationManager.Instance)
            {
                DockNavigationManager.Instance.CloseCurrentApp();
            }
        }

        private void OpenPanel(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) return;

            CanvasGroupPresenter.Show(canvasGroup, fadeDuration);
        }

        private void ClosePanel(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) return;

            CanvasGroupPresenter.Hide(canvasGroup, fadeDuration * 0.8f);
        }
    }
}
