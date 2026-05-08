using UnityEngine;

namespace Bob.SharedMobility
{
    public class MapGestureInputController : MonoBehaviour
    {
        [Header("--- 链接 ---")]
        public MapViewController stateController; 

        [Header("--- 参数 ---")]
        public float longPressTime = 0.5f;

        private bool _isPressed = false;
        private float _pressTimer = 0f;
        private bool _hasTriggeredLongPress = false;

        void Update()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
            // 1. 按下检测
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // 🔥 情况 A: 点到了菜单里的【子按钮】
                    var subBtn = hit.transform.GetComponent<MapViewModeButton>();
                    if (subBtn != null)
                    {
                        // 直接执行按钮逻辑，不走后面的长按/单击流程
                        subBtn.OnClick(stateController);
                        return; 
                    }

                    // 🔥 情况 B: 点到了【地图系统本身】(父物体或图标)
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        // 排除掉 Selector_Menu (如果它也在子物体里)，防止误触背景
                        if (stateController.selectorMenu && hit.transform.IsChildOf(stateController.selectorMenu.transform))
                        {
                            // 如果点的是菜单背景但不是按钮，啥也不做，或者关闭菜单
                            return; 
                        }

                        _isPressed = true;
                        _pressTimer = 0f;
                        _hasTriggeredLongPress = false;
                    }
                }
            }

            // 2. 长按逻辑 (仅针对地图本体)
            if (_isPressed && !_hasTriggeredLongPress)
            {
                _pressTimer += Time.deltaTime;
                if (_pressTimer >= longPressTime)
                {
                    _hasTriggeredLongPress = true;
                    if (stateController) stateController.OpenSelectorMenu();
                }
            }

            // 3. 松开逻辑 (单击地图切换)
            if (Input.GetMouseButtonUp(0))
            {
                if (_isPressed)
                {
                    _isPressed = false;
                    if (!_hasTriggeredLongPress)
                    {
                        if (stateController) stateController.CycleNext(); 
                    }
                }
            }
        }
    }
}