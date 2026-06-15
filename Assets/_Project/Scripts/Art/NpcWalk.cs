using UnityEngine;

namespace Afterhumans.Art
{
    /// <summary>
    /// BOT-N11: Simple ping-pong patrol for NPCs that pace (GDD: Стас «ходит
    /// туда-сюда у двери»). Translates along an axis ±range from the start pose,
    /// turns to face travel direction. Headless-safe (pure transform, no nav).
    ///
    /// Mutually exclusive with NpcIdleBob on the same object (both drive the
    /// transform) — pacing NPCs use this, stationary NPCs use NpcIdleBob.
    /// </summary>
    public class NpcWalk : MonoBehaviour
    {
        [Tooltip("Local-space direction of the walk (will be normalized).")]
        public Vector3 axis = Vector3.right;
        [Tooltip("Half-length of the patrol path in meters (±range from start).")]
        public float range = 1.6f;
        [Tooltip("Walking speed in m/s.")]
        public float speed = 0.6f;
        public bool faceTravel = true;

        private Vector3 _start;
        private float _t;

        private void Start()
        {
            _start = transform.position;
        }

        private void Update()
        {
            _t += Time.deltaTime * speed / Mathf.Max(0.01f, range);
            float off = Mathf.Sin(_t) * range;
            var prev = transform.position;
            var pos = _start + axis.normalized * off;
            transform.position = pos;

            if (faceTravel)
            {
                var d = pos - prev; d.y = 0f;
                if (d.sqrMagnitude > 1e-5f)
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation, Quaternion.LookRotation(d), 5f * Time.deltaTime);
            }
        }
    }
}
