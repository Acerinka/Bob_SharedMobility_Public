using UnityEngine;
using DG.Tweening;

public class DockMenuController : MonoBehaviour
{
    [Header("--- 🔗 自身配置 ---")]
    [Tooltip("我是属于哪个 Dock 图标的？(用于关闭时复位图标)")]
    public DockItem myDockItem;

    [Header("--- 🤖 Bob 交互配置 ---")]
    [Tooltip("🔥 勾选此项，打开这个菜单时，Bob 会自动向下躲避")]
    public bool causesBobAvoid = true; 

    [Header("--- 📺 我的面板 ---")]
    [Tooltip("我的二级菜单 (比如 Settings 面板)")]
    public CanvasGroup myLevel2Panel; 
    
    // 记录当前打开的三级菜单
    private CanvasGroup _currentActiveLevel3; 

    [Header("--- ⚙️ 动画设置 ---")]
    public float fadeDuration = 0.3f;

    void Start()
    {
        HidePanelImmediate(myLevel2Panel);
    }

    // --- 🟢 供指挥官调用的入口 ---

    public void OpenLevel2Menu()
    {
        // 如果有残留的三级菜单，先关掉
        if (_currentActiveLevel3 != null) 
        {
            HidePanelImmediate(_currentActiveLevel3);
            _currentActiveLevel3 = null;
        }
        
        OpenPanelAnim(myLevel2Panel);
    }

    // --- 🟢 供 UI 按钮调用的逻辑 (三级跳转) ---

    // 打开指定的三级菜单 (拖拽赋值)
    public void OpenSpecificLevel3(CanvasGroup targetSubPanel)
    {
        if (targetSubPanel == null) return;

        _currentActiveLevel3 = targetSubPanel;
        ClosePanelAnim(myLevel2Panel); // 关二级
        OpenPanelAnim(targetSubPanel); // 开三级
    }

    // 返回二级
    public void BackToLevel2()
    {
        if (_currentActiveLevel3 != null)
        {
            ClosePanelAnim(_currentActiveLevel3);
            _currentActiveLevel3 = null;
        }
        OpenPanelAnim(myLevel2Panel);
    }

    // --- 🔴 供指挥官调用的“强制关机” ---
    // 这个方法会被 GlobalDockManager 调用
    public void CloseEntireApp()
    {
        // 1. 关掉我的二级
        ClosePanelAnim(myLevel2Panel);
        
        // 2. 关掉我的三级 (如果有)
        if (_currentActiveLevel3 != null)
        {
            ClosePanelAnim(_currentActiveLevel3);
            _currentActiveLevel3 = null;
        }

        // 3. 复位我的 Dock 图标 (变小、缩回水)
        if (myDockItem) myDockItem.ResetState();
    }

    // --- ❌ UI 上的关闭按钮专用 ---
    // 当你点击 X 号时调用这个
    public void OnCloseButtonClicked()
    {
        // 告诉指挥官：“我要关了，你可以清空记录了”
        if (GlobalDockManager.Instance)
            GlobalDockManager.Instance.CloseCurrentApp();
    }

    // --- 📦 动画工具 ---
    void OpenPanelAnim(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.gameObject.SetActive(true);
        cg.alpha = 0;
        cg.transform.localScale = Vector3.one * 0.9f; 
        cg.DOFade(1, fadeDuration);
        cg.transform.DOScale(1f, fadeDuration).SetEase(Ease.OutBack);
        cg.blocksRaycasts = true; 
    }

    void ClosePanelAnim(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.blocksRaycasts = false; 
        cg.DOFade(0, fadeDuration * 0.8f).OnComplete(() => {
            cg.gameObject.SetActive(false);
        });
    }

    void HidePanelImmediate(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0;
        cg.blocksRaycasts = false;
        cg.gameObject.SetActive(false);
    }
}