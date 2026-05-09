using UnityEngine;
using UnityEngine.Events;

namespace Bob.SharedMobility
{
    public interface IAppScreenLifecycle
    {
        void OnScreenEnter(AppNavigationState state);
        void OnScreenExit(AppNavigationState state);
        void OnScreenPause(AppNavigationState state);
        void OnScreenResume(AppNavigationState state);
    }

    public sealed class AppScreenLifecycleController : MonoBehaviour, IAppScreenLifecycle
    {
        [Header("Lifecycle Events")]
        [SerializeField] private UnityEvent onEnter = new UnityEvent();
        [SerializeField] private UnityEvent onExit = new UnityEvent();
        [SerializeField] private UnityEvent onPause = new UnityEvent();
        [SerializeField] private UnityEvent onResume = new UnityEvent();

        [Header("Diagnostics")]
        [SerializeField] private bool logLifecycle = false;

        public void OnScreenEnter(AppNavigationState state)
        {
            if (logLifecycle)
            {
                ProjectLog.Info($"Screen entered: {state.screenId}", this);
            }

            onEnter?.Invoke();
        }

        public void OnScreenExit(AppNavigationState state)
        {
            if (logLifecycle)
            {
                ProjectLog.Info($"Screen exited: {state.screenId}", this);
            }

            onExit?.Invoke();
        }

        public void OnScreenPause(AppNavigationState state)
        {
            if (logLifecycle)
            {
                ProjectLog.Info($"Screen paused: {state.screenId}", this);
            }

            onPause?.Invoke();
        }

        public void OnScreenResume(AppNavigationState state)
        {
            if (logLifecycle)
            {
                ProjectLog.Info($"Screen resumed: {state.screenId}", this);
            }

            onResume?.Invoke();
        }
    }
}
