using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ArenaFloor : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = false;
        }
    }
}
