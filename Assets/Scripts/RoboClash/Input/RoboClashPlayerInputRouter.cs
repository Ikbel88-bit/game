using UnityEngine;

namespace RoboClash
{
    [DisallowMultipleComponent]
    public sealed class RoboClashPlayerInputRouter : MonoBehaviour
    {
        public IRoboClashInputSource Source { get; set; }
        public int PlayerIndex { get; set; }
    }
}
