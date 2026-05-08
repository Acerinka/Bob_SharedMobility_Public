using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.InputSystem.Controls; // 👈 补上了这行关键引用！

namespace Bob.SharedMobility
{
    public class GamepadInputDebugger : MonoBehaviour
    {
        void Update()
        {
            // 如果当前没有手柄连接，就不检测
            if (Gamepad.current == null) return;

            // 遍历手柄上所有的“控件”
            foreach (InputControl control in Gamepad.current.allControls)
            {
                // 现在 ButtonControl 应该能被识别了
                // 我们只检测那些被按下（wasPressedThisFrame）的按钮
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    Debug.Log($"<color=yellow>>>> 找到了！你按下的键位是: {control.name} <<<</color>");
                    Debug.Log($"完整路径: {control.path}");
                }
            }
        }
    }
}