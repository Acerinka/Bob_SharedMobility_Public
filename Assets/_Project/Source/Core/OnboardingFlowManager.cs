using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

namespace Bob.SharedMobility
{
    [RequireComponent(typeof(AudioSource))]
    public class OnboardingFlowManager : MonoBehaviour
    {
        // ==========================================
        // 🏗️ 数据结构定义
        // ==========================================
        [System.Serializable]
        public class SequenceStep
        {
            [Header("--- 画面 ---")]
            public Sprite pageImage;      
            public float duration = 4.0f; 
            [Header("--- Bob 导演 ---")]
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
            public string name; public GameObject pageObj; public Transform bobPos; public AudioClip voice;       
        }

        // ==========================================
        // 🔗 引用
        // ==========================================
        [Header("--- 0. 全局引用 ---")]
        public BobController bob;
        public GameObject mainAppSystem; 
        public GameObject introCanvas;   
        public AudioSource bgmSource; 

        [Header("--- 1. Welcome 阶段 ---")]
        public GameObject panelWelcome;
        public Transform posWelcome;
        public AudioClip audioWelcome;
        // float autoStartDelay 已移除

        [Header("--- 2. First Check 阶段 ---")]
        public GameObject panelFirstCheckRoot;
        public GameObject pageMain;
        public Transform posMain;
        public AudioClip audioFirstCheckMain; 
        public List<SubPageData> subPages; 

        [Header("--- 3. Onboarding 系统 ---")]
        public GameObject panelOnboarding;
        public Image displayImage;        
        
        [Header(">>> 3.1 Intro (2页)")]
        public SequenceStep introStep1; 
        public SequenceStep introStep2; 
        public GameObject introButtonGroup; 
        public AudioClip introAudio;        

        [Header(">>> 3.2 Skip 分支 (3页)")]
        public ChapterConfig skipChapter;

        [Header(">>> 3.3 Yes 分支 (13页)")]
        public ChapterConfig yesChapter;

        private AudioSource _sfxAudio; 
        private Tween _welcomeTimer;
        private Coroutine _currentRoutine;

        // ==========================================
        // 🏁 启动与更新
        // ==========================================
        
        IEnumerator Start()
        {
            _sfxAudio = GetComponent<AudioSource>();
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();

            yield return null; 

            FullRestart();
        }

        void Update()
        {
            // 1. 全局重启 (手柄 Start / 键盘 R / 手柄 B)
            bool restartInput = false;
            if (Gamepad.current != null)
            {
                if (Gamepad.current.startButton.wasPressedThisFrame) restartInput = true;
                if (Gamepad.current.buttonEast.wasPressedThisFrame) restartInput = true; // B键也重启
            }
            if (Input.GetKeyDown(KeyCode.R)) restartInput = true;

            if (restartInput)
            {
                FullRestart();
                return;
            }

            // 2. Welcome 点击进入
            if (panelWelcome != null && panelWelcome.activeSelf)
            {
                bool clicked = false;
                if (Input.GetMouseButtonDown(0)) clicked = true;
                if (Input.GetKeyDown(KeyCode.Return)) clicked = true;
                if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) clicked = true;

                if (clicked)
                {
                    OnClick_StartFirstCheck();
                }
            }
        }

