using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboClash
{
    public sealed class KeyboardMouseInputSource : IRoboClashInputSource
    {
        private readonly float fallbackMouseSensitivity;

        public KeyboardMouseInputSource(float fallbackMouseSensitivity = 0.12f)
        {
            this.fallbackMouseSensitivity = fallbackMouseSensitivity;
        }

        private RoboClashPhoneInputServer PhoneServer => RoboClashPhoneInputServer.Active;
        private bool PhoneActive => RoboClashPhoneInputServer.IsPhoneActive && PhoneServer != null;

        public Vector2 Move
        {
            get
            {
                var input = Vector2.zero;
                if (PhoneActive)
                {
                    input.x += PhoneServer.CurrentInput.MoveHorizontal;
                    input.y += PhoneServer.CurrentInput.MoveForward;
                }

                if (Keyboard.current != null)
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
                if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
                    return Vector2.zero;

                var sensitivity = RoboClashSession.MouseSensitivity;
                if (float.IsNaN(sensitivity) || float.IsInfinity(sensitivity))
                    sensitivity = Mathf.Clamp(fallbackMouseSensitivity, 0.03f, 0.4f);
                return Mouse.current.delta.ReadValue() * sensitivity;
            }
        }

        public bool FireHeld =>
            PhoneActive && PhoneServer.CurrentInput.Shoot ||
            Mouse.current != null && Cursor.lockState == CursorLockMode.Locked &&
            Mouse.current.leftButton.isPressed;

        public bool JumpPressed =>
            PhoneActive && PhoneServer.ConsumePressed(RoboClashPhoneAction.Jump) ||
            Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        public bool ReloadPressed =>
            PhoneActive && PhoneServer.ConsumePressed(RoboClashPhoneAction.Reload) ||
            Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        public bool AbilityPressed =>
            PhoneActive && PhoneServer.ConsumePressed(RoboClashPhoneAction.Ability) ||
            Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;

        public bool AbilityPressedThisFrame =>
            Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;

        public bool UltimatePressed =>
            PhoneActive && PhoneServer.ConsumePressed(RoboClashPhoneAction.Ultimate) ||
            Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        public bool UltimatePressedThisFrame =>
            Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        public bool PausePressed => Keyboard.current != null &&
                                    (Keyboard.current.pKey.wasPressedThisFrame ||
                                     Keyboard.current.escapeKey.wasPressedThisFrame);

        public bool CursorCapturePressed => Mouse.current != null &&
                                            Mouse.current.leftButton.wasPressedThisFrame;

        public bool CursorUnlockPressed => Keyboard.current != null &&
                                           Keyboard.current.escapeKey.wasPressedThisFrame;

        public void ApplyPhoneAim(ref float yaw, ref float pitch, float yawScale, float pitchScale)
        {
            if (!PhoneActive)
                return;

            var input = PhoneServer.CurrentInput;
            PhoneServer.ConsumeAimDelta(out var yawDelta, out var pitchDelta);
            if (PhoneServer.HasRecentGyroAimDeltas)
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
            var keyboardRequested = Keyboard.current != null &&
                                    Keyboard.current.cKey.wasPressedThisFrame;
            var phoneRequested = PhoneActive && PhoneServer.ConsumeRecenterPressed();
            if (!keyboardRequested && !phoneRequested)
                return false;

            PhoneServer?.RecenterAim();
            return true;
        }
    }
}
