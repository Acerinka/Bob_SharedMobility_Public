using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Events;

public class GlobalDirector : MonoBehaviour
{
    public static GlobalDirector Instance;

    [Header("--- 演员 ---")]
    public BobController bob;

    [System.Serializable]
    public class BobTarget
    {
        [Header("配置")]
        public string targetID;
        public List<string> keywords;
        public KeyCode debugKey = KeyCode.None;

        [Header("🔥 模式选择")]
        [Tooltip("是否为远程触发？(勾选后，Bob不会飞过去，而是原地改变浮动状态来触发事件)")]
        public bool isRemoteTrigger = false;

        [Header("连接物体 (常规飞行模式用)")]
        public Transform targetObject;

        [Tooltip("三级菜单 (可选)")]
        public CanvasGroup targetLevel3Panel;

        [Header("🚀 远程触发事件 (Remote模式用)")]
        public UnityEvent onRemoteEvent;

        [Header("微调")]
        public float zOffset = 0f;
        public float yOffset = 0f;
    }

    [Header("--- 🎯 宿主名单 ---")]
    public List<BobTarget> registeredTargets;

    [Header("--- 飞行参数 ---")]
    public float flyDuration = 0.6f;     
    public float appearDuration = 0.3f;
    public float waitTimeBeforeFly = 0f; 

    private string _currentLocID = "";

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        foreach (var t in registeredTargets)
        {
            if (t.debugKey != KeyCode.None && Input.GetKeyDown(t.debugKey))
            {
                Debug.Log($"🔧 快捷键前往: {t.targetID}");
                GoToTarget(t);
            }
        }
        if (Input.GetKeyDown(KeyCode.R)) ResetAll();
    }

    public void ProcessVoiceCommand(string text)
    {
        foreach (var target in registeredTargets)
        {
            foreach (var key in target.keywords)
            {
                if (text.Contains(key))
                {
                    Debug.Log($"✅ 语音匹配: {target.targetID}");
                    GoToTarget(target);
                    return;
                }
            }
        }
        if (text.Contains("reset") || text.Contains("cancel")) ResetAll();
    }

    // =========================================================
    // 🚀 核心逻辑
    // =========================================================
    public void GoToTarget(BobTarget target)
    {
        if (_currentLocID == target.targetID) return;
        _currentLocID = target.targetID;

        // 🔥 0. 远程触发模式
        if (target.isRemoteTrigger)
        {
            Debug.Log($"✨ 执行远程触发: {target.targetID}");
            
            float recommendedDelay = 0.5f;
            if (bob) recommendedDelay = bob.PlayRemoteInteraction();

            DOVirtual.DelayedCall(recommendedDelay, () => {
                target.onRemoteEvent.Invoke();
            });
            return;
        }

        // --- 常规飞行逻辑 ---

        // 🔥🔥🔥【关键修正】告诉复位逻辑：忽略当前目标！别碰它！
        SilentResetEverything(target.targetID);

        if (target.targetObject == null) return;

        IconController iconCtrl = target.targetObject.GetComponent<IconController>();
        DockItem dockItem = target.targetObject.GetComponent<DockItem>();

        Vector3 dest = target.targetObject.position;
        if (dockItem != null && dockItem.liquidBase3D != null)
        {
            dest = dockItem.liquidBase3D.position;
        }
        dest.z += target.zOffset;
        dest.y += target.yOffset; 

        bool shouldVanish = (iconCtrl != null) || (dockItem != null);

        FlyBobSequence(dest, () => {
            
            if (iconCtrl != null)
            {
                iconCtrl.gameObject.SetActive(true);
                iconCtrl.OnBobEnter(); 
            }
            else if (dockItem != null)
            {
                dockItem.ActivateByBob(target.targetLevel3Panel);
            }

        }, shouldVanish);
    }

    // 🔥🔥🔥【关键修正】增加忽略参数
    private void SilentResetEverything(string ignoreTargetID = null)
    {
        if (GlobalDockManager.Instance)
        {
            GlobalDockManager.Instance.CloseCurrentApp();
        }

        foreach (var t in registeredTargets)
        {
            // 如果是当前要去的目标，直接跳过复位！
            // 这样它就保持原样，不会突然变成 1.2 倍
            if (ignoreTargetID != null && t.targetID == ignoreTargetID) continue;

            if (t.targetObject == null) continue;
            
            var i = t.targetObject.GetComponent<IconController>();
            var d = t.targetObject.GetComponent<DockItem>();
            if (i) i.ResetState();
            if (d) d.ResetState();
        }
        
        if (bob) bob.transform.DOKill();
    }

    public void ReleaseBobFrom(Vector3 startPos)
    {
        _currentLocID = "";
        bob.transform.position = startPos; 
        FlyBobSequence(bob.InitialPos, null, false);
    }

    private void FlyBobSequence(Vector3 targetPos, System.Action onArrival, bool isVanishOnArrival)
    {
        Sequence seq = DOTween.Sequence();
        seq.AppendCallback(() => bob.PrepareForFlight());
        seq.Append(bob.AppearAnim(appearDuration));
        if (waitTimeBeforeFly > 0) seq.AppendInterval(waitTimeBeforeFly);

        if (isVanishOnArrival)
        {
            seq.AppendCallback(() => bob.StartFlyingShape());
            seq.Append(bob.transform.DOMove(targetPos, flyDuration).SetEase(Ease.InBack));
        }
        else
        {
            seq.AppendCallback(() => bob.StartReturnShape(flyDuration));
            seq.Append(bob.transform.DOMove(targetPos, flyDuration).SetEase(Ease.InOutSine));
        }

        seq.AppendCallback(() => {
            if (isVanishOnArrival) bob.ArriveAndVanish(targetPos);
            else bob.ArriveAndStay(targetPos);
            onArrival?.Invoke();
        });
    }

    public void ResetAll()
    {
        SilentResetEverything(); // 不传参数，全部复位
        if (bob) bob.ResetState();
    }
}