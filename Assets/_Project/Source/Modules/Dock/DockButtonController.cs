using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    public class DockButtonController : MonoBehaviour
    {
        [Header("Navigation")]
        public bool isHomeButton = false;
        public AppScreenId screenId = AppScreenId.None;
        public DockPanelController myAppController;
        public MapViewController mapController;

        [Header("Bob Interaction")]
        public float stayDuration = 1.0f;
        public bool shouldBobReturn = true;

        [Header("Visuals")]
        public RectTransform targetIcon;
        public Transform liquidBase3D;
        public Image iconImageComponent;

        [Header("Animation")]
        public float impactScale = 0.7f;
        public float activeScale = 1.4f;
        public float floatHeight = 30f;
        public Color activeColor = Color.cyan;

        private Vector3 _iconInitScale;
        private Vector3 _iconInitPos;
        private Vector3 _baseInitScale;
        private Color _iconInitColor;
        private Sequence _currentSequence;
        private Tween _bobLeaveTween;
        private Tween _homeResetTween;
        private int _bobInteractionToken;

        private void Awake()
        {
            CacheInitialState();

            Button button = GetComponent<Button>();
            if (button)
            {
                button.onClick.AddListener(OnUserClick);
            }
        }

        public void OnUserClick()
        {
            KillAllTweens();
            AnimateIconOnly();
            TriggerLogic(null);

            if (isHomeButton)
            {
                _homeResetTween?.Kill();
                _homeResetTween = DOVirtual.DelayedCall(0.5f, ResetState);
            }
        }

        public void ActivateByBob(CanvasGroup subMenu = null)
        {
            ActivateByBob(subMenu, 0);
        }

        public void ActivateByBob(CanvasGroup subMenu, int bobInteractionToken)
        {
            KillAllTweens();
            _bobInteractionToken = bobInteractionToken;
            AnimateIconOnly();
            AnimateLiquidBase();
            TriggerLogic(subMenu);

            float delay = stayDuration > 0f ? stayDuration : 0.5f;
            _bobLeaveTween?.Kill();
            _bobLeaveTween = DOVirtual.DelayedCall(delay, OnBobLeave);
        }

        public void ResetState()
        {
            CancelBobInteraction();
            ResetVisualState();
        }

        public void CancelBobInteraction()
        {
            _bobLeaveTween?.Kill();
            _bobLeaveTween = null;
            _homeResetTween?.Kill();
            _homeResetTween = null;
            _bobInteractionToken = 0;
        }

        private void ResetVisualState()
        {
            KillAllTweens();

            if (targetIcon)
            {
                targetIcon.DOScale(_iconInitScale, 0.3f);
                targetIcon.DOLocalMove(_iconInitPos, 0.3f);
            }

            if (iconImageComponent)
            {
                iconImageComponent.DOColor(_iconInitColor, 0.3f);
            }

            if (liquidBase3D)
            {
                liquidBase3D.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
            }
        }

        private void CacheInitialState()
        {
            if (targetIcon)
            {
                _iconInitScale = targetIcon.localScale;
                _iconInitPos = targetIcon.localPosition;
            }

            if (iconImageComponent)
            {
                _iconInitColor = iconImageComponent.color;
            }

            if (liquidBase3D)
            {
                _baseInitScale = liquidBase3D.localScale == Vector3.zero
                    ? Vector3.one
                    : liquidBase3D.localScale;

                liquidBase3D.localScale = Vector3.zero;
                liquidBase3D.gameObject.SetActive(true);
            }
        }

        private void TriggerLogic(CanvasGroup subMenu)
        {
            AppNavigationService navigationService = AppNavigationService.Instance;
            DockNavigationManager dockNavigationManager = DockNavigationManager.Instance;

            if (navigationService == null && dockNavigationManager == null) return;

            if (isHomeButton)
            {
                if (navigationService)
                {
                    navigationService.OpenHome(mapController);
                    return;
                }

                dockNavigationManager.CloseCurrentApp();

                if (mapController)
                {
                    mapController.SwitchToState(MapViewController.ViewState.Small_Icon);
                }
                else
                {
                    ProjectLog.Warning("Home button cannot reset the map because MapViewController is not assigned.", this);
                }

                return;
            }

            if (myAppController)
            {
                if (navigationService)
                {
                    navigationService.OpenDockPanel(myAppController, subMenu);
                }
                else
                {
                    dockNavigationManager.SwitchToApp(myAppController, subMenu);
                }

                return;
            }

            if (navigationService && screenId != AppScreenId.None)
            {
                navigationService.OpenScreen(screenId, subMenu);
            }
        }

        private void OnBobLeave()
        {
            int interactionToken = _bobInteractionToken;
            _bobInteractionToken = 0;
            _bobLeaveTween = null;
            ResetVisualState();

            if (!shouldBobReturn || !BobInteractionDirector.Instance) return;

            Vector3 exitPosition = liquidBase3D ? liquidBase3D.position : transform.position;
            BobInteractionDirector.Instance.ReleaseBobFrom(exitPosition, interactionToken);
        }

        private void AnimateIconOnly()
        {
            _currentSequence = DOTween.Sequence();

            if (targetIcon)
            {
                _currentSequence.Append(targetIcon.DOScale(_iconInitScale * impactScale, 0.1f).SetEase(Ease.OutQuad));
            }

            if (iconImageComponent)
            {
                _currentSequence.Join(iconImageComponent.DOColor(activeColor, 0.1f));
            }

            _currentSequence.AppendCallback(() =>
            {
                if (!targetIcon) return;

                targetIcon.DOScale(_iconInitScale * activeScale, 0.4f).SetEase(Ease.OutBack);
                targetIcon.DOLocalMoveY(_iconInitPos.y + floatHeight, 0.4f).SetEase(Ease.OutBack);
            });
        }

        private void AnimateLiquidBase()
        {
            if (!liquidBase3D) return;

            liquidBase3D.localScale = Vector3.zero;
            liquidBase3D.DOScale(_baseInitScale, 0.5f).SetEase(Ease.OutElastic);
        }

        private void KillAllTweens()
        {
            _currentSequence?.Kill();

            if (targetIcon) targetIcon.DOKill();
            if (iconImageComponent) iconImageComponent.DOKill();
            if (liquidBase3D) liquidBase3D.DOKill();
        }
    }
}
