using System.Collections.Generic;
using UnityEngine;

namespace Bob.SharedMobility
{
    [DefaultExecutionOrder(-900)]
    public sealed class AppNavigationService : MonoBehaviour
    {
        [System.Serializable]
        public sealed class DockScreenBinding
        {
            public AppScreenId screenId = AppScreenId.None;
            public DockPanelController dockPanel;
            [TextArea(1, 3)] public string inspectorNotes;
        }

        public static AppNavigationService Instance { get; private set; }

        [Header("Scene Services")]
        [SerializeField] private DockNavigationManager dockNavigationManager;
        [SerializeField] private MapViewController mapController;

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

        public event System.Action<AppNavigationState> StateChanged;

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

        [ContextMenu("Resolve Scene References")]
        public void ResolveReferences()
        {
            dockNavigationManager = ResolveSceneReference(dockNavigationManager);
            mapController = ResolveSceneReference(mapController);

            if (dockNavigationManager && dockNavigationManager.navigationService == null)
            {
                dockNavigationManager.navigationService = this;
            }
        }

        [ContextMenu("Rebuild Navigation Registry")]
        public void RebuildRegistry()
        {
            _registeredScreens.Clear();
            _screensById.Clear();
            _dockPanelsById.Clear();
            _screenIdsByDockPanel.Clear();

            foreach (AppScreenController screen in screenControllers)
            {
                RegisterScreen(screen);
            }

            foreach (DockScreenBinding binding in dockScreens)
            {
                RegisterDockPanelBinding(binding);
            }

            if (discoverScreensAutomatically)
            {
                foreach (AppScreenController screen in FindObjectsOfType<AppScreenController>(true))
                {
                    RegisterScreen(screen);
                }
            }

            if (discoverDockPanelsAutomatically)
            {
                foreach (DockPanelController dockPanel in FindObjectsOfType<DockPanelController>(true))
                {
                    RegisterDockPanel(dockPanel);
                }
            }

            RefreshWorldInputBlock();
        }

        public void RegisterScreen(AppScreenController screen)
        {
            if (screen == null || screen.ScreenId == AppScreenId.None) return;

            if (!_registeredScreens.Contains(screen))
            {
                _registeredScreens.Add(screen);
            }

            _screensById[screen.ScreenId] = screen;
        }

