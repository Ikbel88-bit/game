using UnityEngine;

namespace RoboClash
{
    public sealed class ArenaSpawnZone : MonoBehaviour
    {
        [SerializeField] private int spawnIndex;

        public int SpawnIndex => spawnIndex;

        public void SetSpawnIndex(int index)
        {
            spawnIndex = Mathf.Max(0, index);
        }
    }
}
