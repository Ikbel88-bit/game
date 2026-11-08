using UnityEngine;

namespace RoboClash
{
    public interface IRoboClashInputSource
    {
        Vector2 Move { get; }
        Vector2 LookDelta { get; }
        bool FireHeld { get; }
        bool JumpPressed { get; }
        bool ReloadPressed { get; }
        bool AbilityPressed { get; }
        bool UltimatePressed { get; }
        bool PausePressed { get; }
    }
}
