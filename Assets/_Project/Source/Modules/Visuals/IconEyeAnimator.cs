using UnityEngine;
using DG.Tweening;

namespace Bob.SharedMobility
{
    public class IconEyeAnimator : MonoBehaviour
    {
        [Header("--- 眼睛组件 (拖入子物体) ---")]
        public Transform leftEye;
        public Transform rightEye;

        [Header("--- 1. 眨眼设置 (Blink) ---")]
        public float minBlinkInterval = 2.0f; 
        public float maxBlinkInterval = 5.0f; 
        public float blinkDuration = 0.15f;   

        [Header("--- 2. 自主张望设置 (Look Around) ---")]
        [Tooltip("眼珠移动的范围半径")]
        public float lookRadius = 0.03f; 
        
        [Tooltip("眼珠移动的速度 (越小越快，0.2左右比较像眼球跳动)")]
        public float moveDuration = 0.2f;

        [Tooltip("最短多久换一个地方看")]
        public float minLookInterval = 0.5f;
        [Tooltip("最长多久换一个地方看")]
        public float maxLookInterval = 2.5f;
        
        [Tooltip("有多少概率回到正中心发呆 (0-1)")]
        [Range(0, 1)] public float centerChance = 0.4f;

        // --- 内部变量 ---
        private float _nextBlinkTime;
        private float _nextLookTime;
        
        private Vector3 _leftEyeStartPos;
        private Vector3 _rightEyeStartPos;
        
        // 原始比例缓存
        private Vector3 _leftEyeInitScale;
        private Vector3 _rightEyeInitScale;

        void Awake()
        {
            // 1. 记住眼眶的初始位置
            if (leftEye)
            {
                _leftEyeStartPos = leftEye.localPosition;
                _leftEyeInitScale = leftEye.localScale; 
            }
            if (rightEye)
            {
                _rightEyeStartPos = rightEye.localPosition;
                _rightEyeInitScale = rightEye.localScale; 
            }
        }

        void OnEnable()
        {
            // 2. 每次激活时恢复原始状态
            if (leftEye) leftEye.localScale = _leftEyeInitScale;
            if (rightEye) rightEye.localScale = _rightEyeInitScale;

            // 重置计时器，防止一出来就乱动
            ResetBlinkTimer();
            ResetLookTimer();
        }

        void Update()
        {
            HandleBlinking();
            HandleRandomLook();
        }

        // ============================================
        // 逻辑 A: 眨眼 (保持不变)
        // ============================================
        void HandleBlinking()
        {
            if (Time.time >= _nextBlinkTime)
            {
                Blink();
                ResetBlinkTimer();
            }
        }

        void Blink()
        {
            if (leftEye)
            {
                float targetY = _leftEyeInitScale.y * 0.1f;
                leftEye.DOScaleY(targetY, blinkDuration).SetLoops(2, LoopType.Yoyo)
                       .OnComplete(()=> leftEye.localScale = _leftEyeInitScale);
            }

            if (rightEye)
            {
                float targetY = _rightEyeInitScale.y * 0.1f;
                rightEye.DOScaleY(targetY, blinkDuration).SetLoops(2, LoopType.Yoyo)
                       .OnComplete(()=> rightEye.localScale = _rightEyeInitScale);
            }
        }

        void ResetBlinkTimer()
        {
            _nextBlinkTime = Time.time + Random.Range(minBlinkInterval, maxBlinkInterval);
        }

        // ============================================
        // 逻辑 B: 自主张望 (核心修改)
        // ============================================
        void HandleRandomLook()
        {
            if (Time.time >= _nextLookTime)
            {
                PickNewLookTarget();
                ResetLookTimer();
            }
        }

        void PickNewLookTarget()
        {
            if (!leftEye || !rightEye) return;

            Vector3 targetOffset;

            // 决定是看某个随机方向，还是发呆看正前方
            if (Random.value < centerChance)
            {
                // 回到正中心
                targetOffset = Vector3.zero;
            }
            else
            {
                // 随机选一个方向 (在一个圆圈范围内)
                Vector2 randomDir = Random.insideUnitCircle * lookRadius;
                targetOffset = new Vector3(randomDir.x, randomDir.y, 0);
            }

            // 使用 DOTween 平滑移动眼珠
            // OutQuad 曲线模拟眼球快速转动后减速停下的感觉
            leftEye.DOLocalMove(_leftEyeStartPos + targetOffset, moveDuration).SetEase(Ease.OutQuad);
            rightEye.DOLocalMove(_rightEyeStartPos + targetOffset, moveDuration).SetEase(Ease.OutQuad);
        }

        void ResetLookTimer()
        {
            _nextLookTime = Time.time + Random.Range(minLookInterval, maxLookInterval);
        }
    }
}