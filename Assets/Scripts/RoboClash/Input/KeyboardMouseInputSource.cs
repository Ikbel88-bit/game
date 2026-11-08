using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboClash
{
    public sealed class KeyboardMouseInputSource : IRoboClashInputSource
    {
        private readonly float fallbackMouseSensitivity;
        private readonly int playerIndex;
        private readonly bool allowKeyboard;

        public KeyboardMouseInputSource(float fallbackMouseSensitivity = 0.12f,
            int playerIndex = 0, bool allowKeyboard = true)
        {
            this.fallbackMouseSensitivity = fallbackMouseSensitivity;
            this.playerIndex = Mathf.Max(0, playerIndex);
            this.allowKeyboard = allowKeyboard;
        }

        private RoboClashPhoneInputServer PhoneServer => RoboClashPhoneInputServer.Active;
        private bool PhoneActive => PhoneServer != null && PhoneServer.IsPhoneActiveForPlayer(playerIndex);
        private bool KeyboardActive => allowKeyboard && playerIndex == 0;
        private RoboClashPhoneInput PhoneInput => PhoneServer != null
            ? PhoneServer.GetCurrentInput(playerIndex)
            : default;

        public Vector2 Move
        {
            get
            {
                var input = Vector2.zero;
                if (PhoneActive)
                {
                    input.x += PhoneInput.MoveHorizontal;
                    input.y += PhoneInput.MoveForward;
                }

                if (KeyboardActive && Keyboard.current != null)
                {
                    if (Keyboard.current.wKey.isPressed) input.y += 1f;
                    if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                    if (Keyboard.current.dKey.isPressed) input.x += 1f;
                    if (Keyboard.current.aKey.isPressed) input.x -= 1f;
                }

                return input;
            }
        }

        public Vector2 LookDelta
        {
            get
            {
                if (!KeyboardActive || Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
                    return Vector2.zero;

                var sensitivity = RoboClashSession.MouseSensitivity;
                if (float.IsNaN(sensitivity) || float.IsInfinity(sensitivity))
                    sensitivity = Mathf.Clamp(fallbackMouseSensitivity, 0.03f, 0.4f);
                return Mouse.current.delta.ReadValue() * sensitivity;
            }
        }

        public bool FireHeld =>
            PhoneActive && PhoneInput.Shoot ||
            KeyboardActive && Mouse.current != null && Cursor.lockState == CursorLockMode.Locked &&
            Mouse.current.leftButton.isPressed;

        public bool JumpPressed =>
            PhoneActive && PhoneServer.ConsumePressed(playerIndex, RoboClashPhoneAction.Jump) ||
            KeyboardActive && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        public bool ReloadPressed =>
            PhoneActive && PhoneServer.ConsumePressed(playerIndex, RoboClashPhoneAction.Reload) ||
            KeyboardActive && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        public bool AbilityPressed =>
            PhoneActive && PhoneServer.ConsumePressed(playerIndex, RoboClashPhoneAction.Ability) ||
            KeyboardActive && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;

        public bool AbilityPressedThisFrame =>
            KeyboardActive && Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;

        public bool UltimatePressed =>
            PhoneActive && PhoneServer.ConsumePressed(playerIndex, RoboClashPhoneAction.Ultimate) ||
            KeyboardActive && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        public bool UltimatePressedThisFrame =>
            KeyboardActive && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        public bool PausePressed => KeyboardActive && Keyboard.current != null &&
                                    (Keyboard.current.pKey.wasPressedThisFrame ||
                                     Keyboard.current.escapeKey.wasPressedThisFrame);

        public bool CursorCapturePressed => KeyboardActive && Mouse.current != null &&
                                            Mouse.current.leftButton.wasPressedThisFrame;

        public bool CursorUnlockPressed => KeyboardActive && Keyboard.current != null &&
                                           Keyboard.current.escapeKey.wasPressedThisFrame;

        public void ApplyPhoneAim(ref float yaw, ref float pitch, float yawScale, float pitchScale)
        {
            if (!PhoneActive)
                return;

            var input = PhoneInput;
            PhoneServer.ConsumeAimDelta(playerIndex, out var yawDelta, out var pitchDelta);
            if (PhoneServer.HasRecentGyroAimDeltasForPlayer(playerIndex))
            {
                yaw += yawDelta * yawScale;
                pitch = Mathf.Clamp(pitch - pitchDelta * pitchScale, -20f, 45f);
            }
            else
            {
                yaw += input.GyroTurn * 140f * Time.deltaTime;
                pitch = Mathf.Clamp(pitch - input.GyroPitch * 90f * Time.deltaTime, -20f, 45f);
            }
        }

        public bool ConsumePhoneAimRecenter()
        {
            var keyboardRequested = KeyboardActive && Keyboard.current != null &&
                                    Keyboard.current.cKey.wasPressedThisFrame;
            var phoneRequested = PhoneActive && PhoneServer.ConsumeRecenterPressed(playerIndex);
            if (!keyboardRequested && !phoneRequested)
                return false;

            PhoneServer?.RecenterAim(playerIndex);
            return true;
        }
    }
}
