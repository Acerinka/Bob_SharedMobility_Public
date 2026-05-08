using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem; // 引用 InputSystem

namespace Bob.SharedMobility
{
    [RequireComponent(typeof(AudioSource))]
    public class LaneAssistScenarioController : MonoBehaviour
    {
        [Header("--- 核心引用 ---")]
        public BobController bob;
        public MapViewController mapController;
        
        [Header("--- UI 引用 ---")]
        public LaneControlDialog popupUI; 
        public GameObject successIcon1; 
        public GameObject successIcon2; 

        [Header("--- 🎮 触发按键设置 ---")]
        [Tooltip("键盘触发键 (默认 S)")]
        public KeyCode triggerKey = KeyCode.S;

        [Tooltip("手柄触发键 (如果想用手柄触发，请在这里选择按键，比如 ButtonNorth)")]
        public MyGamepadButton triggerGamepadBtn = MyGamepadButton.None;

        [Header("--- 🎵 音效 ---")]
        public AudioClip promptSound; 
        public AudioClip successSound; 

        private AudioSource _audioSource;
        private bool _isProcessing = false;
        private Vector3 _icon1Scale;
        private Vector3 _icon2Scale;

        void Awake()
        {
            if (successIcon1) _icon1Scale = successIcon1.transform.localScale;
            if (successIcon2) _icon2Scale = successIcon2.transform.localScale;
        }

        void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            if (successIcon1) { successIcon1.transform.localScale = Vector3.zero; successIcon1.SetActive(false); }
            if (successIcon2) { successIcon2.transform.localScale = Vector3.zero; successIcon2.SetActive(false); }
        }

        void Update()
        {
            if (mapController && mapController.currentState == MapViewController.ViewState.Full_Screen)
            {
                if (!_isProcessing)
                {
                    // 🔥 同时检测键盘 S 和手柄按键
                    bool keyboardHit = Input.GetKeyDown(triggerKey);
                    bool gamepadHit = CheckInput(triggerGamepadBtn);

                    if (keyboardHit || gamepadHit)
                    {
                        StartScenario();
                    }
                }
            }
        }

        // 简单的手柄检测辅助方法 (复制自 UI 脚本，保持独立性)
        private bool CheckInput(MyGamepadButton btn)
        {
            var gp = Gamepad.current;
            if (gp == null || btn == MyGamepadButton.None) return false;

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

        void StartScenario()
        {
            _isProcessing = true;
            Debug.Log("🎬 剧情开始");

            if (_audioSource && promptSound) _audioSource.PlayOneShot(promptSound);
            if (bob) bob.EnterReminderState();

            if (popupUI)
            {
                popupUI.Show((bool isActivated) => {
                    if (isActivated) OnUserActivate();
                    else OnUserSkip();
                });
            }
        }

        void OnUserActivate()
        {
            Debug.Log("✅ 用户 Activate");
            if (_audioSource && successSound) _audioSource.PlayOneShot(successSound);

            Sequence iconSeq = DOTween.Sequence();

            if (successIcon1)
            {
                iconSeq.AppendCallback(() => {
                    successIcon1.SetActive(true);
                    successIcon1.transform.localScale = Vector3.zero;
                    successIcon1.transform.DOScale(_icon1Scale, 0.5f).SetEase(Ease.OutBack);
                });
                iconSeq.AppendInterval(3.0f);
                iconSeq.Append(successIcon1.transform.DOScale(0, 0.3f));
                iconSeq.AppendCallback(() => successIcon1.SetActive(false));
            }

            if (successIcon2)
            {
                iconSeq.AppendCallback(() => {
                    successIcon2.SetActive(true);
                    successIcon2.transform.localScale = Vector3.zero;
                    successIcon2.transform.DOScale(_icon2Scale, 0.5f).SetEase(Ease.OutBack);
                });
                iconSeq.AppendInterval(3.0f);
                iconSeq.Append(successIcon2.transform.DOScale(0, 0.3f));
                iconSeq.AppendCallback(() => successIcon2.SetActive(false));
            }

            iconSeq.OnComplete(EndScenario);
        }

        void OnUserSkip()
        {
            Debug.Log("❌ 用户 Skip");
            EndScenario();
        }

        void EndScenario()
        {
            if (bob) bob.ExitReminderState();
            _isProcessing = false;
        }
    }
}