        public void RegisterDockPanel(DockPanelController dockPanel)
        {
            if (dockPanel == null) return;

            AppScreenId screenId = ResolveExplicitDockPanelScreenId(dockPanel);
            if (screenId == AppScreenId.None)
            {
                ProjectLog.Warning($"Dock panel '{dockPanel.name}' is missing an AppScreenId registration.", dockPanel);
                return;
            }

            RegisterDockPanel(screenId, dockPanel);
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

            if (_screensById.TryGetValue(screenId, out AppScreenController screen))
            {
                if (screen.NavigationLayer == AppNavigationLayer.Modal || screen.NavigationLayer == AppNavigationLayer.Overlay)
                {
                    return OpenModal(screenId);
                }

                CloseAllModals(false);
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

            if (!currentDockPanel && currentScreen != AppScreenId.Home)
            {
                HideRegisteredScreen(currentScreen);
            }

            CloseAllModals(false);

            if (dockNavigationManager)
            {
                dockNavigationManager.SwitchToApp(dockPanel, subPanel);
            }
            else
            {
                if (subPanel)
                {
                    dockPanel.OpenSpecificLevel3(subPanel);
                }
                else
                {
                    dockPanel.OpenLevel2Menu();
                }
            }

            NotifyDockPanelOpened(dockPanel, subPanel);
            return true;
        }

        public void CloseCurrentScreen()
        {
            AppScreenId closingScreen = currentScreen;

            CloseAllModals(false);

            if (dockNavigationManager)
            {
                dockNavigationManager.CloseCurrentApp();
            }
            else if (currentDockPanel)
            {
                currentDockPanel.CloseEntireApp();
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

        public bool OpenModal(AppScreenId modalScreenId)
        {
            if (!_screensById.TryGetValue(modalScreenId, out AppScreenController modal))
            {
                ProjectLog.Warning($"No modal screen registered for '{modalScreenId}'.", this);
                return false;
            }

            if (modal.NavigationLayer != AppNavigationLayer.Modal && modal.NavigationLayer != AppNavigationLayer.Overlay)
            {
                ProjectLog.Warning($"Screen '{modalScreenId}' is registered as {modal.NavigationLayer}, not Modal/Overlay.", modal);
            }

            modal.Show();
            if (!_modalStack.Contains(modal))
            {
                _modalStack.Add(modal);
            }

            currentModal = modalScreenId;
            RefreshWorldInputBlock();
            NotifyStateChanged();
            return true;
        }

        public void CloseTopModal()
        {
            if (_modalStack.Count == 0) return;

            AppScreenController topModal = _modalStack[_modalStack.Count - 1];
            _modalStack.RemoveAt(_modalStack.Count - 1);

            if (topModal)
            {
                topModal.Hide();
            }

            RefreshWorldInputBlock();
            NotifyStateChanged();
        }

        public void CloseAllModals()
        {
            CloseAllModals(true);
        }

        private void RegisterDockPanelBinding(DockScreenBinding binding)
        {
            if (binding == null || binding.dockPanel == null) return;

            RegisterDockPanel(binding.screenId, binding.dockPanel);
        }

        private void RegisterDockPanel(AppScreenId screenId, DockPanelController dockPanel)
        {
            if (screenId == AppScreenId.None || dockPanel == null) return;

            _dockPanelsById[screenId] = dockPanel;
            _screenIdsByDockPanel[dockPanel] = screenId;
        }

        private DockScreenBinding FindDockBinding(DockPanelController dockPanel)
        {
            foreach (DockScreenBinding binding in dockScreens)
            {
                if (binding != null && binding.dockPanel == dockPanel)
                {
                    return binding;
                }
            }

            return null;
        }

        private AppScreenId ResolveExplicitDockPanelScreenId(DockPanelController dockPanel)
        {
            DockScreenBinding binding = FindDockBinding(dockPanel);
            if (binding != null && binding.screenId != AppScreenId.None)
            {
                return binding.screenId;
            }

            return dockPanel.ScreenId;
        }

        private AppScreenId ResolveScreenId(DockPanelController dockPanel)
        {
            if (dockPanel == null) return AppScreenId.None;

            if (_screenIdsByDockPanel.TryGetValue(dockPanel, out AppScreenId screenId))
            {
                return screenId;
            }

            return ResolveExplicitDockPanelScreenId(dockPanel);
        }

        private void SetNavigationState(
            AppScreenId screenId,
            AppNavigationLayer layer,
            DockPanelController dockPanel,
            CanvasGroup subPanel)
        {
            if (currentScreen == screenId
                && currentLayer == layer
                && currentDockPanel == dockPanel
                && currentSubPanel == subPanel)
            {
                RefreshWorldInputBlock();
                return;
            }

            currentScreen = screenId;
            currentLayer = layer;
            currentDockPanel = dockPanel;
            currentSubPanel = subPanel;

            RefreshWorldInputBlock();
            NotifyStateChanged();
        }

        private void HideRegisteredScreen(AppScreenId screenId)
        {
            foreach (AppScreenController screen in _registeredScreens)
            {
                if (screen != null && screen.ScreenId == screenId)
                {
                    screen.Hide();
                }
            }
        }

        private void CloseAllModals(bool notify)
        {
            if (_modalStack.Count == 0)
            {
                RefreshWorldInputBlock();
                return;
            }

            foreach (AppScreenController modal in _modalStack)
            {
                if (modal)
                {
                    modal.Hide();
                }
            }

            _modalStack.Clear();
            RefreshWorldInputBlock();

            if (notify)
            {
                NotifyStateChanged();
            }
        }

        private void RefreshWorldInputBlock()
        {
            currentModal = AppScreenId.None;
            worldInputBlocked = false;

            for (int i = _modalStack.Count - 1; i >= 0; i--)
            {
                AppScreenController modal = _modalStack[i];
                if (!modal)
                {
                    _modalStack.RemoveAt(i);
                    continue;
                }

                if (currentModal == AppScreenId.None)
                {
                    currentModal = modal.ScreenId;
                }

                if (modal.BlocksWorldInputWhenVisible)
                {
                    worldInputBlocked = true;
                }
            }
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke(CurrentState);
        }

        private static T ResolveSceneReference<T>(T current) where T : UnityEngine.Object
        {
            return current != null ? current : FindObjectOfType<T>(true);
        }
    }
}
