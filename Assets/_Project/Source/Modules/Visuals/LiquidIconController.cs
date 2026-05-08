using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

namespace Bob.SharedMobility
{
    public class LiquidIconController : MonoBehaviour
    {
        [System.Serializable]
        public struct IconStateData
        {
            public float borderThickness; 
            public float contentScale;    
            public float flowSpeed;       
            public float surfSpeed;       
            [Range(0, 1)] public float isActive; 
        }

        [Header("--- 组件引用 ---")]
        public Renderer iconRenderer;
        [Tooltip("这就是那个会残留的假眼睛物体")]
        public GameObject iconFakeEyes; 
        
        [Header("--- 💡 视觉补偿 ---")]
        [Tooltip("如果图标觉得小，填1.2。这会直接改变_originalScale。")]
        public float visualCorrection = 1.0f; 

        [Header("--- 💡 Small Map 专属 ---")]
        [Tooltip("如果是 Small Map，勾选此项！防止被外部脚本重置大小。")]
        public bool isSmallMapIcon = false; 

        [Header("--- 动画配置 ---")]
        public float burstPeakMultiplier = 1.15f; 

        [Header("--- 融合参数 ---")]
        public float stayDuration = 2.0f; 
        public float transitionDuration = 0.5f;
        public bool shouldBobReturn = true;

        [Header("--- 状态配置 ---")]
        public IconStateData initialState; 
        public IconStateData activeState;  
        public IconStateData impactState;  
        public IconStateData burstState;   

        [Header("--- 交互事件 ---")]
        [Tooltip("Music这种纯装饰图标不要挂事件")]
        public UnityEvent onInteractionComplete;

        private Material _mat;
        private Vector3 _originalScale; 
        private Tweener _breathingTweener;
        private bool _handoffControl = false; 

        [HideInInspector] public bool blockNextResetScale = false;

        // Shader ID
        private static readonly int ThicknessID = Shader.PropertyToID("_BorderThickness");
        private static readonly int ScaleID = Shader.PropertyToID("_ContentScale");
        private static readonly int FlowSpeedID = Shader.PropertyToID("_FlowSpeed");
        private static readonly int SurfSpeedID = Shader.PropertyToID("_SurfSpeed");
        private static readonly int IsActiveID = Shader.PropertyToID("_IsActive");

        void Awake()
        {
            if (iconRenderer) _mat = iconRenderer.material;
            
            // 计算一次性基准大小
            if (transform.localScale.magnitude > 0.01f)
                _originalScale = transform.localScale * visualCorrection;
            else
                _originalScale = Vector3.one * visualCorrection;

            // 立即应用
            transform.localScale = _originalScale;

            if (iconFakeEyes) iconFakeEyes.SetActive(false);
            ApplyStateImmediate(initialState);
        }

        public void ForceUpdateOriginalScale(Vector3 externalScale)
        {
            if (isSmallMapIcon) return; // Map 拒绝外部修改
            _originalScale = externalScale * visualCorrection;
        }

        public void PlaySpawnAnimation(float duration)
        {
            if (!_mat) return;
            StopAllAnimations(); 

            _mat.SetFloat(ThicknessID, 0.5f);
            _mat.SetFloat(FlowSpeedID, 2.0f); 

            float targetThickness = initialState.borderThickness > 0 ? initialState.borderThickness : 0.05f;
            _mat.DOFloat(targetThickness, ThicknessID, duration).SetEase(Ease.OutExpo);
            _mat.DOFloat(initialState.flowSpeed, FlowSpeedID, duration);
            
            transform.localScale = Vector3.zero;
            transform.DOScale(_originalScale, duration).SetEase(Ease.OutBack);
        }

