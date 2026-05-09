using System.Collections.Generic;
using UnityEngine;

namespace Bob.SharedMobility
{
    public sealed partial class AppNavigationService
    {
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
            _missingRouteWarnings.Clear();

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

        [ContextMenu("Validate Navigation Routes")]
        public void ValidateNavigationRoutes()
        {
            if (!routeTable)
            {
                ProjectLog.Warning("AppNavigationService has no route table assigned.", this);
                return;
            }

            HashSet<AppScreenId> routeIds = new HashSet<AppScreenId>();
            foreach (AppNavigationRouteTable.Route route in routeTable.Routes)
            {
                if (route == null || route.screenId == AppScreenId.None)
                {
                    ProjectLog.Warning("Route table contains an empty route entry.", routeTable);
                    continue;
                }

                if (!routeIds.Add(route.screenId))
                {
                    ProjectLog.Warning($"Route table contains duplicate screen id '{route.screenId}'.", routeTable);
                }

                if (route.routeKind == AppNavigationRouteTable.RouteKind.DockScreen
                    && !_dockPanelsById.ContainsKey(route.screenId))
                {
                    ProjectLog.Warning($"Dock route '{route.screenId}' is not bound to a DockPanelController in the scene registry.", this);
                }

                bool requiresScreenController = route.routeKind == AppNavigationRouteTable.RouteKind.Screen
                    || route.routeKind == AppNavigationRouteTable.RouteKind.Modal
                    || route.routeKind == AppNavigationRouteTable.RouteKind.Overlay;

                if (requiresScreenController
                    && route.productionStatus == AppNavigationRouteTable.ProductionStatus.ProductionReady
                    && !_screensById.ContainsKey(route.screenId)
                    && route.screenPrefab == null)
                {
                    ProjectLog.Warning($"Production-ready route '{route.screenId}' has no scene AppScreenController and no screen prefab assigned.", routeTable);
                }
            }

            foreach (AppScreenId screenId in _screensById.Keys)
            {
                WarnIfRouteMissing(screenId, this);
            }

            foreach (AppScreenId screenId in _dockPanelsById.Keys)
            {
                WarnIfRouteMissing(screenId, this);
            }
        }

        public void RegisterScreen(AppScreenController screen)
        {
            if (screen == null || screen.ScreenId == AppScreenId.None) return;

            if (!_registeredScreens.Contains(screen))
            {
                _registeredScreens.Add(screen);
            }

            _screensById[screen.ScreenId] = screen;
            WarnIfRouteMissing(screen.ScreenId, screen);
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
            WarnIfRouteMissing(screenId, dockPanel);
        }

        private bool TryInstantiateRouteScreen(AppScreenId screenId)
        {
            if (!routeTable || !routeTable.TryGetRoute(screenId, out AppNavigationRouteTable.Route route))
            {
                return false;
            }

            if (!route.screenPrefab)
            {
                return false;
            }

            Transform parent = dynamicScreenRoot ? dynamicScreenRoot : transform;
            GameObject instance = Instantiate(route.screenPrefab, parent);
            instance.name = route.screenPrefab.name.Replace("PF_", "Screen_");

            AppScreenController screen = instance.GetComponent<AppScreenController>();
            if (!screen)
            {
                ProjectLog.Warning($"Route prefab for '{screenId}' has no AppScreenController.", instance);
                Destroy(instance);
                return false;
            }

            if (screen.ScreenId != screenId)
            {
                ProjectLog.Warning($"Route prefab for '{screenId}' has AppScreenController.ScreenId '{screen.ScreenId}'. Fix the prefab identity.", screen);
                Destroy(instance);
                return false;
            }

            screen.Hide(true);
            RegisterScreen(screen);
            return true;
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

        private void WarnIfRouteMissing(AppScreenId screenId, UnityEngine.Object context)
        {
            if (!routeTable || screenId == AppScreenId.None || routeTable.Contains(screenId)) return;
            if (!_missingRouteWarnings.Add(screenId)) return;

            ProjectLog.Warning($"Screen '{screenId}' is registered in the scene but missing from the navigation route table.", context);
        }

        private static T ResolveSceneReference<T>(T current) where T : UnityEngine.Object
        {
            return current != null ? current : FindObjectOfType<T>(true);
        }
    }
}
