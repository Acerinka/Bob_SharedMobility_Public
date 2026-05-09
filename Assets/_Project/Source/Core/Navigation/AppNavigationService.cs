using System.Collections.Generic;
using UnityEngine;

namespace Bob.SharedMobility
{
    [DefaultExecutionOrder(-900)]
    public sealed partial class AppNavigationService : MonoBehaviour
    {
        [System.Serializable]
        public sealed class DockScreenBinding
        {
            public AppScreenId screenId = AppScreenId.None;
            public DockPanelController dockPanel;
            [TextArea(1, 3)] public string inspectorNotes;
        }

        private enum ScreenLifecyclePhase
        {
            Pause,
            Resume
        }

        public static AppNavigationService Instance { get; private set; }

        [Header("Scene Services")]
        [SerializeField] private DockNavigationManager dockNavigationManager;
        [SerializeField] private MapViewController mapController;

        [Header("Route Table")]
        [SerializeField] private AppNavigationRouteTable routeTable;
        [SerializeField] private Transform dynamicScreenRoot;

        [Header("Screen Registry")]
        [SerializeField] private bool discoverScreensAutomatically = true;
        [SerializeField] private List<AppScreenController> screenControllers = new List<AppScreenController>();

        [Header("Dock Screen Registry")]
        [SerializeField] private bool discoverDockPanelsAutomatically = true;
        [SerializeField] private List<DockScreenBinding> dockScreens = new List<DockScreenBinding>();

        [Header("Runtime State")]
        [SerializeField] private AppScreenId currentScreen = AppScreenId.Home;
        [SerializeField] private AppNavigationLayer currentLayer = AppNavigationLayer.Shell;
        [SerializeField] private AppScreenId currentModal = AppScreenId.None;
        [SerializeField] private bool worldInputBlocked = false;
        [SerializeField] private DockPanelController currentDockPanel;
        [SerializeField] private CanvasGroup currentSubPanel;

        private readonly List<AppScreenController> _registeredScreens = new List<AppScreenController>();
        private readonly List<AppScreenController> _modalStack = new List<AppScreenController>();
        private readonly Dictionary<AppScreenId, AppScreenController> _screensById = new Dictionary<AppScreenId, AppScreenController>();
        private readonly Dictionary<AppScreenId, DockPanelController> _dockPanelsById = new Dictionary<AppScreenId, DockPanelController>();
        private readonly Dictionary<DockPanelController, AppScreenId> _screenIdsByDockPanel = new Dictionary<DockPanelController, AppScreenId>();
        private readonly HashSet<AppScreenId> _missingRouteWarnings = new HashSet<AppScreenId>();

        public event System.Action<AppNavigationState> StateChanged;

        public AppNavigationRouteTable RouteTable => routeTable;
        public AppScreenId CurrentScreen => currentScreen;
        public AppNavigationLayer CurrentLayer => currentLayer;
        public AppScreenId CurrentModal => currentModal;
        public DockPanelController CurrentDockPanel => currentDockPanel;
        public CanvasGroup CurrentSubPanel => currentSubPanel;
        public bool BlocksWorldInput => worldInputBlocked;
        public AppNavigationState CurrentState => new AppNavigationState(currentScreen, currentLayer, currentModal, worldInputBlocked);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                ProjectLog.Warning("Multiple AppNavigationService instances detected; the latest instance will be used.", this);
            }

