using UnityEngine.InputSystem;

namespace Bob.SharedMobility
{
    public static class GamepadButtonReader
    {
        public static bool IsPressed(VoiceCommandButton button)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null || button == VoiceCommandButton.None) return false;

            switch (button)
            {
                case VoiceCommandButton.ButtonSouth: return gamepad.buttonSouth.isPressed;
                case VoiceCommandButton.ButtonEast: return gamepad.buttonEast.isPressed;
                case VoiceCommandButton.ButtonWest: return gamepad.buttonWest.isPressed;
                case VoiceCommandButton.ButtonNorth: return gamepad.buttonNorth.isPressed;
                case VoiceCommandButton.LeftShoulder: return gamepad.leftShoulder.isPressed;
                case VoiceCommandButton.RightShoulder: return gamepad.rightShoulder.isPressed;
                default: return false;
            }
        }

        public static bool WasPressedThisFrame(MyGamepadButton button)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null || button == MyGamepadButton.None) return false;

            switch (button)
            {
                case MyGamepadButton.ButtonSouth: return gamepad.buttonSouth.wasPressedThisFrame;
                case MyGamepadButton.ButtonEast: return gamepad.buttonEast.wasPressedThisFrame;
                case MyGamepadButton.ButtonWest: return gamepad.buttonWest.wasPressedThisFrame;
                case MyGamepadButton.ButtonNorth: return gamepad.buttonNorth.wasPressedThisFrame;
                case MyGamepadButton.DpadUp: return gamepad.dpad.up.wasPressedThisFrame;
                case MyGamepadButton.DpadDown: return gamepad.dpad.down.wasPressedThisFrame;
                case MyGamepadButton.DpadLeft: return gamepad.dpad.left.wasPressedThisFrame;
                case MyGamepadButton.DpadRight: return gamepad.dpad.right.wasPressedThisFrame;
                case MyGamepadButton.LeftShoulder: return gamepad.leftShoulder.wasPressedThisFrame;
                case MyGamepadButton.RightShoulder: return gamepad.rightShoulder.wasPressedThisFrame;
                default: return false;
            }
        }
    }
}
