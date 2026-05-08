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
        public bool BlocksWorldInputWhenVisible => blocksWorldInputWhenVisible;
        public bool IsVisible => CanvasGroupPresenter.IsVisibleAndBlocking(canvasGroup);

        private void Awake()
        {
            ResolveReferences();

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
            CanvasGroupPresenter.Show(canvasGroup, instant ? 0f : fadeDuration);
        }

        public void Hide(bool instant = false)
        {
            ResolveReferences();
            CanvasGroupPresenter.Hide(canvasGroup, instant ? 0f : fadeDuration);
        }

        private void ResolveReferences()
        {
            if (!canvasGroup)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }
    }
}
