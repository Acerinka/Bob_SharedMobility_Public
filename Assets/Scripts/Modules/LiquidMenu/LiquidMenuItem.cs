using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Events;

[DefaultExecutionOrder(-100)] 
public class LiquidMenuItem : MonoBehaviour
{
    [Header("--- 🌳 家族树结构 ---")]
    public LiquidMenuItem parentItem;
    public List<LiquidMenuItem> childItems;
    public bool isRoot = false;

    [Header("--- ⚡ 自动化控制 ---")]
    public bool autoExpandOnStart = false;

    [Header("--- 📐 尺寸修正 ---")]
    [Tooltip("🔥【出生大小修正】: 图标弹出来时要是多大？填 1.2 表示一出来就是 1.2 倍")]
    public float spawnScaleMultiplier = 1.0f; 

    [Tooltip("🔥【激活大小倍率】: 点击展开时，基于【出生大小】再变大多少？填 1.0 表示不变")]
    public float activeScaleMultiplier = 1.0f; 

    [Header("--- 🎨 动画参数 ---")]
    public float popDuration = 0.6f;     
    public float retractDuration = 0.2f; 
    [Range(0, 2)] public float elasticity = 1.0f; 
    public float staggerDelay = 0.05f;   

    [Header("--- 👁️ 显隐控制 ---")]
    public bool autoHideSiblings = true;
    public List<GameObject> customObjectsToHide;
    public List<GameObject> customObjectsToShow;

    [Header("--- ⚡ 事件触发 ---")]
    public UnityEvent onActionTriggered;

    // --- 内部状态 ---
    private Vector3 _initLocalPos; 
    private Vector3 _initScale;    
    private bool _isOpen = false; 

    public bool IsOpen => _isOpen;

    // 🔥🔥【修复点】：这里改成 public，让外部控制器可以读取它！
    public Vector3 TargetSpawnScale => _initScale * spawnScaleMultiplier;

    void Awake()
    {
        _initLocalPos = transform.localPosition;
        _initScale = transform.localScale;

        if (!isRoot)
        {
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (isRoot && autoExpandOnStart)
        {
            DOVirtual.DelayedCall(0.5f, () => OpenChildren());
        }
    }

    // ========================================================================
    // 🖱️ 交互入口
    // ========================================================================
    public void OnClick()
    {
        LiquidMenuManager.Instance.SetCurrentFocus(this);

        if (childItems.Count > 0)
        {
            if (!_isOpen) OpenChildren();
        }
        else
        {
            Debug.Log($"⚡ 触发功能事件: {name}");
            if (!_isOpen)
            {
                _isOpen = true; 
                transform.DOScale(TargetSpawnScale * activeScaleMultiplier, 0.2f);
                ProcessVisibility(true);
            }
            onActionTriggered.Invoke(); 
        }
    }

    // ========================================================================
    // 🌊 展开逻辑 (Open)
    // ========================================================================
    public void OpenChildren()
    {
        if (_isOpen) return;
        _isOpen = true;
        
        if (Mathf.Abs(activeScaleMultiplier - 1.0f) > 0.001f)
        {
            transform.DOScale(TargetSpawnScale * activeScaleMultiplier, 0.3f).SetEase(Ease.OutBack);
        }
        
        ProcessVisibility(true);

        for (int i = 0; i < childItems.Count; i++)
        {
            var child = childItems[i];
            child.gameObject.SetActive(true);
            
            child.transform.position = this.transform.position; 
            child.transform.localScale = Vector3.zero;

            var childIconCtrl = child.GetComponent<IconController>();
            if (childIconCtrl) 
            {
                Vector3 childRealSize = child._initScale * child.spawnScaleMultiplier;
                childIconCtrl.ForceUpdateOriginalScale(childRealSize);
            }

            float delay = i * staggerDelay;
            child.ShowAnimate(delay, popDuration, elasticity);

            if (child.autoExpandOnStart)
            {
                float autoExpandDelay = delay + popDuration * 0.8f;
                DOVirtual.DelayedCall(autoExpandDelay, () => child.OpenChildren());
            }
        }
    }

    // ========================================================================
    // 🔙 收起逻辑 (Close)
    // ========================================================================
    public void CloseChildren()
    {
        if (!_isOpen) return;
        _isOpen = false;

        foreach (var child in childItems)
        {
            if (child.IsOpen) child.CloseChildren(); 
            child.HideAnimate(this.transform.position); 
        }

        transform.DOScale(TargetSpawnScale, retractDuration);

        ProcessVisibility(false);

        if (parentItem != null)
            LiquidMenuManager.Instance.SetCurrentFocus(parentItem);
        else
            LiquidMenuManager.Instance.SetCurrentFocus(null);
    }

    // ========================================================================
    // 👁️ 显隐核心
    // ========================================================================
    private void ProcessVisibility(bool isOpening)
    {
        if (autoHideSiblings)
        {
            if (parentItem != null)
            {
                foreach (var sibling in parentItem.childItems)
                {
                    if (sibling != this) 
                    {
                        if (isOpening) 
                            sibling.HideAnimate(parentItem.transform.position); 
                        else 
                            sibling.ShowAnimate(0, 0.4f, 1f); 
                    }
                }
            }
            else if (isRoot)
            {
                if (isOpening) LiquidMenuManager.Instance.HideOtherRoots(this);
                else LiquidMenuManager.Instance.ShowAllRoots();
            }
        }

        if (customObjectsToHide != null)
        {
            foreach (var obj in customObjectsToHide)
            {
                if (obj == null) continue;
                obj.SetActive(!isOpening); 
            }
        }

        if (customObjectsToShow != null)
        {
            foreach (var obj in customObjectsToShow)
            {
                if (obj == null) continue;
                obj.SetActive(isOpening);
            }
        }
    }

    // --- 动画封装 ---

    public void ShowAnimate(float delay, float duration, float elastic)
    {
        gameObject.SetActive(true);
        var iconCtrl = GetComponent<IconController>();
        if (iconCtrl) iconCtrl.PlaySpawnAnimation(duration * 0.8f);

        transform.DOLocalMove(_initLocalPos, duration)
                 .SetEase(Ease.OutElastic, elastic)
                 .SetDelay(delay)
                 .OnComplete(() => {
                     transform.localPosition = _initLocalPos; 
                 });

        transform.DOScale(TargetSpawnScale, duration).SetEase(Ease.OutElastic, elastic).SetDelay(delay);
    }

    public void HideAnimate(Vector3? targetWorldPos = null)
    {
        Vector3 dest = targetWorldPos ?? transform.position;

        transform.DOKill(); 

        transform.DOMove(dest, retractDuration).SetEase(Ease.InQuad);

        transform.DOScale(Vector3.zero, retractDuration).SetEase(Ease.InQuad)
                 .OnComplete(() => gameObject.SetActive(false));
    }
}