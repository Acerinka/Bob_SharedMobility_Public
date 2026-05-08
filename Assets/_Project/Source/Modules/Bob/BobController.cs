using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace Bob.SharedMobility
{
    public class BobController : MonoBehaviour
    {
        // ==========================================
        // 🏗️ 数据结构定义
        // ==========================================

        [System.Serializable]
        public class SkinSet
        {
            public string skinName;       
            public GameObject bodyObject; 
            public GameObject coreObject; 
        }

        [System.Serializable]
        public struct RemoteStateData
        {
            public float duration;
            public float burstFloatSpeed;
            public float burstFloatAmplitude;
            public float burstLiftAmount;
            public float optimalTriggerDelay; 
        }

        // 提醒模式动画参数 (用于 EnterReminderState)
        [System.Serializable]
        public struct ReminderAnimSettings
        {
            [Tooltip("总动画时长")]
            public float totalDuration;
            [Tooltip("跳跃高度 (Y轴偏移量)")]
            public float jumpHeight;
            [Tooltip("旋转角度 (360 = 转一圈, 720 = 转两圈)")]
            public float rotationAngle;
            [Tooltip("旋转使用的曲线")]
            public Ease rotationEase;
        }

        // Native action tuning for TriggerAction.
        [System.Serializable]
        public struct BobActionSettings
        {
            [Header("🦘 跳跃设置 (Jump)")]
            public float jumpHeight;    // 跳跃高度 (默认 0.8)
            public float jumpUpTime;    // 上冲时间 (默认 0.3)
            public float jumpDownTime;  // 下落时间 (默认 0.5)

            [Header("💡 发光脉冲设置 (Pulse)")]
            public float pulseIntensity; // 发光强度 (默认 1.5)
            public float pulseInTime;    // 变亮时间 (默认 0.2)
            public float pulseOutTime;   // 变暗时间 (默认 0.8)

            [Header("🌪️ 旋转设置 (Spin)")]
            public float spinDuration;   // 转一圈要多久 (默认 0.6)
        }

        public enum BobActionType
        {
            None,
            JumpHigh,    // 利用 _burstYOffset 做原生跳跃
            PulseLight,  // 利用 bodyOnlyVolume 做发光呼吸
            SpinFast     // 利用 Rotation 做快速自转
        }

        // ==========================================
        // Runtime configuration.
        // ==========================================

        [Header("Skins")]
        public List<SkinSet> skins;       
        public int currentSkinIndex = 0;  

        [Header("Core")]
        public Transform eyesRoot;           

        [Header("Reminder")]
        public GameObject lightBulbIcon; 
        public int reminderSkinIndex = 2; 
        public ReminderAnimSettings reminderAnim = new ReminderAnimSettings
        {
            totalDuration = 0.8f,
            jumpHeight = 0.8f,
            rotationAngle = 360f,
            rotationEase = Ease.InOutBack
        };

        [Header("Actions")]
        public BobActionSettings actionSettings = new BobActionSettings
        {
            jumpHeight = 0.8f,
            jumpUpTime = 0.3f,
            jumpDownTime = 0.5f,
            pulseIntensity = 1.5f,
            pulseInTime = 0.2f,
            pulseOutTime = 0.8f,
            spinDuration = 0.6f
        };

        [Header("Idle")]
        public bool enableOrbitAtStart = true;

        [Header("Diagnostics")]
        public bool enableDebugSkinHotkeys = true;

        [Header("Remote Interaction")]
        public RemoteStateData remoteState = new RemoteStateData 
        { 
            duration = 1.0f, 
            burstFloatSpeed = 20.0f,    
            burstFloatAmplitude = 0.15f,
            burstLiftAmount = 0.5f, 
            optimalTriggerDelay = 0.5f
        };

        [Header("Hover")]
        public float floatSpeed = 1.0f;      
        public float floatAmplitude = 0.1f;  
        
        private float _defaultFloatSpeed;
        private float _defaultFloatAmplitude;
        private float _burstYOffset = 0f; 

        [Header("Orbit")]
        public float rotateSpeed = 1.0f;     
        public float rotateRadius = 0.2f;    

        [Header("Mixer")]
        [Range(0, 1)] public float masterVolume = 0; 
        [Range(0, 1)] public float bodyOnlyVolume = 0; 
        [Range(0, 1)] public float coreOnlyVolume = 0; 

        [Header("Shader")]
        public float minEnergy = 1f; public float maxEnergy = 5f;
        public float minCoreLight = 1.0f; public float maxCoreLight = 3.0f;
        public float minCoreSpeed = 0.5f; public float maxCoreSpeed = 3.0f;

        [Header("Flight")]
        public int stretchBlendShapeIndex = 0; 
        public float stretchDuration = 0.3f; 
        public float landSlideDist = 0.4f; 
        public float landSquashY = 0.65f; 
        public float landSquashXZ = 1.35f;
        public float landSquashTime = 0.1f;
        public float landRecoverTime = 0.6f;
        [Range(0, 1)] public float landEnergyPulse = 0.0f; 
        public float avoidDistance = 150f; 
        public float moveDuration = 0.5f;  
        public float restoreDelay = 2.0f;  

        [HideInInspector] public Vector3 lastVanishedPos; 
        public Vector3 InitialPos => _initialPos;

        private Material _curBodyMat; 
        private Material _curCoreMat; 
        private Vector3 _initialPos;     
        private Quaternion _initialRot; 
        private Vector3 _initialScale;
        private bool _isFlying = false; 
        private bool _isAvoiding = false; 
        private bool _isInReminderMode = false; 
        private Tween _restoreTimer;      

        private static readonly int EnergyID = Shader.PropertyToID("_EnergyLevel");
        private static readonly int CoreLightID = Shader.PropertyToID("_CoreLight");
        private static readonly int CoreSpeedID = Shader.PropertyToID("_CoreSpeed");

        // ==========================================
        // 🚀 生命周期 & 核心循环
        // ==========================================

        void Start()
        {
            _initialPos = transform.localPosition;
            _initialRot = transform.localRotation;
            _initialScale = transform.localScale;
            lastVanishedPos = transform.position; 
            
            _defaultFloatSpeed = floatSpeed;
            _defaultFloatAmplitude = floatAmplitude;
            
            if (lightBulbIcon) lightBulbIcon.SetActive(false);
            ChangeSkin(currentSkinIndex);
        }

        void Update()
        {
            // 键盘切皮肤仅在非提醒模式下可用
            if (!_isInReminderMode && enableDebugSkinHotkeys)
            {
                if (ProjectInput.WasKeyPressed(KeyCode.Alpha1)) ChangeSkin(0);
                if (ProjectInput.WasKeyPressed(KeyCode.Alpha2)) ChangeSkin(1);
                if (ProjectInput.WasKeyPressed(KeyCode.Alpha3)) ChangeSkin(2);
            }

            if (gameObject.activeSelf && !_isFlying && !_isAvoiding && !_isInReminderMode && transform.localScale.x > 0.1f)
            {
                DoIdleMotion(); 
                DoMixerSimulation(); 
            }
            else if (_isAvoiding || _isInReminderMode) 
            {
                DoMixerSimulation();
            }
        }

        void DoIdleMotion()
        {
            Vector3 startWorldPos = (transform.parent != null) ? transform.parent.TransformPoint(_initialPos) : _initialPos;
            bool atHomeBase = Vector3.Distance(lastVanishedPos, startWorldPos) < 0.1f;

            if (enableOrbitAtStart && atHomeBase) DoOrbitMotion(); 
            else DoStationaryHover(); 
        }

        void DoOrbitMotion()
        {
            float t = Time.time;
            float angle = t * rotateSpeed;
            float xOffset = Mathf.Cos(angle) * rotateRadius;
            float zOffset = Mathf.Sin(angle) * rotateRadius;
            float waveY = (_burstYOffset > 0.01f) ? 0f : Mathf.Sin(t * floatSpeed) * floatAmplitude;

            transform.position = lastVanishedPos + new Vector3(xOffset, waveY + _burstYOffset, zOffset);
            
            Vector3 lookDir = new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle));
            if (rotateSpeed < 0) lookDir = -lookDir;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
        }

        void DoStationaryHover()
        {
            float t = Time.time;
            float waveY = (_burstYOffset > 0.01f) ? 0f : Mathf.Sin(t * floatSpeed) * floatAmplitude;
            transform.position = lastVanishedPos + new Vector3(0, waveY + _burstYOffset, 0);
            transform.localRotation = _initialRot; 
        }

        // =========================================================
        // Native animation actions used by onboarding and feedback flows.
        // =========================================================
        public void TriggerAction(BobActionType action)
        {
            DOTween.Kill(this, "BobAction"); // 打断之前的动作

            switch (action)
            {
                case BobActionType.JumpHigh:
                    // Jump up and settle back using Inspector tuning.
                    Sequence jumpSeq = DOTween.Sequence();
                    jumpSeq.Append(DOTween.To(() => _burstYOffset, x => _burstYOffset = x, actionSettings.jumpHeight, actionSettings.jumpUpTime).SetEase(Ease.OutQuad));
                    jumpSeq.Append(DOTween.To(() => _burstYOffset, x => _burstYOffset = x, 0f, actionSettings.jumpDownTime).SetEase(Ease.OutBounce));
                    jumpSeq.SetId("BobAction");
                    break;

                case BobActionType.PulseLight:
                    // 瞬间变亮 + 缓慢变回
                    float startVol = bodyOnlyVolume;
                    Sequence pulseSeq = DOTween.Sequence();
                    pulseSeq.Append(DOTween.To(() => bodyOnlyVolume, x => bodyOnlyVolume = x, actionSettings.pulseIntensity, actionSettings.pulseInTime));
                    pulseSeq.Append(DOTween.To(() => bodyOnlyVolume, x => bodyOnlyVolume = x, 0f, actionSettings.pulseOutTime));
                    pulseSeq.SetId("BobAction");
                    break;

                case BobActionType.SpinFast:
                    // 快速自转一圈
                    transform.DORotate(new Vector3(0, 360, 0), actionSettings.spinDuration, RotateMode.LocalAxisAdd)
                             .SetEase(Ease.InOutBack)
                             .SetId("BobAction");
                    break;
            }
        }

        // =========================================================
        // Reminder mode animation.
        // =========================================================
        public void EnterReminderState()
        {
            _isInReminderMode = true;
            _isFlying = false; 
            _burstYOffset = 0f;

            ChangeSkin(reminderSkinIndex);

            if (lightBulbIcon) 
            {
                lightBulbIcon.SetActive(true);
                lightBulbIcon.transform.localScale = Vector3.zero;
                lightBulbIcon.transform.DOScale(Vector3.one, reminderAnim.totalDuration * 0.5f).SetEase(Ease.OutBack);
            }

            Sequence seq = DOTween.Sequence();
            
            float upTime = reminderAnim.totalDuration * 0.4f;
            float downTime = reminderAnim.totalDuration * 0.6f;

            // A. 向上跳
            seq.Append(DOTween.To(()=>_burstYOffset, x=>_burstYOffset=x, reminderAnim.jumpHeight, upTime).SetEase(Ease.OutQuad));
            
            // B. 旋转 (增量)
            seq.Join(transform.DOLocalRotate(new Vector3(0, reminderAnim.rotationAngle, 0), reminderAnim.totalDuration, RotateMode.LocalAxisAdd)
               .SetEase(reminderAnim.rotationEase));
            
            // C. 落下
            seq.Append(DOTween.To(()=>_burstYOffset, x=>_burstYOffset=x, 0f, downTime).SetEase(Ease.OutBounce));
        }

        public void ExitReminderState()
        {
            if (lightBulbIcon)
            {
                lightBulbIcon.transform.DOScale(0, 0.2f).OnComplete(()=> lightBulbIcon.SetActive(false));
            }
            ChangeSkin(0);
            _isInReminderMode = false;
        }

        // =========================================================
        // Remote interaction animation.
        // =========================================================
        public float PlayRemoteInteraction()
        {
            if (_isFlying) return 0f;

            DOTween.Kill(this, "RemoteFloat");

            float originalBodyVol = bodyOnlyVolume;
            
            // 1. 发光
            DOTween.To(() => bodyOnlyVolume, x => bodyOnlyVolume = x, 1.0f, remoteState.duration * 0.3f)
                   .SetId("RemoteFloat")
                   .OnComplete(() => {
                       DOTween.To(() => bodyOnlyVolume, x => bodyOnlyVolume = x, originalBodyVol, remoteState.duration * 0.7f).SetId("RemoteFloat");
                   });

            // 2. 浮动加速
            DOTween.To(() => floatSpeed, x => floatSpeed = x, remoteState.burstFloatSpeed, 0.2f).SetEase(Ease.OutQuad).SetId("RemoteFloat");

            // 3. 强制抬升
            DOTween.To(() => _burstYOffset, x => _burstYOffset = x, remoteState.burstLiftAmount, remoteState.duration * 0.4f)
                   .SetEase(Ease.OutQuad)
                   .SetId("RemoteFloat")
                   .OnComplete(() => {
                       DOTween.To(() => _burstYOffset, x => _burstYOffset = x, 0f, remoteState.duration * 0.6f)
                              .SetEase(Ease.InOutSine).SetId("RemoteFloat");
                   });

            // 4. 恢复默认
            DOVirtual.DelayedCall(remoteState.duration, () => {
                DOTween.To(() => floatSpeed, x => floatSpeed = x, _defaultFloatSpeed, 0.5f).SetEase(Ease.InOutSine).SetId("RemoteFloat");
            }).SetId("RemoteFloat");
            
            return remoteState.optimalTriggerDelay;
        }

        // =========================================================
        // 🛠️ 辅助与状态管理
        // =========================================================

        public void PrepareForFlight() { DOTween.Kill(transform); DOTween.Kill(this); if (_restoreTimer != null) _restoreTimer.Kill(); _isFlying = true; _isAvoiding = false; _isInReminderMode = false; floatSpeed = _defaultFloatSpeed; floatAmplitude = _defaultFloatAmplitude; _burstYOffset = 0f; transform.position = lastVanishedPos; transform.localScale = Vector3.zero; transform.localRotation = _initialRot; SetBlendShapeWeight(0f); bodyOnlyVolume = 0; gameObject.SetActive(true); }
        public Tween AppearAnim(float duration) { return transform.DOScale(_initialScale, duration).SetEase(Ease.OutBack); }
        public void StartFlyingShape() { foreach (var skin in skins) { if (skin.bodyObject && skin.bodyObject.activeSelf) { var mesh = skin.bodyObject.GetComponent<SkinnedMeshRenderer>(); if (mesh) { DOTween.To(() => mesh.GetBlendShapeWeight(stretchBlendShapeIndex), x => mesh.SetBlendShapeWeight(stretchBlendShapeIndex, x), 100f, stretchDuration); } } } }
        public void StartReturnShape(float totalFlightTime) { foreach (var skin in skins) { if (skin.bodyObject && skin.bodyObject.activeSelf) { var mesh = skin.bodyObject.GetComponent<SkinnedMeshRenderer>(); if (mesh) { Sequence s = DOTween.Sequence(); s.Append(DOTween.To(() => mesh.GetBlendShapeWeight(stretchBlendShapeIndex), x => mesh.SetBlendShapeWeight(stretchBlendShapeIndex, x), 100f, totalFlightTime * 0.4f).SetEase(Ease.OutQuad)); s.Append(DOTween.To(() => mesh.GetBlendShapeWeight(stretchBlendShapeIndex), x => mesh.SetBlendShapeWeight(stretchBlendShapeIndex, x), 0f, totalFlightTime * 0.6f).SetEase(Ease.InQuad)); } } } }
        public void ArriveAndVanish(Vector3 vanishPos) { lastVanishedPos = vanishPos; _isFlying = false; gameObject.SetActive(false); transform.localScale = _initialScale; SetBlendShapeWeight(0); }
        public void ArriveAndStay(Vector3 stayPos) { lastVanishedPos = stayPos; transform.position = stayPos; gameObject.SetActive(true); SetBlendShapeWeight(0); Sequence landSeq = DOTween.Sequence(); transform.localScale = _initialScale; Vector3 squashScale = new Vector3(_initialScale.x * landSquashXZ, _initialScale.y * landSquashY, _initialScale.z * landSquashXZ); landSeq.Append(transform.DOScale(squashScale, landSquashTime).SetEase(Ease.OutQuad)); landSeq.Append(transform.DOScale(_initialScale, landRecoverTime).SetEase(Ease.OutElastic)); Vector3 forwardDir = transform.forward; if (landSlideDist > 0) { Sequence slideSeq = DOTween.Sequence(); slideSeq.Append(transform.DOMove(stayPos + forwardDir * landSlideDist, landRecoverTime * 0.3f).SetEase(Ease.OutQuad)); slideSeq.Append(transform.DOMove(stayPos, landRecoverTime * 0.7f).SetEase(Ease.OutBack)); landSeq.Join(slideSeq); } if (landEnergyPulse > 0) { bodyOnlyVolume = 0; Sequence lightSeq = DOTween.Sequence(); lightSeq.Append(DOTween.To(() => bodyOnlyVolume, x => bodyOnlyVolume = x, landEnergyPulse, landSquashTime)); lightSeq.Append(DOTween.To(() => bodyOnlyVolume, x => bodyOnlyVolume = x, 0f, landRecoverTime).SetEase(Ease.OutSine)); landSeq.Join(lightSeq); } landSeq.OnComplete(() => { _isFlying = false; }); }
        public void DodgeDown() { if (_isFlying) return; if (_restoreTimer != null) _restoreTimer.Kill(); if (_isAvoiding) return; _isAvoiding = true; transform.DOKill(); transform.DOLocalMoveY(_initialPos.y - avoidDistance, moveDuration).SetEase(Ease.OutBack); }
        public void ReturnToIdleDelayed() { if (_isFlying) return; if (_restoreTimer != null) _restoreTimer.Kill(); _restoreTimer = DOVirtual.DelayedCall(restoreDelay, () => { _isAvoiding = false; transform.DOKill(); transform.DOLocalMove(_initialPos, moveDuration).SetEase(Ease.OutBack); }); }
        void SetBlendShapeWeight(float val) { foreach (var skin in skins) { if (skin.bodyObject) { var mesh = skin.bodyObject.GetComponent<SkinnedMeshRenderer>(); if (mesh) mesh.SetBlendShapeWeight(stretchBlendShapeIndex, val); } } }
        public void ChangeSkin(int index) { if (index < 0 || index >= skins.Count) return; currentSkinIndex = index; for (int i = 0; i < skins.Count; i++) { bool isActive = (i == index); if (skins[i].bodyObject) skins[i].bodyObject.SetActive(isActive); if (skins[i].coreObject) skins[i].coreObject.SetActive(isActive); } GameObject activeBody = skins[index].bodyObject; GameObject activeCore = skins[index].coreObject; _curBodyMat = null; _curCoreMat = null; if (activeBody) { Renderer r = activeBody.GetComponent<Renderer>(); if (r) _curBodyMat = r.material; } if (activeCore) { Renderer r = activeCore.GetComponent<Renderer>(); if (r) _curCoreMat = r.material; } }
        void DoMixerSimulation() { float finalBodyVol = Mathf.Clamp01(masterVolume + bodyOnlyVolume); float finalCoreVol = Mathf.Clamp01(masterVolume + coreOnlyVolume); if (_curBodyMat) _curBodyMat.SetFloat(EnergyID, Mathf.Lerp(minEnergy, maxEnergy, finalBodyVol)); if (_curCoreMat) { _curCoreMat.SetFloat(CoreLightID, Mathf.Lerp(minCoreLight, maxCoreLight, finalCoreVol)); _curCoreMat.SetFloat(CoreSpeedID, Mathf.Lerp(minCoreSpeed, maxCoreSpeed, finalCoreVol)); } }
        public void ResetState() { DOTween.Kill(transform); DOTween.Kill(this); if (_restoreTimer != null) _restoreTimer.Kill(); _isFlying = false; _isAvoiding = false; _isInReminderMode = false; gameObject.SetActive(true); transform.localPosition = _initialPos; transform.localRotation = _initialRot; transform.localScale = _initialScale; lastVanishedPos = transform.position; SetBlendShapeWeight(0); masterVolume = 0; bodyOnlyVolume = 0; coreOnlyVolume = 0; floatSpeed = _defaultFloatSpeed > 0 ? _defaultFloatSpeed : 1.0f; floatAmplitude = _defaultFloatAmplitude > 0 ? _defaultFloatAmplitude : 0.1f; _burstYOffset = 0f; if(lightBulbIcon) lightBulbIcon.SetActive(false); ChangeSkin(0); }
    }
}
