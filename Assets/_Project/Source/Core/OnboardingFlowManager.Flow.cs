using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class OnboardingFlowManager
    {
        private IEnumerator RunIntroRoutine()
        {
            PlayLongAudio(introAudio);

            yield return PlayStep(introStep1);
            yield return PlayStep(introStep2);

            ShowIntroChoiceButtons(true);
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
    }
}
