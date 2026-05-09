using UnityEngine;

namespace Bob.SharedMobility
{
    public class DockNavigationManager : MonoBehaviour
    {
        public static DockNavigationManager Instance { get; private set; }

        [Header("References")]
        public BobController bobActor;
        public MapViewController mapController;

        [Header("Navigation Service")]
        public AppNavigationService navigationService;

        private DockPanelController _currentActiveApp;

        public DockPanelController CurrentActiveApp => _currentActiveApp;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                ProjectLog.Warning("Multiple DockNavigationManager instances detected; the latest instance will be used.", this);
            }

            Instance = this;

            if (!navigationService)
            {
                navigationService = AppNavigationService.Instance;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SwitchToApp(DockPanelController targetApp, CanvasGroup subMenu = null)
        {
            if (_currentActiveApp != null && _currentActiveApp != targetApp)
            {
                _currentActiveApp.ApplyCloseEntireApp();
            }

            if (targetApp == null)
            {
                DockPanelController closedApp = _currentActiveApp;
                _currentActiveApp = null;

                if (closedApp != null)
                {
                    navigationService?.NotifyDockPanelClosed(closedApp);
                }

                return;
            }

            _currentActiveApp = targetApp;

            if (subMenu != null)
            {
                targetApp.ApplyOpenSpecificLevel3(subMenu);
            }
            else
            {
                targetApp.ApplyOpenLevel2Menu();
            }

            UpdateBobAvoidance(targetApp);
            navigationService?.NotifyDockPanelOpened(targetApp, subMenu);
        }

        public void ToggleApp(DockPanelController targetApp, CanvasGroup subMenu = null)
        {
            if (targetApp == null)
            {
                CloseCurrentApp();
                return;
            }

            if (_currentActiveApp == targetApp
                && (subMenu == null || targetApp.CurrentActiveLevel3 == subMenu))
            {
                CloseCurrentApp();
                return;
            }

            SwitchToApp(targetApp, subMenu);
        }

        public void CloseCurrentApp()
        {
            DockPanelController closingApp = _currentActiveApp;

            if (_currentActiveApp != null)
            {
                _currentActiveApp.ApplyCloseEntireApp();
                _currentActiveApp = null;
            }

            if (bobActor)
            {
                bobActor.ReturnToIdleDelayed();
            }

            if (closingApp != null)
            {
                navigationService?.NotifyDockPanelClosed(closingApp);
            }
        }

        private void UpdateBobAvoidance(DockPanelController targetApp)
        {
            if (!bobActor || !targetApp) return;

            if (targetApp.causesBobAvoid)
            {
                bobActor.DodgeDown();
            }
            else
            {
                bobActor.ReturnToIdleDelayed();
            }
        }
    }
}
