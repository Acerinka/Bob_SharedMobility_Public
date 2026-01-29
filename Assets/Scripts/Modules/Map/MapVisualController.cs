using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class MapVisualController : MonoBehaviour
{
    public enum ViewState { Small_Icon, Medium_Screen, Full_Screen }
    public ViewState currentState = ViewState.Small_Icon;

    [System.Serializable]
    public struct ViewStateConfig
    {
        public string stateName; 
        public List<GameObject> objectsToShow;
        public List<GameObject> objectsToHide;
    }

    [Header("--- 🧱 物理碰撞体控制 (修复点击范围) ---")]
    [Tooltip("请把挂着 MapGestureController 的那个物体上的 BoxCollider 拖进来")]
    public BoxCollider targetCollider; 
    
    // 下面这些数值你需要自己在 Inspector 里调，对着 Scene 里的绿框调
    public Vector3 smallColliderSize = new Vector3(0.2f, 0.2f, 0.1f);
    public Vector3 mediumColliderSize = new Vector3(1.5f, 1.0f, 0.1f);
    public Vector3 fullColliderSize = new Vector3(3.0f, 1.5f, 0.1f);

    [Header("--- 1. 尺寸精细控制 ---")]
    public Vector3 startScale = Vector3.one; 
    public float smallIconFixMultiplier = 1.0f; 

    [Space(10)]
    public GameObject viewSmall;
    
    [Space(10)]
    public GameObject viewMedium;
    public Vector3 mediumTargetScale = Vector3.one;

    [Space(10)]
    public GameObject viewFull;
    public Vector3 fullTargetScale = Vector3.one;

    [Header("--- 2. 菜单设置 ---")]
    public LiquidMenuItem selectorMenu; 
    public Vector3 selectorTargetScale = Vector3.one;

    [Header("--- 3. 动画参数 ---")]
    public float popDuration = 0.6f;
    public float retractDuration = 0.3f;
    [Range(0, 2)] public float elasticity = 1.0f;
    public Ease openEase = Ease.OutElastic;
    public Ease closeEase = Ease.InBack;

    [Header("--- 4. 环境显隐精细配置 ---")]
    public ViewStateConfig smallConfig = new ViewStateConfig { stateName = "Small State Config" };
    public ViewStateConfig mediumConfig = new ViewStateConfig { stateName = "Medium State Config" };
    public ViewStateConfig fullConfig = new ViewStateConfig { stateName = "Full State Config" };

    [Header("--- 5. 延迟浮现控制 ---")]
    public List<GameObject> delayedIcons;
    public float appearDelay = 3.0f;
    public float appearDuration = 1.0f;

    private bool _isTransitioning = false;
    private Tween _delayedCallTween;

    void Start()
    {
        if (viewSmall) startScale = viewSmall.transform.localScale;

        ForceInitView(viewSmall, true, startScale);
        ForceInitView(viewMedium, false, mediumTargetScale);
        ForceInitView(viewFull, false, fullTargetScale);

        if (viewSmall)
        {
            var iconCtrl = viewSmall.GetComponent<IconController>();
            if (iconCtrl) 
            {
                iconCtrl.ForceUpdateOriginalScale(startScale);
                iconCtrl.ResetState();
            }
        }

        if (selectorMenu) 
        {
            selectorMenu.transform.localScale = Vector3.zero;
            selectorMenu.gameObject.SetActive(false);
        }

        foreach (var icon in delayedIcons)
        {
            if(icon) { icon.transform.localScale = Vector3.zero; icon.SetActive(false); }
        }

        // 初始化时也应用一次 Collider 设置
        UpdateColliderSize(ViewState.Small_Icon);
        ApplyConfig(smallConfig);
    }

    public void TriggerMediumView() { SwitchToState(ViewState.Medium_Screen); }
    public void TriggerFullView() { SwitchToState(ViewState.Full_Screen); }

    public void SwitchToState(ViewState newState, bool instant = false)
    {
        if (!_isTransitioning && currentState == newState && !instant) return;

        currentState = newState;
        _isTransitioning = true;

        // 🔥 切换状态时，同步修改碰撞体大小
        UpdateColliderSize(newState);

        GameObject targetView = null;
        Vector3 endScale = Vector3.one;

        if (newState == ViewState.Medium_Screen) 
        { 
            targetView = viewMedium; 
            endScale = mediumTargetScale; 
        }
        else if (newState == ViewState.Full_Screen) 
        { 
            targetView = viewFull; 
            endScale = fullTargetScale; 
        }
        else 
        { 
            targetView = viewSmall; 
            endScale = startScale * smallIconFixMultiplier; 
        }

        Sequence seq = DOTween.Sequence();

        if (viewSmall) { viewSmall.transform.DOKill(); viewSmall.GetComponent<IconController>()?.StopAllAnimations(); }
        if (viewMedium) { viewMedium.transform.DOKill(); viewMedium.GetComponent<IconController>()?.StopAllAnimations(); }
        if (viewFull) { viewFull.transform.DOKill(); viewFull.GetComponent<IconController>()?.StopAllAnimations(); }

        if (viewSmall && viewSmall.activeSelf) seq.Join(viewSmall.transform.DOScale(Vector3.zero, retractDuration).SetEase(closeEase));
        if (viewMedium && viewMedium.activeSelf) seq.Join(viewMedium.transform.DOScale(Vector3.zero, retractDuration).SetEase(closeEase));
        if (viewFull && viewFull.activeSelf) seq.Join(viewFull.transform.DOScale(Vector3.zero, retractDuration).SetEase(closeEase));

        seq.AppendCallback(() => {
            if(viewSmall) viewSmall.SetActive(false);
            if(viewMedium) viewMedium.SetActive(false);
            if(viewFull) viewFull.SetActive(false);
        });

        seq.AppendCallback(() => {
            if (targetView)
            {
                var iconCtrl = targetView.GetComponent<IconController>();
                if (iconCtrl) 
                {
                    iconCtrl.ForceUpdateOriginalScale(endScale);
                    iconCtrl.ResetState();
                }

                targetView.SetActive(true);
                targetView.transform.localScale = Vector3.zero;
                targetView.transform.DOScale(endScale, popDuration).SetEase(openEase, elasticity);
            }
        });

        seq.OnComplete(() => {
            _isTransitioning = false;
            if (newState == ViewState.Small_Icon) ApplyConfig(smallConfig);
            else if (newState == ViewState.Medium_Screen) ApplyConfig(mediumConfig);
            else if (newState == ViewState.Full_Screen) ApplyConfig(fullConfig);
            HandleDelayedIcons(newState); 
        });
    }

    // 🔥 新增：修改碰撞体大小的逻辑
    void UpdateColliderSize(ViewState state)
    {
        if (targetCollider == null) return;

        Vector3 finalSize = smallColliderSize;
        switch (state)
        {
            case ViewState.Small_Icon: finalSize = smallColliderSize; break;
            case ViewState.Medium_Screen: finalSize = mediumColliderSize; break;
            case ViewState.Full_Screen: finalSize = fullColliderSize; break;
        }

        // 可以加一个动画让它平滑变化，或者直接变
        // 这里直接变，反应最快
        targetCollider.size = finalSize;
    }

    // 辅助方法... (保持不变)
    public void CycleNext()
    {
        if (currentState == ViewState.Small_Icon) SwitchToState(ViewState.Medium_Screen);
        else if (currentState == ViewState.Medium_Screen) SwitchToState(ViewState.Full_Screen);
        else if (currentState == ViewState.Full_Screen) { if (viewFull) { viewFull.transform.DOKill(true); viewFull.transform.DOPunchRotation(new Vector3(0, 0, 2f), 0.3f, 10, 1); } }
    }
    public void CyclePrev() { if (currentState == ViewState.Full_Screen) SwitchToState(ViewState.Medium_Screen); else SwitchToState(ViewState.Small_Icon); }
    public void OpenSelectorMenu() { if(selectorMenu) { selectorMenu.gameObject.SetActive(true); selectorMenu.transform.DOKill(); selectorMenu.transform.localScale = Vector3.zero; selectorMenu.transform.DOScale(selectorTargetScale, popDuration).SetEase(openEase, elasticity); selectorMenu.OpenChildren(); } }
    public void CloseSelectorMenu() { if(selectorMenu && selectorMenu.gameObject.activeSelf) { selectorMenu.transform.DOKill(); selectorMenu.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => selectorMenu.gameObject.SetActive(false)); } }
    void ApplyConfig(ViewStateConfig config) { foreach (var obj in config.objectsToHide) if (obj) obj.SetActive(false); foreach (var obj in config.objectsToShow) if (obj) obj.SetActive(true); }
    void HandleDelayedIcons(ViewState newState) { if (_delayedCallTween != null) _delayedCallTween.Kill(); if (newState == ViewState.Small_Icon) { foreach (var icon in delayedIcons) { if (icon) { icon.transform.DOKill(); icon.transform.DOScale(Vector3.zero, 0.3f).OnComplete(() => icon.SetActive(false)); } } } else { _delayedCallTween = DOVirtual.DelayedCall(appearDelay, () => { foreach (var icon in delayedIcons) { if (icon) { icon.SetActive(true); icon.transform.localScale = Vector3.zero; icon.transform.DOScale(Vector3.one, appearDuration).SetEase(Ease.OutSine); } } }); } }
    void ForceInitView(GameObject view, bool active, Vector3 scale) { if(!view) return; view.SetActive(active); view.transform.localScale = active ? scale : Vector3.zero; if(active) { var ic = view.GetComponent<IconController>(); if(ic) { ic.ForceUpdateOriginalScale(scale); ic.ResetState(); } } }
}