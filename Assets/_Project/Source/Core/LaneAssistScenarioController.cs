using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    [RequireComponent(typeof(AudioSource))]
    public class LaneAssistScenarioController : MonoBehaviour
    {
        [Header("References")]
        public BobController bob;
        public MapViewController mapController;
        public LaneControlDialog popupUI;
        public GameObject successIcon1;
        public GameObject successIcon2;

        [Header("Trigger")]
        public KeyCode triggerKey = KeyCode.S;
        public MyGamepadButton triggerGamepadBtn = MyGamepadButton.None;

        [Header("Audio")]
        public AudioClip promptSound;
        public AudioClip successSound;

        private AudioSource _audioSource;
        private bool _isProcessing;
        private Vector3 _icon1Scale;
        private Vector3 _icon2Scale;

        private void Awake()
        {
            if (successIcon1) _icon1Scale = successIcon1.transform.localScale;
            if (successIcon2) _icon2Scale = successIcon2.transform.localScale;
        }

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            HideSuccessIcon(successIcon1);
            HideSuccessIcon(successIcon2);
        }

        private void Update()
        {
            if (_isProcessing || mapController == null || mapController.currentState != MapViewController.ViewState.Full_Screen)
            {
                return;
            }

            bool keyboardHit = Input.GetKeyDown(triggerKey);
            bool gamepadHit = GamepadButtonReader.WasPressedThisFrame(triggerGamepadBtn);

            if (keyboardHit || gamepadHit)
            {
                StartScenario();
            }
        }

        private void StartScenario()
        {
            _isProcessing = true;

            if (_audioSource && promptSound) _audioSource.PlayOneShot(promptSound);
            if (bob) bob.EnterReminderState();

            if (popupUI)
            {
                popupUI.Show(isActivated =>
                {
                    if (isActivated) OnUserActivate();
                    else OnUserSkip();
                });
                return;
            }

            EndScenario();
        }

        private void OnUserActivate()
        {
            if (_audioSource && successSound) _audioSource.PlayOneShot(successSound);

            if (!successIcon1 && !successIcon2)
            {
                EndScenario();
                return;
            }

            Sequence iconSeq = DOTween.Sequence();
            AppendSuccessIconSequence(iconSeq, successIcon1, _icon1Scale);
            AppendSuccessIconSequence(iconSeq, successIcon2, _icon2Scale);
            iconSeq.OnComplete(EndScenario);
        }

        private void OnUserSkip()
        {
            EndScenario();
        }

        private void EndScenario()
        {
            if (bob) bob.ExitReminderState();
            _isProcessing = false;
        }

        private static void HideSuccessIcon(GameObject icon)
        {
            if (!icon) return;

            icon.transform.localScale = Vector3.zero;
            icon.SetActive(false);
        }

        private static void AppendSuccessIconSequence(Sequence sequence, GameObject icon, Vector3 targetScale)
        {
            if (!icon) return;

            sequence.AppendCallback(() =>
            {
                icon.SetActive(true);
                icon.transform.localScale = Vector3.zero;
                icon.transform.DOScale(targetScale, 0.5f).SetEase(Ease.OutBack);
            });
            sequence.AppendInterval(3.0f);
            sequence.Append(icon.transform.DOScale(0f, 0.3f));
            sequence.AppendCallback(() => icon.SetActive(false));
        }
    }
}
