using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// 🔥 挂载在这个脚本所在的 Canvas 上
public class UIInteractionFixer : MonoBehaviour
{
    [Header("--- 调试 ---")]
    [Tooltip("勾选后，点击鼠标会打印出你到底点到了谁")]
    public bool debugClick = true;

    [Header("--- 自动修复 ---")]
    [Tooltip("勾选后，Start时会自动把 MainCamera 塞给 Canvas")]
    public bool autoAssignCamera = true;
    
    [Tooltip("勾选后，Start时会自动把所有【非按钮】图片的射线检测关掉，防止遮挡")]
    public bool autoDisableBlockingImages = true;

    void Start()
    {
        // 1. 修复相机丢失问题
        if (autoAssignCamera)
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
                Debug.Log("🔧 [UI修复] 已强制将 MainCamera 赋给 Canvas！");
            }
        }

        // 2. 修复遮挡问题 (暴力清理)
        if (autoDisableBlockingImages)
        {
            FixRaycastTargets();
        }
    }

    void Update()
    {
        // 3. 实时调试：到底点到了谁？
        if (debugClick && Input.GetMouseButtonDown(0))
        {
            CheckWhatIsClicked();
        }
    }

    // 🔥 核心功能：看看谁挡住了射线
    void CheckWhatIsClicked()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
        {
            Debug.Log($"👇 --- 鼠标点击位置检测到 {results.Count} 个UI物体 ---");
            foreach (var result in results)
            {
                string status = result.gameObject.GetComponent<Button>() ? "✅ [按钮]" : "❌ [挡路物体]";
                Debug.Log($"{status} 层级: {result.depth} | 物体名: <color=yellow>{result.gameObject.name}</color>");
            }
        }
        else
        {
            Debug.Log("👇 鼠标点击位置没有检测到任何 UI (可能没有 Event Camera 或离得太远)");
        }
    }

    // 🔥 暴力修复：只保留按钮的射线，其他全关掉
    public void FixRaycastTargets()
    {
        // 获取所有 Image
        Image[] allImages = GetComponentsInChildren<Image>(true);
        int fixedCount = 0;

        foreach (var img in allImages)
        {
            // 如果这个 Image 身上没有 Button 组件，且它的父物体也没有 Button 组件
            if (img.GetComponent<Button>() == null && img.GetComponentInParent<Button>() == null)
            {
                // 如果它原本是开着的，我们就把它关了
                if (img.raycastTarget)
                {
                    img.raycastTarget = false;
                    fixedCount++;
                }
            }
            else
            {
                // 如果它是按钮的一部分，必须开启
                if (!img.raycastTarget)
                {
                    img.raycastTarget = true;
                    // Debug.Log($"🔧 修正：开启了按钮 {img.name} 的射线检测");
                }
            }
        }
        
        // 处理 Text (文字有时也会挡住按钮)
        // TMPro.TextMeshProUGUI[] allTexts = GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        // 如果你用的是普通 Text
        Text[] allTexts = GetComponentsInChildren<Text>(true);
        foreach(var t in allTexts)
        {
             if (t.GetComponentInParent<Button>() != null)
             {
                 t.raycastTarget = false; // 按钮里的文字通常不需要阻挡射线，让按钮自己处理
             }
        }

        Debug.Log($"🧹 [UI修复] 已自动关闭 {fixedCount} 个背景图片的 RaycastTarget，防止遮挡。");
    }
}