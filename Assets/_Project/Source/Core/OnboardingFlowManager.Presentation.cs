using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class OnboardingFlowManager
    {
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

        private void EndIntroFlow()
        {
            if (introCanvas) introCanvas.SetActive(false);
            if (mainAppSystem) mainAppSystem.SetActive(true);
            if (bob) bob.ResetState();
            if (bgmSource) bgmSource.Stop();
        }

        private void CompleteIntroImmediately()
        {
            StopActiveRoutines();
            KillFlowTweens();
            if (_sfxAudio) _sfxAudio.Stop();
            EndIntroFlow();
        }

        private void HideAll()
        {
            if (panelWelcome) panelWelcome.SetActive(false);
            if (panelFirstCheckRoot) panelFirstCheckRoot.SetActive(false);
            if (panelOnboarding) panelOnboarding.SetActive(false);
            if (pageMain) pageMain.SetActive(false);
            SetSubPagesActive(false);
        }

        private void ApplyColdBootVisibilityState()
        {
            if (introCanvas) introCanvas.SetActive(true);
            if (mainAppSystem) mainAppSystem.SetActive(false);
            HideAll();
            if (panelWelcome) panelWelcome.SetActive(true);
            if (introButtonGroup) introButtonGroup.SetActive(false);
        }

        private void ShowIntroChoiceButtons(bool animated)
        {
            if (!introButtonGroup) return;

            bool wasActive = introButtonGroup.activeSelf;
            introButtonGroup.SetActive(true);
            introButtonGroup.transform.DOKill();

            if (!animated || wasActive)
            {
                introButtonGroup.transform.localScale = Vector3.one;
                return;
            }

            introButtonGroup.transform.localScale = Vector3.zero;
            introButtonGroup.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
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
