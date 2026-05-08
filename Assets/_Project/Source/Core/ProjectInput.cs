using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Bob.SharedMobility
{
    public static class ProjectInput
    {
        public static bool WasKeyPressed(KeyCode key)
        {
            return key != KeyCode.None && Input.GetKeyDown(key);
        }

        public static bool WasPrimaryPointerPressed()
        {
            return Input.GetMouseButtonDown(0);
        }

        public static bool IsPrimaryPointerHeld()
        {
            return Input.GetMouseButton(0);
        }

        public static bool WasPrimaryPointerReleased()
        {
            return Input.GetMouseButtonUp(0);
        }

        public static Vector2 PointerPosition => Input.mousePosition;

        public static bool WasPrimaryActionPressed()
        {
            return WasKeyPressed(KeyCode.Return)
                || GamepadButtonReader.WasPressedThisFrame(MyGamepadButton.ButtonSouth);
        }

        public static bool WasCancelActionPressed()
        {
            return WasKeyPressed(KeyCode.Escape)
                || GamepadButtonReader.WasPressedThisFrame(MyGamepadButton.ButtonEast);
        }

        public static bool WasRestartPressed()
        {
            return WasKeyPressed(KeyCode.R)
                || WasGamepadStartPressed()
                || GamepadButtonReader.WasPressedThisFrame(MyGamepadButton.ButtonEast);
        }

#if ENABLE_INPUT_SYSTEM
        public static bool IsKeyboardKeyPressed(Key key)
        {
            return Keyboard.current != null && Keyboard.current[key].isPressed;
        }

        public static bool IsVoiceCommandPressed(Key keyboardKey, VoiceCommandButton gamepadButton)
        {
            return IsKeyboardKeyPressed(keyboardKey)
                || GamepadButtonReader.IsPressed(gamepadButton);
        }
#endif

        private static bool WasGamepadStartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
#else
            return false;
#endif
        }
    }
}
