using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DockItem : MonoBehaviour
{
    [Header("--- 🔗 功能配置 ---")]
    public bool isHomeButton = false;
    public DockMenuController myAppController; 

    [Header("--- 🏠 Home 按钮专用 (修复Bug) ---")]
    [Tooltip("请把场景里的 MapSystem (挂着MapVisualController的物体) 拖到这里！\n只有 Home 按钮需要填这个。")]
    public MapVisualController mapController; 

    [Header("--- 🤖 Bob 交互配置 ---")]
    public float stayDuration = 1.0f;
    public bool shouldBobReturn = true;

    [Header("--- 🎨 视觉组件 ---")]
    public RectTransform targetIcon; 
    [Tooltip("水底座")]
    public Transform liquidBase3D; 
    public Image iconImageComponent; 

    [Header("--- 🔢 动画参数 ---")]
    public float impactScale = 0.7f;
    public float activeScale = 1.4f;
    public float floatHeight = 30f; 
    public Color activeColor = Color.cyan;

    private Vector3 _iconInitScale;
    private Vector3 _iconInitPos;
    private Vector3 _baseInitScale;
    private Color _iconInitColor;
    private Sequence _currentSeq;

    void Awake()
    {
        if (targetIcon) { 
            _iconInitScale = targetIcon.localScale; 
            _iconInitPos = targetIcon.localPosition; 
        }
        if (iconImageComponent) _iconInitColor = iconImageComponent.color;
        
        if (liquidBase3D) { 
            _baseInitScale = liquidBase3D.localScale; 
            if(_baseInitScale == Vector3.zero) _baseInitScale = Vector3.one; 
            liquidBase3D.localScale = Vector3.zero; 
            liquidBase3D.gameObject.SetActive(true); 
        }
        
        Button btn = GetComponent<Button>();
        if (btn) btn.onClick.AddListener(OnUserClick);
    }

    public void OnUserClick()
    {
        KillAllTweens();
        AnimateIconOnly(); 
        TriggerLogic(null); // 手动点击不带三级参数

        if (isHomeButton)
        {
            DOVirtual.DelayedCall(0.5f, ResetState);
        }
    }

    // 🔥 升级版：支持接收子菜单目标
    public void ActivateByBob(CanvasGroup subMenu = null)
    {
        KillAllTweens();
        AnimateIconOnly();
        AnimateLiquidBase(); 
        
        // 传递子菜单目标
        TriggerLogic(subMenu);

        if (stayDuration > 0)
            DOVirtual.DelayedCall(stayDuration, OnBobLeave);
        else
            DOVirtual.DelayedCall(0.5f, OnBobLeave);
    }

    // ==========================================
    // 🧠 逻辑核心 (升级版)
    // ==========================================
    private void TriggerLogic(CanvasGroup subMenu)
    {
        if (GlobalDockManager.Instance == null) return;

        if (isHomeButton)
        {
            // 1. 关闭当前打开的 App
            GlobalDockManager.Instance.CloseCurrentApp(); 

            // 2. 🔥🔥🔥【核心修复】强制让地图回到 Small Icon 状态
            // 这样才能恢复 Small View 的显隐关系
            if (mapController) 
            {
                mapController.SwitchToState(MapVisualController.ViewState.Small_Icon);
            }
            else
            {
                // 如果是 Home 按钮但没拖 MapController，给个警告
                Debug.LogWarning("⚠️ Home 按钮未绑定 MapVisualController！无法重置地图状态。请在 Inspector 中赋值。");
            }
        }
        else if (myAppController)
        {
            GlobalDockManager.Instance.SwitchToApp(myAppController, subMenu); 
        }
    }

    void OnBobLeave()
    {
        ResetState(); 
        if (shouldBobReturn && GlobalDirector.Instance)
        {
            Vector3 exitPos = liquidBase3D ? liquidBase3D.position : transform.position;
            GlobalDirector.Instance.ReleaseBobFrom(exitPos);
        }
    }

    void AnimateIconOnly()
    {
        _currentSeq = DOTween.Sequence();
        if (targetIcon) _currentSeq.Append(targetIcon.DOScale(_iconInitScale * impactScale, 0.1f).SetEase(Ease.OutQuad));
        if (iconImageComponent) _currentSeq.Join(iconImageComponent.DOColor(activeColor, 0.1f));

        _currentSeq.AppendCallback(() => {
            if (targetIcon)
            {
                targetIcon.DOScale(_iconInitScale * activeScale, 0.4f).SetEase(Ease.OutBack);
                targetIcon.DOLocalMoveY(_iconInitPos.y + floatHeight, 0.4f).SetEase(Ease.OutBack);
            }
        });
    }

    void AnimateLiquidBase()
    {
        if (liquidBase3D)
        {
            liquidBase3D.localScale = Vector3.zero;
            liquidBase3D.DOScale(_baseInitScale, 0.5f).SetEase(Ease.OutElastic);
        }
    }

    void KillAllTweens()
    {
        if (_currentSeq != null) _currentSeq.Kill();
        if (targetIcon) targetIcon.DOKill();
        if (iconImageComponent) iconImageComponent.DOKill();
        if (liquidBase3D) liquidBase3D.DOKill();
    }

    public void ResetState()
    {
        KillAllTweens();

        if (targetIcon)
        {
            targetIcon.DOScale(_iconInitScale, 0.3f);
            targetIcon.DOLocalMove(_iconInitPos, 0.3f);
        }
        if (iconImageComponent) iconImageComponent.DOColor(_iconInitColor, 0.3f);
        if (liquidBase3D) liquidBase3D.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
    }
}