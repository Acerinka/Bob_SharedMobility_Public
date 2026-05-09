using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Bob.SharedMobility
{
    public partial class BobController : MonoBehaviour
    {
        private const string BobActionTweenId = "BobController.Action";
        private const string FlightShapeTweenId = "BobController.FlightShape";
        private const string RemoteFloatTweenId = "BobController.RemoteFloat";

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

        [System.Serializable]
        public struct ReminderAnimSettings
        {
            [Tooltip("Total animation duration.")]
            public float totalDuration;
            [Tooltip("Jump height as a Y-axis offset.")]
            public float jumpHeight;
            [Tooltip("Rotation angle. 360 means one full spin.")]
            public float rotationAngle;
            [Tooltip("Ease curve used by the rotation tween.")]
            public Ease rotationEase;
        }

        [System.Serializable]
        public struct BobActionSettings
        {
            [Header("Jump")]
            public float jumpHeight;
            public float jumpUpTime;
            public float jumpDownTime;

            [Header("Pulse")]
            public float pulseIntensity;
            public float pulseInTime;
            public float pulseOutTime;

            [Header("Spin")]
            public float spinDuration;
        }

        public enum BobActionType
        {
            None,
            JumpHigh,
            PulseLight,
            SpinFast
        }

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

        [Header("Orbit")]
        public float rotateSpeed = 1.0f;
        public float rotateRadius = 0.2f;

        [Header("Mixer")]
        [Range(0, 1)] public float masterVolume = 0;
        [Range(0, 1)] public float bodyOnlyVolume = 0;
        [Range(0, 1)] public float coreOnlyVolume = 0;

        [Header("Shader")]
        public float minEnergy = 1f;
        public float maxEnergy = 5f;
        public float minCoreLight = 1.0f;
        public float maxCoreLight = 3.0f;
        public float minCoreSpeed = 0.5f;
        public float maxCoreSpeed = 3.0f;

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

        private float _defaultFloatSpeed;
        private float _defaultFloatAmplitude;
        private float _burstYOffset = 0f;
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

        public Vector3 InitialPos => HomeWorldPosition;
        public Vector3 HomeWorldPosition => transform.parent != null
            ? transform.parent.TransformPoint(_initialPos)
            : _initialPos;
        public float LandingRecoveryDuration => Mathf.Max(0f, landSquashTime) + Mathf.Max(0f, landRecoverTime);
        public bool IsFlying => _isFlying;

        private void Start()
        {
            _initialPos = transform.localPosition;
            _initialRot = transform.localRotation;
            _initialScale = transform.localScale;
            lastVanishedPos = HomeWorldPosition;

            _defaultFloatSpeed = floatSpeed;
            _defaultFloatAmplitude = floatAmplitude;

            if (lightBulbIcon)
            {
                lightBulbIcon.SetActive(false);
            }

            ChangeSkin(currentSkinIndex);
        }

        private void Update()
        {
            if (!_isInReminderMode && enableDebugSkinHotkeys)
            {
                if (ProjectInput.WasKeyPressed(KeyCode.Alpha1)) ChangeSkin(0);
                if (ProjectInput.WasKeyPressed(KeyCode.Alpha2)) ChangeSkin(1);
                if (ProjectInput.WasKeyPressed(KeyCode.Alpha3)) ChangeSkin(2);
            }

            bool canIdle = gameObject.activeSelf
                && !_isFlying
                && !_isAvoiding
                && !_isInReminderMode
                && transform.localScale.x > 0.1f;

            if (canIdle)
            {
                DoIdleMotion();
                DoMixerSimulation();
            }
            else if (_isAvoiding || _isInReminderMode)
            {
                DoMixerSimulation();
            }
        }

        public void ChangeSkin(int index)
        {
            if (skins == null || index < 0 || index >= skins.Count) return;

            currentSkinIndex = index;

            for (int i = 0; i < skins.Count; i++)
            {
                bool isActive = i == index;
                if (skins[i].bodyObject) skins[i].bodyObject.SetActive(isActive);
                if (skins[i].coreObject) skins[i].coreObject.SetActive(isActive);
            }

            GameObject activeBody = skins[index].bodyObject;
            GameObject activeCore = skins[index].coreObject;

            _curBodyMat = null;
            _curCoreMat = null;

            if (activeBody && activeBody.TryGetComponent(out Renderer bodyRenderer))
            {
                _curBodyMat = bodyRenderer.material;
            }

            if (activeCore && activeCore.TryGetComponent(out Renderer coreRenderer))
            {
                _curCoreMat = coreRenderer.material;
            }
        }

        public void ResetState()
        {
            KillMotionTweens();

            _isFlying = false;
            _isAvoiding = false;
            _isInReminderMode = false;
            gameObject.SetActive(true);
            transform.localPosition = _initialPos;
            transform.localRotation = _initialRot;
            transform.localScale = _initialScale;
            lastVanishedPos = HomeWorldPosition;
            SetBlendShapeWeight(0);

            masterVolume = 0;
            bodyOnlyVolume = 0;
            coreOnlyVolume = 0;
            floatSpeed = _defaultFloatSpeed > 0 ? _defaultFloatSpeed : 1.0f;
            floatAmplitude = _defaultFloatAmplitude > 0 ? _defaultFloatAmplitude : 0.1f;
            _burstYOffset = 0f;

            if (lightBulbIcon)
            {
                lightBulbIcon.SetActive(false);
            }

            ChangeSkin(0);
        }

    }
}
