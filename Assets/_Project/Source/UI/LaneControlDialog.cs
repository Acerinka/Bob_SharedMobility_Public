using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using UnityEngine.InputSystem; 

namespace Bob.SharedMobility
{
    // 🔥 定义手柄按键枚举，方便在 Inspector 选择
    public enum MyGamepadButton
    {
        None,
        ButtonSouth, // Xbox A / PS Cross
        ButtonEast,  // Xbox B / PS Circle
        ButtonWest,  // Xbox X / PS Square
        ButtonNorth, // Xbox Y / PS Triangle
        DpadUp,
        DpadDown,
        DpadLeft,
        DpadRight,
        LeftShoulder,
        RightShoulder
    }

    public class LaneControlDialog : MonoBehaviour
    {
        [Header("--- 引用 ---")]
        public CanvasGroup panelGroup; 
        public Button activateBtn;     
        public Button skipBtn;

        [Header("--- 🎮 手柄按键绑定 ---")]
        [Tooltip("确认键 (默认填 ButtonSouth)")]
        public MyGamepadButton activateInput = MyGamepadButton.ButtonSouth;
        
        [Tooltip("取消/跳过键 (默认填 ButtonWest)")]
        public MyGamepadButton skipInput = MyGamepadButton.ButtonWest;

        private Action<bool> _callback; 
        private RectTransform _rect;
        private Vector3 _initialScale; 

        void Awake()
        {
            if (panelGroup) 
            {
                _rect = panelGroup.GetComponent<RectTransform>();
                _initialScale = panelGroup.transform.localScale;
            }
        }

        void Start()
        {
            if (panelGroup)
            {
                panelGroup.alpha = 0;
                if (_rect) _rect.localScale = Vector3.zero; 
                panelGroup.gameObject.SetActive(false);
            }

            activateBtn.onClick.AddListener(() => OnClick(true));
            skipBtn.onClick.AddListener(() => OnClick(false));
        }

        void Update()
        {
            // 只有弹窗显示时才检测
            if (panelGroup.gameObject.activeSelf && panelGroup.alpha > 0.5f)
            {
                if (Gamepad.current != null)
                {
                    // 🔥 动态检测你在 Inspector 里选的键
                    if (CheckInput(activateInput)) 
                    {
                        Debug.Log($"🎮 手柄触发确认: {activateInput}");
                        OnClick(true);
                    }
                    
                    if (CheckInput(skipInput)) 
                    {
                        Debug.Log($"🎮 手柄触发跳过: {skipInput}");
                        OnClick(false);
                    }
                }

                // 键盘调试备份
                if (Input.GetKeyDown(KeyCode.Return)) OnClick(true);
                if (Input.GetKeyDown(KeyCode.Escape)) OnClick(false);
            }
        }

        // 🔥 通用按键检测逻辑
        private bool CheckInput(MyGamepadButton btn)
        {
            var gp = Gamepad.current;
            if (gp == null) return false;

            switch (btn)
            {
                case MyGamepadButton.ButtonSouth: return gp.buttonSouth.wasPressedThisFrame;
                case MyGamepadButton.ButtonEast: return gp.buttonEast.wasPressedThisFrame;
                case MyGamepadButton.ButtonWest: return gp.buttonWest.wasPressedThisFrame;
                case MyGamepadButton.ButtonNorth: return gp.buttonNorth.wasPressedThisFrame;
                case MyGamepadButton.DpadUp: return gp.dpad.up.wasPressedThisFrame;
                case MyGamepadButton.DpadDown: return gp.dpad.down.wasPressedThisFrame;
                case MyGamepadButton.DpadLeft: return gp.dpad.left.wasPressedThisFrame;
                case MyGamepadButton.DpadRight: return gp.dpad.right.wasPressedThisFrame;
                case MyGamepadButton.LeftShoulder: return gp.leftShoulder.wasPressedThisFrame;
                case MyGamepadButton.RightShoulder: return gp.rightShoulder.wasPressedThisFrame;
                default: return false;
            }
        }

        public void Show(Action<bool> onChoiceMade)
        {
            _callback = onChoiceMade;

            if (panelGroup)
            {
                panelGroup.gameObject.SetActive(true);
                panelGroup.blocksRaycasts = true; 
                panelGroup.alpha = 0;
                if (_rect) _rect.localScale = Vector3.zero;

                panelGroup.DOFade(1, 0.4f);
                if (_rect) _rect.DOScale(_initialScale, 0.4f).SetEase(Ease.OutBack);
            }
        }

        public void Hide()
        {
            if (panelGroup)
            {
                panelGroup.blocksRaycasts = false;
                panelGroup.DOFade(0, 0.3f);
                if (_rect) 
                {
                    _rect.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
                         .OnComplete(() => panelGroup.gameObject.SetActive(false));
                }
            }
        }

        void OnClick(bool isActivate)
        {
            if (_callback != null) _callback(isActivate);
            Hide(); 
        }
    }
}