        // =========================================================
        // 🔥🔥 动画入口
        // =========================================================
        public void PlayEnterSequence(float impactDur = 0.15f, float burstDur = 0.8f)
        {
            blockNextResetScale = true; 
            if (_originalScale.magnitude < 0.01f) _originalScale = Vector3.one;

            Sequence seq = DOTween.Sequence();

            // 1. 撞击
            seq.AppendCallback(() => {
                StopAllAnimations();
                transform.DOScale(_originalScale * 0.85f, impactDur).SetEase(Ease.OutQuad);
                if (_mat) ApplyStateTween(impactState, impactDur);
            });

            seq.AppendInterval(impactDur);

            // 2. 爆发
            seq.AppendCallback(() => {
                blockNextResetScale = false; 

                // 🔥 眼睛弹出来
                if (iconFakeEyes) {
                    iconFakeEyes.SetActive(true);
                    iconFakeEyes.transform.localScale = Vector3.zero;
                    iconFakeEyes.transform.DOScale(Vector3.one, burstDur).SetEase(Ease.OutBack);
                }
                if (_mat) ApplyStateTween(burstState, burstDur);

                // 撑大 -> 回落 -> 呼吸
                transform.DOScale(_originalScale * burstPeakMultiplier, burstDur * 0.4f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => {
                        transform.DOScale(_originalScale, burstDur * 0.6f)
                            .SetEase(Ease.InOutSine)
                            .OnComplete(() => {
                                StartBreathingLoop(); 
                                
                                // 检查事件
                                bool hasEvent = onInteractionComplete != null && onInteractionComplete.GetPersistentEventCount() > 0;
                                
                                if (hasEvent)
                                {
                                    // Map 逻辑：移交控制
                                    Debug.Log($"🔔 {name} 触发交互事件");
                                    _handoffControl = true;
                                    onInteractionComplete.Invoke();
                                    if (shouldBobReturn && BobInteractionDirector.Instance)
                                        BobInteractionDirector.Instance.ReleaseBobFrom(transform.position);
                                }
                                else
                                {
                                    // 🔥 Music 逻辑：无事件，自动返航
                                    Debug.Log($"🎵 {name} 无事件，Bob 自动返航");
                                    float waitTime = stayDuration > 0 ? stayDuration : 0.5f;
                                    DOVirtual.DelayedCall(waitTime, () => {
                                        OnBobLeave();
                                    });
                                }
                            });
                    });
            });
        }

        public void OnBobEnter()
        {
            PlayEnterSequence(0.2f, 0.6f); 
        }

        // 🔥🔥🔥 核心修复：Bob 离开时的清理逻辑
        void OnBobLeave()
        {
            if (_handoffControl) return;

            StopAllAnimations(); // 停止呼吸
            
            // 1. 图标变回原样
            transform.DOScale(_originalScale, transitionDuration).SetEase(Ease.OutQuad);
            ApplyStateTween(initialState, transitionDuration);

            // 2. 🔥🔥🔥 把眼睛收回去！(这就是解决眼睛残留的关键)
            if (iconFakeEyes && iconFakeEyes.activeSelf)
            {
                iconFakeEyes.transform.DOKill();
                iconFakeEyes.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
                    .OnComplete(() => {
                        iconFakeEyes.SetActive(false); // 缩完后禁用
                    });
            }

            DOVirtual.DelayedCall(transitionDuration + 0.1f, () => {
                 ApplyStateImmediate(initialState); 
            });

            // 3. Bob 飞走
            if (shouldBobReturn && BobInteractionDirector.Instance)
            {
                BobInteractionDirector.Instance.ReleaseBobFrom(transform.position);
            }
        }

        // --- 辅助 ---
        void ApplyStateTween(IconStateData target, float duration) 
        { 
            if (!_mat) return; 
            _mat.DOFloat(target.borderThickness, ThicknessID, duration); 
            _mat.DOFloat(target.contentScale, ScaleID, duration); 
            _mat.DOFloat(target.isActive, IsActiveID, duration); 
            _mat.SetFloat(FlowSpeedID, target.flowSpeed); 
            _mat.SetFloat(SurfSpeedID, target.surfSpeed); 
        }

        public void ApplyStateImmediate(IconStateData data) 
        { 
            if (!_mat) return; 
            _mat.SetFloat(ThicknessID, data.borderThickness); 
            _mat.SetFloat(ScaleID, data.contentScale); 
            _mat.SetFloat(FlowSpeedID, data.flowSpeed); 
            _mat.SetFloat(SurfSpeedID, data.surfSpeed); 
            _mat.SetFloat(IsActiveID, data.isActive); 
            
            if (!_handoffControl && !blockNextResetScale && _originalScale.magnitude > 0.01f) 
                transform.localScale = _originalScale; 
        }

        void StartBreathingLoop() 
        { 
            if (_breathingTweener != null) _breathingTweener.Kill(); 
            _breathingTweener = transform.DOScale(_originalScale * 1.05f, 1.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine); 
        }

        public void StopAllAnimations() 
        { 
            if (_breathingTweener != null) _breathingTweener.Kill(); 
            transform.DOKill(); 
            if (_mat) _mat.DOKill(); 
        }

        public void ResetState() 
        { 
            StopAllAnimations(); 
            _handoffControl = false; 
            
            // 🔥 复位时也要关掉眼睛
            if (iconFakeEyes) iconFakeEyes.SetActive(false); 
            
            ApplyStateImmediate(initialState); 
            
            if (!blockNextResetScale) 
            { 
                // Small Map 防缩水保护
                if (isSmallMapIcon && transform.localScale.magnitude > 0.1f)
                {
                    // Do nothing
                }
                else
                {
                    if (_originalScale.magnitude > 0.01f) 
                        transform.DOScale(_originalScale, 0.3f); 
                    else 
                        transform.DOScale(Vector3.one, 0.3f);
                }
            } 
            else 
            { 
                blockNextResetScale = false; 
            } 
        }
    }
}