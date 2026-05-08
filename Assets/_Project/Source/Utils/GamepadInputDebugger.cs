using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Bob.SharedMobility
{
    public class GamepadInputDebugger : MonoBehaviour
    {
        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Gamepad.current == null) return;

            foreach (InputControl control in Gamepad.current.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    ProjectLog.Info($"Gamepad button pressed: {control.name} ({control.path})", this);
                }
            }
#endif
        }
    }
}
