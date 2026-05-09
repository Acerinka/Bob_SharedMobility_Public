using UnityEngine;

namespace Bob.SharedMobility
{
    public sealed partial class AppNavigationService
    {
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

            if (_modalStack.Count == 0)
            {
                PauseCurrentScreen();
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
            if (_modalStack.Count == 0)
            {
                ResumeCurrentScreen();
            }

            NotifyStateChanged();
        }

        public void CloseAllModals()
        {
            CloseAllModals(true);
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

        private void CloseAllModals(bool notify, bool resumeUnderlying = true)
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

            if (resumeUnderlying)
            {
                ResumeCurrentScreen();
            }

            if (notify)
            {
                NotifyStateChanged();
            }
        }

        private void PauseCurrentScreen()
        {
            if (_screensById.TryGetValue(currentScreen, out AppScreenController screen) && screen)
            {
                screen.Pause();
            }

            if (currentDockPanel)
            {
                NotifyDockPanelLifecycle(currentDockPanel, ScreenLifecyclePhase.Pause);
            }
        }

        private void ResumeCurrentScreen()
        {
            if (_screensById.TryGetValue(currentScreen, out AppScreenController screen) && screen)
            {
                screen.Resume();
            }

            if (currentDockPanel)
            {
                NotifyDockPanelLifecycle(currentDockPanel, ScreenLifecyclePhase.Resume);
            }
        }

        private void NotifyDockPanelLifecycle(DockPanelController dockPanel, ScreenLifecyclePhase phase)
        {
            if (!dockPanel) return;

            AppScreenId screenId = ResolveScreenId(dockPanel);
            AppNavigationState state = new AppNavigationState(
                screenId,
                currentLayer,
                currentModal,
                worldInputBlocked);

            foreach (MonoBehaviour behaviour in dockPanel.GetComponents<MonoBehaviour>())
            {
                IAppScreenLifecycle lifecycleHandler = behaviour as IAppScreenLifecycle;
                if (lifecycleHandler == null) continue;

                switch (phase)
                {
                    case ScreenLifecyclePhase.Pause:
                        lifecycleHandler.OnScreenPause(state);
                        break;
                    case ScreenLifecyclePhase.Resume:
                        lifecycleHandler.OnScreenResume(state);
                        break;
                }
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
    }
}