        // ==========================================
        // 🔥🔥🔥 全局重置
        // ==========================================
        public void FullRestart()
        {
            StopAllCoroutines();
            if (_currentRoutine != null) StopCoroutine(_currentRoutine);
            _currentRoutine = null;

            if (_welcomeTimer != null) _welcomeTimer.Kill();
            DOTween.KillAll(); 

            if (bgmSource) bgmSource.Stop();
            if (_sfxAudio) _sfxAudio.Stop();

            if (introCanvas) introCanvas.SetActive(true);
            if (mainAppSystem) mainAppSystem.SetActive(false); 
            HideAll(); 
            
            if (bob) 
            {
                // 重启时先强制显示，重置状态
                bob.gameObject.SetActive(true); 
                bob.transform.localScale = Vector3.one; 
                bob.ResetState(); 
                bob.ChangeSkin(0); 

                // 如果 Welcome 有位置就去，没有就隐藏
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

            EnterWelcome();
        }

        // ==========================================
        // 1️⃣ Welcome 阶段
        // ==========================================
        void EnterWelcome()
        {
            HideAll();
            if(panelWelcome) panelWelcome.SetActive(true);
            
            // 此时根据 posWelcome 是否为空决定 Bob 去留
            MoveBob(posWelcome);
            PlaySFX(audioWelcome); 

            if (_welcomeTimer != null) _welcomeTimer.Kill();
        }

        public void OnClick_StartFirstCheck()
        {
            if (_welcomeTimer != null) _welcomeTimer.Kill();
            EnterFirstCheckMain();
        }

        // ==========================================
        // 2️⃣ First Check 阶段
        // ==========================================
        void EnterFirstCheckMain()
        {
            if (_welcomeTimer != null) _welcomeTimer.Kill();
            HideAll(); 
            if(panelFirstCheckRoot) panelFirstCheckRoot.SetActive(true);
            foreach (var sub in subPages) if (sub.pageObj) sub.pageObj.SetActive(false);
            if(pageMain) pageMain.SetActive(true); 
            
            // 🔥 这里会读取 posMain，如果是 None，Bob 就会消失
            MoveBob(posMain);
            PlaySFX(audioFirstCheckMain); 
        }

        public void OnClick_OpenSubPage(int index)
        {
            if(pageMain) pageMain.SetActive(false); 
            foreach (var sub in subPages) if (sub.pageObj) sub.pageObj.SetActive(false);

            if (index >= 0 && index < subPages.Count)
            {
                var data = subPages[index];
                if(data.pageObj) data.pageObj.SetActive(true); 
                
                MoveBob(data.bobPos);
                PlaySFX(data.voice); 
                
                if(bob && data.bobPos != null) bob.TriggerAction(BobController.BobActionType.PulseLight);
            }
        }

        public void OnClick_NextSubPage(int currentIndex)
        {
            int nextIndex = currentIndex + 1;
            if (nextIndex < subPages.Count) OnClick_OpenSubPage(nextIndex);
            else OnClick_FinishFirstCheck();
        }

        public void OnClick_BackToCheckMain() { EnterFirstCheckMain(); }
        public void OnClick_FinishFirstCheck() { StartOnboardingIntro(); }
        public void OnClick_SkipFirstCheck() { StartOnboardingIntro(); }

        // ==========================================
        // 3️⃣ Onboarding 阶段
        // ==========================================
        public void StartOnboardingIntro()
        {
            HideAll();
            if (panelOnboarding) panelOnboarding.SetActive(true);
            if (introButtonGroup) introButtonGroup.SetActive(false); 
            if (_currentRoutine != null) StopCoroutine(_currentRoutine);
            _currentRoutine = StartCoroutine(RunIntroRoutine());
        }

        IEnumerator RunIntroRoutine()
        {
            PlayLongAudio(introAudio);
            ApplyStep(introStep1);
            yield return new WaitForSeconds(introStep1.duration);
            ApplyStep(introStep2);
            
            if (introButtonGroup) 
            {
                introButtonGroup.SetActive(true);
                introButtonGroup.transform.localScale = Vector3.zero;
                introButtonGroup.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            }
        }

        public void OnClick_ChooseSkip()
        {
            if (introButtonGroup) introButtonGroup.SetActive(false);
            if (_currentRoutine != null) StopCoroutine(_currentRoutine);
            _currentRoutine = StartCoroutine(RunChapterRoutine(skipChapter));
        }

        public void OnClick_ChooseYes()
        {
            if (introButtonGroup) introButtonGroup.SetActive(false);
            if (_currentRoutine != null) StopCoroutine(_currentRoutine);
            _currentRoutine = StartCoroutine(RunChapterRoutine(yesChapter));
        }

        IEnumerator RunChapterRoutine(ChapterConfig chapter)
        {
            StartCoroutine(PlayAudioPlaylist(chapter.audioPlaylist));
            foreach (var step in chapter.steps)
            {
                ApplyStep(step);
                yield return new WaitForSeconds(step.duration);
            }
            EndIntroFlow();
        }

        IEnumerator PlayAudioPlaylist(List<AudioClip> clips)
        {
            bgmSource.Stop();
            foreach (var clip in clips)
            {
                if (clip != null)
                {
                    bgmSource.clip = clip;
                    bgmSource.Play();
                    yield return new WaitForSeconds(clip.length);
                }
            }
        }

        void ApplyStep(SequenceStep step)
        {
            if (displayImage && step.pageImage) displayImage.sprite = step.pageImage;
            MoveBob(step.bobPos);
            if (bob && step.bobPos != null) bob.TriggerAction(step.action);
        }

        void EndIntroFlow()
        {
            if (introCanvas) introCanvas.SetActive(false);
            if (mainAppSystem) mainAppSystem.SetActive(true);
            if (bob) bob.ResetState();
            if (bgmSource) bgmSource.Stop();
        }

        void HideAll()
        {
            if (panelWelcome) panelWelcome.SetActive(false);
            if (panelFirstCheckRoot) panelFirstCheckRoot.SetActive(false);
            if (panelOnboarding) panelOnboarding.SetActive(false);
            if (pageMain) pageMain.SetActive(false);
            foreach (var p in subPages) if(p.pageObj) p.pageObj.SetActive(false);
        }

        // ==========================================
        // 🔥🔥 修复后的 MoveBob：支持隐身
        // ==========================================
        void MoveBob(Transform target)
        {
            if (!bob) return;

            // 如果目标是空 (None)，说明这一页 Bob 需要隐身
            if (target == null) 
            { 
                // 只是安静地隐藏，不再报错
                bob.gameObject.SetActive(false); 
                return; 
            }

            // 如果有目标，确保显示
            bob.gameObject.SetActive(true);
            DOTween.Kill(bob, "AppFlowMove");

            DOTween.To(() => bob.lastVanishedPos, x => bob.lastVanishedPos = x, target.position, 0.6f)
                   .SetEase(Ease.OutBack).SetId("AppFlowMove");

            bob.transform.DOLookAt(Camera.main.transform.position, 0.5f);
        }

        void PlaySFX(AudioClip clip) { if (_sfxAudio && clip) { _sfxAudio.Stop(); _sfxAudio.PlayOneShot(clip); } }
        void PlayLongAudio(AudioClip clip) { if (bgmSource && clip) { bgmSource.Stop(); bgmSource.clip = clip; bgmSource.Play(); } }
    }
}