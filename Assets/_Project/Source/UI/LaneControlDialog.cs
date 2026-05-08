using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    public enum MyGamepadButton
    {
        None,
        ButtonSouth,
        ButtonEast,
        ButtonWest,
        ButtonNorth,
        DpadUp,
        DpadDown,
        DpadLeft,
        DpadRight,
        LeftShoulder,
        RightShoulder
    }

    public class LaneControlDialog : MonoBehaviour
    {
        [Header("References")]
        public CanvasGroup panelGroup;
        public Button activateBtn;
        public Button skipBtn;

        [Header("Gamepad")]
        public MyGamepadButton activateInput = MyGamepadButton.ButtonSouth;
        public MyGamepadButton skipInput = MyGamepadButton.ButtonWest;

        private Action<bool> _callback;
        private RectTransform _rect;
        private Vector3 _initialScale;

        private void Awake()
        {
            if (panelGroup)
            {
                _rect = panelGroup.GetComponent<RectTransform>();
                _initialScale = panelGroup.transform.localScale;
            }
        }

        private void Start()
        {
            if (panelGroup)
            {
                panelGroup.alpha = 0f;
                if (_rect) _rect.localScale = Vector3.zero;
                panelGroup.gameObject.SetActive(false);
            }

            if (activateBtn) activateBtn.onClick.AddListener(() => Choose(true));
            if (skipBtn) skipBtn.onClick.AddListener(() => Choose(false));
        }

        private void Update()
        {
            if (!IsVisible()) return;

            if (GamepadButtonReader.WasPressedThisFrame(activateInput) || ProjectInput.WasKeyPressed(KeyCode.Return))
            {
                Choose(true);
            }

            if (GamepadButtonReader.WasPressedThisFrame(skipInput) || ProjectInput.WasKeyPressed(KeyCode.Escape))
            {
                Choose(false);
            }
        }

        public void Show(Action<bool> onChoiceMade)
        {
            _callback = onChoiceMade;

            if (!panelGroup) return;

            panelGroup.DOKill();
            if (_rect) _rect.DOKill();

            panelGroup.gameObject.SetActive(true);
            panelGroup.blocksRaycasts = true;
            panelGroup.alpha = 0f;
            if (_rect) _rect.localScale = Vector3.zero;

            panelGroup.DOFade(1f, 0.4f);
            if (_rect) _rect.DOScale(_initialScale, 0.4f).SetEase(Ease.OutBack);
        }

        public void Hide()
        {
            if (!panelGroup) return;

            panelGroup.DOKill();
            if (_rect) _rect.DOKill();

            panelGroup.blocksRaycasts = false;
            Tween fadeTween = panelGroup.DOFade(0f, 0.3f);

            if (_rect)
            {
                _rect.DOScale(Vector3.zero, 0.3f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => panelGroup.gameObject.SetActive(false));
            }
            else
            {
                fadeTween.OnComplete(() => panelGroup.gameObject.SetActive(false));
            }
        }

        private bool IsVisible()
        {
            return panelGroup != null && panelGroup.gameObject.activeSelf && panelGroup.alpha > 0.5f;
        }

        private void Choose(bool isActivate)
        {
            Action<bool> callback = _callback;
            _callback = null;

            callback?.Invoke(isActivate);
            Hide();
        }
    }
}