            Instance = this;
            ResolveReferences();
            RebuildRegistry();
        }

        private void Start()
        {
            ResolveReferences();
            RebuildRegistry();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool OpenHome(MapViewController preferredMapController = null)
        {
            CloseCurrentScreen();

            MapViewController targetMap = preferredMapController ? preferredMapController : mapController;
            if (targetMap)
            {
                targetMap.SwitchToState(MapViewController.ViewState.Small_Icon);
            }

            SetNavigationState(AppScreenId.Home, AppNavigationLayer.Shell, null, null);
            return true;
        }

        public bool OpenScreen(AppScreenId screenId)
        {
            return OpenScreen(screenId, null);
        }

        public bool OpenScreen(AppScreenId screenId, CanvasGroup subPanel)
        {
            if (screenId == AppScreenId.Home)
            {
                return OpenHome();
            }

            if (_dockPanelsById.TryGetValue(screenId, out DockPanelController dockPanel))
            {
                return OpenDockPanel(dockPanel, subPanel);
            }

            if (!_screensById.ContainsKey(screenId))
            {
                TryInstantiateRouteScreen(screenId);
            }

            if (_screensById.TryGetValue(screenId, out AppScreenController screen))
            {
                if (screen.NavigationLayer == AppNavigationLayer.Modal || screen.NavigationLayer == AppNavigationLayer.Overlay)
                {
                    return OpenModal(screenId);
                }

                CloseAllModals(false, false);
                CloseCurrentScreen();
                screen.Show();
                SetNavigationState(screenId, screen.NavigationLayer, null, null);
                return true;
            }

            ProjectLog.Warning($"No navigation target registered for screen '{screenId}'.", this);
            return false;
        }

        public bool OpenDockPanel(DockPanelController dockPanel)
        {
            return OpenDockPanel(dockPanel, null);
        }

        public bool OpenDockPanel(DockPanelController dockPanel, CanvasGroup subPanel)
        {
            if (dockPanel == null) return false;

            ResolveReferences();
            RegisterDockPanel(dockPanel);

            if (currentDockPanel == dockPanel && currentSubPanel == subPanel)
            {
                RefreshWorldInputBlock();
                return true;
            }

            if (!currentDockPanel && currentScreen != AppScreenId.Home)
            {
                HideRegisteredScreen(currentScreen);
            }

            CloseAllModals(false, false);

            if (dockNavigationManager)
            {
                dockNavigationManager.SwitchToApp(dockPanel, subPanel);
            }
            else
            {
                if (subPanel)
                {
                    dockPanel.ApplyOpenSpecificLevel3(subPanel);
                }
                else
                {
                    dockPanel.ApplyOpenLevel2Menu();
                }
            }

            NotifyDockPanelOpened(dockPanel, subPanel);
            return true;
        }

        public bool ToggleDockPanel(DockPanelController dockPanel)
        {
            return ToggleDockPanel(dockPanel, null);
        }

        public bool ToggleDockPanel(DockPanelController dockPanel, CanvasGroup subPanel)
        {
            if (dockPanel == null) return false;

            ResolveReferences();
            RegisterDockPanel(dockPanel);

            if (currentDockPanel == dockPanel && currentSubPanel == subPanel)
            {
                CloseCurrentScreen();
                return true;
            }

            return OpenDockPanel(dockPanel, subPanel);
        }

        public void CloseCurrentScreen()
        {
            AppScreenId closingScreen = currentScreen;

            CloseAllModals(false, false);

            if (dockNavigationManager)
            {
                dockNavigationManager.CloseCurrentApp();
            }
            else if (currentDockPanel)
            {
                currentDockPanel.ApplyCloseEntireApp();
            }

            HideRegisteredScreen(closingScreen);

            SetNavigationState(AppScreenId.Home, AppNavigationLayer.Shell, null, null);
        }

        public void NotifyDockPanelOpened(DockPanelController dockPanel, CanvasGroup subPanel)
        {
            if (dockPanel == null) return;

            RegisterDockPanel(dockPanel);

            AppScreenId screenId = ResolveScreenId(dockPanel);
            if (screenId == AppScreenId.None) return;

            AppNavigationLayer layer = subPanel ? AppNavigationLayer.SubPanel : AppNavigationLayer.DockPanel;
            SetNavigationState(screenId, layer, dockPanel, subPanel);
        }

        public void NotifyDockPanelClosed(DockPanelController dockPanel)
        {
            if (dockPanel == null || currentDockPanel != dockPanel) return;

            SetNavigationState(AppScreenId.Home, AppNavigationLayer.Shell, null, null);
        }

    }
}
