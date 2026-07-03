using UnityEngine;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Gait-TEST harness only (not used in the playable scene). Drives the CharacterController
    /// straight forward at a SLOW constant speed so a FIXED side camera can capture a clean
    /// single-stride sequence (the follow-camera + slow screenshot rate make that impossible in
    /// the live build). Loops the dog back along Z so it keeps crossing the camera's view.
    /// CorgiStateAnimator reads this CC's velocity → the procedural walk plays as it moves.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class AutoWalk : MonoBehaviour
    {
        public float speed = 0.25f;     // m/s — slow so each stride spans many frames
        public float gravity = 9.81f;
        public float startZ = 3f;
        public float endZ = 7f;

        private CharacterController _cc;
        private float _vy;

        private void Awake() { _cc = GetComponent<CharacterController>(); }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_cc.isGrounded && _vy < 0f) _vy = -2f; else _vy -= gravity * dt;
            _cc.Move((transform.forward * speed + Vector3.up * _vy) * dt);

            if (transform.position.z > endZ)
            {
                var p = transform.position; p.z = startZ; transform.position = p;
            }
        }
    }
}
