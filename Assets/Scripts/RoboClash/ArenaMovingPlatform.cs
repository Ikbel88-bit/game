using UnityEngine;

namespace RoboClash
{
    public sealed class ArenaMovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, 8f);
        [SerializeField, Min(0.1f)] private float cycleDuration = 5f;

        private Vector3 startPosition;
        private Rigidbody body;
        public Vector3 Velocity { get; private set; }

        private void Awake()
        {
            startPosition = transform.position;
            body = GetComponent<Rigidbody>();
        }

        public void Configure(Vector3 offset, float duration)
        {
            localOffset = offset;
            cycleDuration = Mathf.Max(0.1f, duration);
        }

        private void FixedUpdate()
        {
            var phase = Mathf.PingPong(Time.time / cycleDuration, 1f);
            var nextPosition = startPosition + localOffset * phase;
            Velocity = (nextPosition - transform.position) / Time.fixedDeltaTime;
            if (body != null && body.isKinematic)
                body.MovePosition(nextPosition);
            else
                transform.position = nextPosition;
        }
    }
}
