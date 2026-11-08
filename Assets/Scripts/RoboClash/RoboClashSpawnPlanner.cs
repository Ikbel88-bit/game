using System.Collections.Generic;
using UnityEngine;

namespace RoboClash
{
    public static class RoboClashSpawnPlanner
    {
        public static List<Vector3> PickSpread(IList<Vector3> candidates, int count,
            float minSeparation = 12f)
        {
            var result = new List<Vector3>(Mathf.Max(0, count));
            if (candidates == null || candidates.Count == 0 || count <= 0)
                return result;

            var pool = new List<Vector3>(candidates);
            var first = Random.Range(0, pool.Count);
            result.Add(pool[first]);
            pool.RemoveAt(first);

            while (result.Count < count && pool.Count > 0)
            {
                var best = 0;
                var bestMinimum = -1f;
                for (var i = 0; i < pool.Count; i++)
                {
                    var minimum = float.MaxValue;
                    for (var r = 0; r < result.Count; r++)
                        minimum = Mathf.Min(minimum, (pool[i] - result[r]).sqrMagnitude);
                    if (minimum > bestMinimum)
                    {
                        bestMinimum = minimum;
                        best = i;
                    }
                }

                result.Add(pool[best]);
                pool.RemoveAt(best);
            }

            var reuse = 0;
            while (result.Count < count)
            {
                var basePoint = candidates[reuse % candidates.Count];
                var offset = Quaternion.Euler(0f, 137.5f * (reuse + 1), 0f) *
                             Vector3.forward * (minSeparation * 0.5f);
                result.Add(basePoint + offset);
                reuse++;
            }

            return result;
        }

        public static Vector3 PickRespawn(IList<Vector3> candidates, IList<Vector3> enemyPositions,
            Vector3 fallback)
        {
            if (candidates == null || candidates.Count == 0)
                return fallback;

            var scored = new List<KeyValuePair<float, Vector3>>(candidates.Count);
            foreach (var candidate in candidates)
            {
                var minimum = float.MaxValue;
                if (enemyPositions != null)
                {
                    foreach (var enemy in enemyPositions)
                        minimum = Mathf.Min(minimum, (candidate - enemy).sqrMagnitude);
                }

                scored.Add(new KeyValuePair<float, Vector3>(minimum, candidate));
            }

            scored.Sort((left, right) => right.Key.CompareTo(left.Key));
            return scored[Random.Range(0, Mathf.Min(3, scored.Count))].Value;
        }

        public static Quaternion FaceTarget(Vector3 from, Vector3 target)
        {
            var direction = target - from;
            direction.y = 0f;
            return direction.sqrMagnitude < 0.01f
                ? Quaternion.identity
                : Quaternion.LookRotation(direction.normalized);
        }
    }
}
