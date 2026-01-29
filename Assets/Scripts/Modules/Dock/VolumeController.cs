using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [Header("--- 🎚️ 组件引用 ---")]
    public Slider volumeSlider;

    [Header("--- ⚙️ 参数设置 ---")]
    public float stepAmount = 0.1f; // 每次按按钮增加多少 (0.1 = 10%)

    // 当 Slider 值改变时调用 (可以在这里连接真实的系统音量)
    public void OnSliderValueChanged(float value)
    {
        // 比如: AudioListener.volume = value;
        Debug.Log($"当前音量: {value * 100}%");
    }

    // --- 🔘 按钮功能 ---

    public void IncreaseVolume()
    {
        if (volumeSlider) volumeSlider.value += stepAmount;
    }

    public void DecreaseVolume()
    {
        if (volumeSlider) volumeSlider.value -= stepAmount;
    }
}