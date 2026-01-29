using UnityEngine;
using TMPro; // 如果你用的是 TextMeshPro，必须引用这个
using UnityEngine.UI;

public class ACController : MonoBehaviour
{
    [Header("--- 🌡️ 温度显示 ---")]
    public TMP_Text leftTempText;  // 左边温度文字 (如果是旧版Text组件，就把 TMP_Text 改成 Text)
    public TMP_Text rightTempText; // 右边温度文字

    [Header("--- ⚙️ 参数 ---")]
    public int minTemp = 16; // 最低温
    public int maxTemp = 30; // 最高温

    // 内部记录当前的温度值
    private int _currentLeft = 24;
    private int _currentRight = 24;

    void Start()
    {
        UpdateUI();
    }

    // --- 🔘 左边控制 ---
    public void IncreaseLeft()
    {
        if (_currentLeft < maxTemp)
        {
            _currentLeft++;
            UpdateUI();
        }
    }

    public void DecreaseLeft()
    {
        if (_currentLeft > minTemp)
        {
            _currentLeft--;
            UpdateUI();
        }
    }

    // --- 🔘 右边控制 ---
    public void IncreaseRight()
    {
        if (_currentRight < maxTemp)
        {
            _currentRight++;
            UpdateUI();
        }
    }

    public void DecreaseRight()
    {
        if (_currentRight > minTemp)
        {
            _currentRight--;
            UpdateUI();
        }
    }

    // 更新文字显示
    void UpdateUI()
    {
        if(leftTempText) leftTempText.text = _currentLeft.ToString();
        if(rightTempText) rightTempText.text = _currentRight.ToString();
    }
}