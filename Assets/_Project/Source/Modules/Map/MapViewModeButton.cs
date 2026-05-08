using UnityEngine;

namespace Bob.SharedMobility
{
    public class MapViewModeButton : MonoBehaviour
    {
        [Header("--- 按钮功能设置 ---")]
        [Tooltip("点击这个按钮，地图应该变成什么状态？")]
        public MapViewController.ViewState targetState;

        [Header("--- 视觉反馈 (可选) ---")]
        public Transform visualIcon; // 比如按钮上的文字或图标

        // 当被点击时，这个函数会被 GestureController 调用
        public void OnClick(MapViewController controller)
        {
            Debug.Log($"🎯 子按钮被点击: 切换到 {targetState}");
            
            // 1. 执行切换
            controller.SwitchToState(targetState);
            
            // 2. 关闭菜单
            controller.CloseSelectorMenu();
        }
    }
}