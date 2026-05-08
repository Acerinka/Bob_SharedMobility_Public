using UnityEngine;

namespace Bob.SharedMobility
{
    public class DockNavigationManager : MonoBehaviour
    {
        public static DockNavigationManager Instance { get; private set; }

        [Header("References")]
        public BobController bobActor;
        public MapViewController mapController;

        private DockPanelController _currentActiveApp;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                ProjectLog.Warning("Multiple DockNavigationManager instances detected; the latest instance will be used.", this);
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

        public void SwitchToApp(DockPanelController targetApp, CanvasGroup subMenu = null)
        {
            if (_currentActiveApp == targetApp && subMenu == null)
            {
                CloseCurrentApp();
                return;
            }

            if (_currentActiveApp != null && _currentActiveApp != targetApp)
            {
                _currentActiveApp.CloseEntireApp();
            }

            if (targetApp == null)
            {
                _currentActiveApp = null;
                return;
            }

            _currentActiveApp = targetApp;

            if (subMenu != null)
            {
                targetApp.OpenSpecificLevel3(subMenu);
            }
            else
            {
                targetApp.OpenLevel2Menu();
            }

            UpdateBobAvoidance(targetApp);
        }

        public void CloseCurrentApp()
        {
            if (_currentActiveApp != null)
            {
                _currentActiveApp.CloseEntireApp();
                _currentActiveApp = null;
            }

            if (bobActor)
            {
                bobActor.ReturnToIdleDelayed();
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
