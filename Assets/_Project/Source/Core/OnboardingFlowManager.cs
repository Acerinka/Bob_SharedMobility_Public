using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    [RequireComponent(typeof(AudioSource))]
    public class OnboardingFlowManager : MonoBehaviour
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
        public ChapterConfig skipChapter;

        [Header("Yes Chapter")]
        public ChapterConfig yesChapter;

        private AudioSource _sfxAudio;
        private Coroutine _currentRoutine;
        private Coroutine _audioRoutine;

        private IEnumerator Start()
        {
            _sfxAudio = GetComponent<AudioSource>();

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }

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
            if (introButtonGroup) introButtonGroup.SetActive(false);

            StopCurrentRoutine();
            _currentRoutine = StartCoroutine(RunIntroRoutine());
        }

        public void OnClick_ChooseSkip()
        {
            StartChapter(skipChapter);
        }

        public void OnClick_ChooseYes()
        {
            StartChapter(yesChapter);
        }

        private void EnterWelcome()
        {
            HideAll();
            if (panelWelcome) panelWelcome.SetActive(true);

            MoveBob(posWelcome);
            PlaySFX(audioWelcome);
        }

        private void EnterFirstCheckMain()
        {
            HideAll();

            if (panelFirstCheckRoot) panelFirstCheckRoot.SetActive(true);
            SetSubPagesActive(false);
            if (pageMain) pageMain.SetActive(true);

            MoveBob(posMain);
            PlaySFX(audioFirstCheckMain);
        }

        private IEnumerator RunIntroRoutine()
        {
            PlayLongAudio(introAudio);

            yield return PlayStep(introStep1);
            yield return PlayStep(introStep2);

            if (introButtonGroup)
            {
                introButtonGroup.SetActive(true);
                introButtonGroup.transform.DOKill();
                introButtonGroup.transform.localScale = Vector3.zero;
                introButtonGroup.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            }
        }

        private void StartChapter(ChapterConfig chapter)
        {
            if (introButtonGroup) introButtonGroup.SetActive(false);

            StopCurrentRoutine();
            _currentRoutine = StartCoroutine(RunChapterRoutine(chapter));
        }

        private IEnumerator RunChapterRoutine(ChapterConfig chapter)
        {
            StopAudioRoutine();

            if (chapter == null)
            {
                EndIntroFlow();
                yield break;
            }

            _audioRoutine = StartCoroutine(PlayAudioPlaylist(chapter.audioPlaylist));

            if (chapter.steps != null)
            {
                foreach (SequenceStep step in chapter.steps)
                {
                    yield return PlayStep(step);
                }
            }

            EndIntroFlow();
        }

        private IEnumerator PlayAudioPlaylist(List<AudioClip> clips)
        {
            if (!bgmSource) yield break;

            if (bgmSource) bgmSource.Stop();

            if (clips == null) yield break;

            foreach (AudioClip clip in clips)
            {
                if (!clip) continue;

                bgmSource.clip = clip;
                bgmSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }

        private IEnumerator PlayStep(SequenceStep step)
        {
            if (step == null) yield break;

            ApplyStep(step);
            yield return new WaitForSeconds(Mathf.Max(0f, step.duration));
        }

        private void ApplyStep(SequenceStep step)
        {
            if (displayImage && step.pageImage)
            {
                displayImage.sprite = step.pageImage;
            }

            MoveBob(step.bobPos);

            if (bob && step.bobPos != null)
            {
                bob.TriggerAction(step.action);
            }
        }

        private void EndIntroFlow()
        {
            if (introCanvas) introCanvas.SetActive(false);
            if (mainAppSystem) mainAppSystem.SetActive(true);
            if (bob) bob.ResetState();
            if (bgmSource) bgmSource.Stop();
        }

        private void HideAll()
        {
            if (panelWelcome) panelWelcome.SetActive(false);
            if (panelFirstCheckRoot) panelFirstCheckRoot.SetActive(false);
            if (panelOnboarding) panelOnboarding.SetActive(false);
            if (pageMain) pageMain.SetActive(false);
            SetSubPagesActive(false);
        }

        private void SetSubPagesActive(bool isActive)
        {
            if (subPages == null) return;

            foreach (SubPageData page in subPages)
            {
                if (page.pageObj) page.pageObj.SetActive(isActive);
            }
        }

        private void MoveBob(Transform target)
        {
            if (!bob) return;

            if (target == null)
            {
                bob.gameObject.SetActive(false);
                return;
            }

            bob.gameObject.SetActive(true);
            DOTween.Kill(BobMoveTweenId);

            DOTween.To(() => bob.lastVanishedPos, x => bob.lastVanishedPos = x, target.position, 0.6f)
                .SetEase(Ease.OutBack)
                .SetId(BobMoveTweenId);

            if (SceneCameraProvider.TryGetUICamera(out Camera uiCamera, bob))
            {
                bob.transform.DOLookAt(uiCamera.transform.position, 0.5f).SetId(BobMoveTweenId);
            }
        }

        private void ResetBobForWelcome()
        {
            if (!bob) return;

            bob.gameObject.SetActive(true);
            bob.transform.localScale = Vector3.one;
            bob.ResetState();
            bob.ChangeSkin(0);

            if (posWelcome != null)
            {
                bob.transform.position = posWelcome.position;
                bob.lastVanishedPos = posWelcome.position;
            }
            else
            {
                bob.gameObject.SetActive(false);
            }
        }

        private void PlaySFX(AudioClip clip)
        {
            if (!_sfxAudio || !clip) return;

            _sfxAudio.Stop();
            _sfxAudio.PlayOneShot(clip);
        }

        private void PlayLongAudio(AudioClip clip)
        {
            if (!bgmSource || !clip) return;

            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.Play();
        }

        private void StopActiveRoutines()
        {
            StopCurrentRoutine();
            StopAudioRoutine();
        }

        private void StopCurrentRoutine()
        {
            if (_currentRoutine == null) return;

            StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }

        private void StopAudioRoutine()
        {
            if (_audioRoutine == null) return;

            StopCoroutine(_audioRoutine);
            _audioRoutine = null;
        }

        private void KillFlowTweens()
        {
            DOTween.Kill(BobMoveTweenId);

            if (introButtonGroup)
            {
                introButtonGroup.transform.DOKill();
            }
        }
    }
}
