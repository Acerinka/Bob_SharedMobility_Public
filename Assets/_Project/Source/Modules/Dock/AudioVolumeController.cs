using UnityEngine;
using UnityEngine.UI;

namespace Bob.SharedMobility
{
    public class AudioVolumeController : MonoBehaviour
    {
        [Header("References")]
        public Slider volumeSlider;

        [Header("Settings")]
        public float stepAmount = 0.1f;

        private void Awake()
        {
            if (volumeSlider)
            {
                volumeSlider.SetValueWithoutNotify(AudioListener.volume);
                volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }

        public void OnSliderValueChanged(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
        }

        public void IncreaseVolume()
        {
            SetVolume(AudioListener.volume + stepAmount);
        }

        public void DecreaseVolume()
        {
            SetVolume(AudioListener.volume - stepAmount);
        }

        private void SetVolume(float value)
        {
            float clampedValue = Mathf.Clamp01(value);
            AudioListener.volume = clampedValue;

            if (volumeSlider)
            {
                volumeSlider.SetValueWithoutNotify(clampedValue);
            }
        }
    }
}
