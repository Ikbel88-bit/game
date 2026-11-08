using UnityEngine;

namespace RoboClash
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ArenaWall : MonoBehaviour
    {
        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = false;
        }
    }
}
