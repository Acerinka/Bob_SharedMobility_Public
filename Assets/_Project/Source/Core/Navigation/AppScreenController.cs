using System.Collections.Generic;
using UnityEngine;

namespace Bob.SharedMobility
{
    public sealed class AppScreenController : MonoBehaviour
    {
        [Header("Navigation Identity")]
        [SerializeField] private AppScreenId screenId = AppScreenId.None;
        [SerializeField] private AppNavigationLayer navigationLayer = AppNavigationLayer.DockPanel;

        [Header("Presentation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool visibleOnStart = false;
        [SerializeField] private bool blocksWorldInputWhenVisible = true;
        [SerializeField] private float fadeDuration = 0.25f;

        public AppScreenId ScreenId => screenId;
        public AppNavigationLayer NavigationLayer => navigationLayer;
        public CanvasGroup CanvasGroup => canvasGroup;
        public bool BlocksWorldInputWhenVisible => blocksWorldInputWhenVisible;
        public bool IsVisible => _isVisible || CanvasGroupPresenter.IsVisibleAndBlocking(canvasGroup);

        private readonly List<IAppScreenLifecycle> _lifecycleHandlers = new List<IAppScreenLifecycle>();
        private bool _isVisible;

        private void Awake()
        {
            ResolveReferences();
            ResolveLifecycleHandlers();

            if (visibleOnStart)
            {
                Show(true);
            }
            else
            {
                Hide(true);
            }
        }

        private void OnEnable()
        {
            if (AppNavigationService.Instance)
            {
                AppNavigationService.Instance.RegisterScreen(this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!canvasGroup)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
#endif

        public void Show(bool instant = false)
        {
            ResolveReferences();
            ResolveLifecycleHandlers();
            CanvasGroupPresenter.Show(canvasGroup, instant ? 0f : fadeDuration);

            if (_isVisible) return;

            _isVisible = true;
            NotifyLifecycleEnter();
        }

        public void Hide(bool instant = false)
        {
            ResolveReferences();
            ResolveLifecycleHandlers();

            if (_isVisible)
            {
                NotifyLifecycleExit();
                _isVisible = false;
            }

            CanvasGroupPresenter.Hide(canvasGroup, instant ? 0f : fadeDuration);
        }

        public void Pause()
        {
            ResolveLifecycleHandlers();
            NotifyLifecyclePause();
        }

        public void Resume()
        {
            ResolveLifecycleHandlers();
            NotifyLifecycleResume();
        }

        private void ResolveReferences()
        {
            if (!canvasGroup)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void ResolveLifecycleHandlers()
        {
            _lifecycleHandlers.Clear();

            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour == this) continue;

                if (behaviour is IAppScreenLifecycle lifecycleHandler)
                {
                    _lifecycleHandlers.Add(lifecycleHandler);
                }
            }
        }

        private void NotifyLifecycleEnter()
        {
            AppNavigationState state = CreateLifecycleState();

            foreach (IAppScreenLifecycle lifecycleHandler in _lifecycleHandlers)
            {
                lifecycleHandler.OnScreenEnter(state);
            }
        }

        private void NotifyLifecycleExit()
        {
            AppNavigationState state = CreateLifecycleState();

            foreach (IAppScreenLifecycle lifecycleHandler in _lifecycleHandlers)
            {
                lifecycleHandler.OnScreenExit(state);
            }
        }

        private void NotifyLifecyclePause()
        {
            AppNavigationState state = CreateLifecycleState();

            foreach (IAppScreenLifecycle lifecycleHandler in _lifecycleHandlers)
            {
                lifecycleHandler.OnScreenPause(state);
            }
        }

        private void NotifyLifecycleResume()
        {
            AppNavigationState state = CreateLifecycleState();

            foreach (IAppScreenLifecycle lifecycleHandler in _lifecycleHandlers)
            {
                lifecycleHandler.OnScreenResume(state);
            }
        }

        private AppNavigationState CreateLifecycleState()
        {
            AppNavigationService navigationService = AppNavigationService.Instance;
            AppScreenId modalScreen = navigationService ? navigationService.CurrentModal : AppScreenId.None;
            bool blocksWorldInput = navigationService ? navigationService.BlocksWorldInput : blocksWorldInputWhenVisible;
            return new AppNavigationState(screenId, navigationLayer, modalScreen, blocksWorldInput);
        }
    }
}
