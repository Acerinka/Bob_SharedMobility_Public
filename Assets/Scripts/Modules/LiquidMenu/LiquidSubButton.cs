using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class LiquidSubButton : MonoBehaviour
{
    [Header("--- 🖱️ 子按钮配置 ---")]
    [Tooltip("点击这个区域触发的具体功能")]
    public UnityEvent onClick;

    [Tooltip("点击时是否需要简单的缩放反馈？")]
    public bool enableVisualFeedback = true;

    // 当被 Manager 点击时调用
    public void OnSubButtonClick()
    {
        // 1. 视觉反馈 (Q弹一下)
        if (enableVisualFeedback)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.2f);
        }

        // 2. 执行事件
        onClick.Invoke();
    }
}