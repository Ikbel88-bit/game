using UnityEngine.InputSystem;

namespace RoboClash
{
    public static class RoboClashInputAssignment
    {
        public static bool CanAssign(int playerCount, out string message)
        {
            var pads = Gamepad.all.Count;
            var needed = playerCount - 1;
            if (pads >= needed)
            {
                message = null;
                return true;
            }

            message = $"{playerCount} players need {needed} gamepad(s) plus keyboard/mouse. Connected gamepads: {pads}.";
            return false;
        }

        public static IRoboClashInputSource For(int playerIndex, int playerCount)
        {
            var pads = Gamepad.all.Count;
            var allPads = pads >= playerCount;
            if (playerIndex == 0 && !allPads)
                return new KeyboardMouseInputSource();
            return new GamepadInputSource(allPads ? playerIndex : playerIndex - 1);
        }
    }
}
