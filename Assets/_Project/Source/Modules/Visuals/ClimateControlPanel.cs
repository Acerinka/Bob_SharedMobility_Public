using TMPro;
using UnityEngine;

namespace Bob.SharedMobility
{
    public class ClimateControlPanel : MonoBehaviour
    {
        [Header("Temperature Labels")]
        public TMP_Text leftTempText;
        public TMP_Text rightTempText;

        [Header("Temperature Range")]
        public int minTemp = 16;
        public int maxTemp = 30;

        private int _currentLeft = 24;
        private int _currentRight = 24;

        private void Start()
        {
            UpdateUI();
        }

        public void IncreaseLeft()
        {
            SetLeftTemperature(_currentLeft + 1);
        }

        public void DecreaseLeft()
        {
            SetLeftTemperature(_currentLeft - 1);
        }

        public void IncreaseRight()
        {
            SetRightTemperature(_currentRight + 1);
        }

        public void DecreaseRight()
        {
            SetRightTemperature(_currentRight - 1);
        }

        private void SetLeftTemperature(int value)
        {
            _currentLeft = Mathf.Clamp(value, minTemp, maxTemp);
            UpdateUI();
        }

        private void SetRightTemperature(int value)
        {
            _currentRight = Mathf.Clamp(value, minTemp, maxTemp);
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (leftTempText) leftTempText.text = _currentLeft.ToString();
            if (rightTempText) rightTempText.text = _currentRight.ToString();
        }
    }
}
