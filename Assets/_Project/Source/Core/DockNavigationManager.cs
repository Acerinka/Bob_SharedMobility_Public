using UnityEngine;
using System.Collections.Generic;

namespace Bob.SharedMobility
{
    public class DockNavigationManager : MonoBehaviour
    {
        public static DockNavigationManager Instance;

        [Header("--- 🔗 引用 ---")]
        public BobController bobActor; 
        public MapViewController mapController; 

        private DockPanelController _currentActiveApp;

        void Awake()
        {
            Instance = this;
        }

        // 🔥 升级版：接受子菜单参数
        public void SwitchToApp(DockPanelController targetApp, CanvasGroup subMenu = null)
        {
            // 如果再次点击同一个已打开的 App，且没有指定跳三级，则视为关闭
            if (_currentActiveApp == targetApp && subMenu == null)
            {
                CloseCurrentApp();
                return; 
            }

            // 如果切了新App，先把旧的关干净
            if (_currentActiveApp != null && _currentActiveApp != targetApp) 
                _currentActiveApp.CloseEntireApp(); 

            if (targetApp != null)
            {
                _currentActiveApp = targetApp;

                if (subMenu != null)
                {
                    Debug.Log("🚀 检测到三级菜单指令，直接跳转...");
                    targetApp.OpenSpecificLevel3(subMenu);
                }
                else
                {
                    targetApp.OpenLevel2Menu();
                }

                // Bob 避让逻辑
                if (bobActor)
                {
                    if (targetApp.causesBobAvoid) bobActor.DodgeDown();
                    else bobActor.ReturnToIdleDelayed();
                }
            }
        }

        // 🔥🔥🔥【关键修复】只关闭 App，不碰地图！
        public void CloseCurrentApp()
        {
            Debug.Log("📢 BobInteractionDirector 关闭当前 App...");

            if (_currentActiveApp != null)
            {
                _currentActiveApp.CloseEntireApp();
                _currentActiveApp = null;
            }

            // ❌ 删除：原来的代码在这里强制把地图切回 Small。
            // 现在这行逻辑只属于 Home 按钮 (在 DockButtonController.cs 里已经写了)，
            // 普通 App 关闭时不应该影响地图状态。
            
            /* if (mapController != null && mapController.currentState != MapViewController.ViewState.Small_Icon)
            {
                 mapController.SwitchToState(MapViewController.ViewState.Small_Icon);
            }
            */

            // Bob 恢复待机
            if (bobActor)
            {
                bobActor.ReturnToIdleDelayed();
            }
        }
    }
}