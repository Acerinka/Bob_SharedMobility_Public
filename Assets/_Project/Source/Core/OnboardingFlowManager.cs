using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    [RequireComponent(typeof(AudioSource))]
    public partial class OnboardingFlowManager : MonoBehaviour
    {
        private const string BobMoveTweenId = "OnboardingFlow.BobMove";

        [System.Serializable]
        public class SequenceStep
        {
            [Header("Content")]
            public Sprite pageImage;
            public float duration = 4.0f;

            [Header("Bob")]
            public Transform bobPos;
            public BobController.BobActionType action;
        }

        [System.Serializable]
        public class ChapterConfig
        {
            public List<AudioClip> audioPlaylist;
            public List<SequenceStep> steps;
        }

        [System.Serializable]
        public class SubPageData
        {
            public string name;
            public GameObject pageObj;
            public Transform bobPos;
            public AudioClip voice;
        }

        [Header("References")]
        public BobController bob;
        public GameObject mainAppSystem;
        public GameObject introCanvas;
        public AudioSource bgmSource;

        [Header("Welcome")]
        public GameObject panelWelcome;
        public Transform posWelcome;
        public AudioClip audioWelcome;

        [Header("First Check")]
        public GameObject panelFirstCheckRoot;
        public GameObject pageMain;
        public Transform posMain;
        public AudioClip audioFirstCheckMain;
        public List<SubPageData> subPages;

        [Header("Onboarding")]
        public GameObject panelOnboarding;
        public Image displayImage;

        [Header("Intro")]
        public SequenceStep introStep1;
        public SequenceStep introStep2;
        public GameObject introButtonGroup;
        public AudioClip introAudio;

        [Header("Skip Chapter")]
        public bool skipChoiceEndsIntroImmediately = true;
        public ChapterConfig skipChapter;

        [Header("Yes Chapter")]
        public ChapterConfig yesChapter;

        private AudioSource _sfxAudio;
        private Coroutine _currentRoutine;
        private Coroutine _audioRoutine;

        private void Awake()
        {
            _sfxAudio = GetComponent<AudioSource>();

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }

            ApplyColdBootVisibilityState();
        }

        private IEnumerator Start()
        {
            yield return null;
            FullRestart();
        }

        private void Update()
        {
            if (ProjectInput.WasRestartPressed())
            {
                FullRestart();
                return;
            }

            if (panelWelcome != null
                && panelWelcome.activeSelf
                && (ProjectInput.WasPrimaryPointerPressed() || ProjectInput.WasPrimaryActionPressed()))
            {
                OnClick_StartFirstCheck();
            }
        }

        public void FullRestart()
        {
            StopActiveRoutines();
            KillFlowTweens();

            bgmSource.Stop();
            if (_sfxAudio) _sfxAudio.Stop();

            if (introCanvas) introCanvas.SetActive(true);
            if (mainAppSystem) mainAppSystem.SetActive(false);

            HideAll();
            ResetBobForWelcome();
            EnterWelcome();
        }

        public void OnClick_StartFirstCheck()
        {
            EnterFirstCheckMain();
        }

        public void OnClick_OpenSubPage(int index)
        {
            if (pageMain) pageMain.SetActive(false);
            SetSubPagesActive(false);

            if (subPages == null || index < 0 || index >= subPages.Count)
            {
                return;
            }

            SubPageData data = subPages[index];
            if (data.pageObj) data.pageObj.SetActive(true);

            MoveBob(data.bobPos);
            PlaySFX(data.voice);

            if (bob && data.bobPos != null)
            {
                bob.TriggerAction(BobController.BobActionType.PulseLight);
            }
        }

        public void OnClick_NextSubPage(int currentIndex)
        {
            int nextIndex = currentIndex + 1;
            if (subPages != null && nextIndex < subPages.Count)
            {
                OnClick_OpenSubPage(nextIndex);
                return;
            }

            OnClick_FinishFirstCheck();
        }

        public void OnClick_BackToCheckMain()
        {
            EnterFirstCheckMain();
        }

        public void OnClick_FinishFirstCheck()
        {
            StartOnboardingIntro();
        }

        public void OnClick_SkipFirstCheck()
        {
            StartOnboardingIntro();
        }

        public void StartOnboardingIntro()
        {
            HideAll();
            if (panelOnboarding) panelOnboarding.SetActive(true);
            ShowIntroChoiceButtons(false);

            StopCurrentRoutine();
            _currentRoutine = StartCoroutine(RunIntroRoutine());
        }

        public void OnClick_ChooseSkip()
        {
            if (skipChoiceEndsIntroImmediately)
            {
                CompleteIntroImmediately();
                return;
            }

            StartChapter(skipChapter);
        }

        public void OnClick_ChooseYes()
        {
            StartChapter(yesChapter);
        }
    }
}
