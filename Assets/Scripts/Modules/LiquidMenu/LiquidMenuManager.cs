using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems; // 🔥 1. 必须引用这个命名空间

public class LiquidMenuManager : MonoBehaviour
{
    public static LiquidMenuManager Instance;

    [Header("--- 顶级菜单 ---")]
    public List<LiquidMenuItem> rootItems;

    private LiquidMenuItem _currentFocus;

    void Awake() => Instance = this;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 🔥🔥【核心修复】UI 防穿透盾牌 🔥🔥
            // 意思就是：如果鼠标正指着 UI (Home键、菜单面板等)，
            // 直接 Return (退出)，不要执行下面的 3D 射线检测！
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return; 
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // 使用 RaycastAll 可以穿透检测，但这里简单的 Raycast 配合 Layer 也可以
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 🔥 【新逻辑】: 优先检查是否点到了“子按钮” (Hotspot)
                var subButton = hit.transform.GetComponent<LiquidSubButton>();
                if (subButton != null)
                {
                    subButton.OnSubButtonClick();
                    return; // 命中了子按钮，直接结束，不往下传
                }

                // 🔥 【旧逻辑】: 检查是否点到了“主菜单图标”
                var item = hit.transform.GetComponent<LiquidMenuItem>(); 
                // 或者 item = hit.transform.GetComponentInParent<LiquidMenuItem>();
                
                if (item != null)
                {
                    // 逻辑修复：切换根节点分支
                    if (item.isRoot && _currentFocus != null && _currentFocus != item && !_currentFocus.transform.IsChildOf(item.transform))
                    {
                         _currentFocus.CloseChildren(); 
                    }
                    
                    item.OnClick(); 
                    return; 
                }
            }

            // 3. 点到了空白处 -> 返回上一级
            if (_currentFocus != null)
            {
                _currentFocus.CloseChildren();
            }
        }
    }

    public void SetCurrentFocus(LiquidMenuItem item) => _currentFocus = item;

    public void HideOtherRoots(LiquidMenuItem activeRoot)
    {
        foreach (var root in rootItems)
        {
            if (root != activeRoot) root.HideAnimate();
        }
    }

    public void ShowAllRoots()
    {
        foreach (var root in rootItems)
        {
            if (!root.gameObject.activeSelf) 
                root.ShowAnimate(0, 0.5f, 1f);
        }
    }
}