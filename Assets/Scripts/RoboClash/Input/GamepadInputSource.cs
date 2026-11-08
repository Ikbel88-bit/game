using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboClash
{
    public sealed class GamepadInputSource : IRoboClashInputSource
    {
        private readonly int padIndex;
        private const float LookDegreesPerSecond = 160f;
        private const float Deadzone = 0.15f;

        public GamepadInputSource(int padIndex)
        {
            this.padIndex = padIndex;
        }

        private Gamepad Pad => padIndex >= 0 && padIndex < Gamepad.all.Count
            ? Gamepad.all[padIndex]
            : null;

        private static Vector2 Dead(Vector2 value)
        {
            return value.magnitude < Deadzone ? Vector2.zero : value;
        }

        public Vector2 Move => Pad != null ? Dead(Pad.leftStick.ReadValue()) : Vector2.zero;
        public Vector2 LookDelta => Pad != null
            ? Dead(Pad.rightStick.ReadValue()) * LookDegreesPerSecond * Time.deltaTime
            : Vector2.zero;
        public bool FireHeld => Pad != null && Pad.rightTrigger.ReadValue() > 0.5f;
        public bool JumpPressed => Pad != null && Pad.buttonSouth.wasPressedThisFrame;
        public bool ReloadPressed => Pad != null && Pad.buttonWest.wasPressedThisFrame;
        public bool AbilityPressed => Pad != null && Pad.leftShoulder.wasPressedThisFrame;
        public bool UltimatePressed => Pad != null && Pad.rightShoulder.wasPressedThisFrame;
        public bool PausePressed => Pad != null && Pad.startButton.wasPressedThisFrame;
    }
}
