using UnityEngine;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Clean 3rd-person controller for the playable hero corgi in Botanika.
    /// W/Up = forward (toward the nose, away from a behind-camera), S/Down = back,
    /// A/D = turn, Shift = run. CharacterController-based with gravity, no jump.
    ///
    /// Unlike KafkaDirectController (which negates Vertical for the meadow FBX rig and
    /// thus walks toward the camera), this moves along +transform.forward so it pairs
    /// naturally with KafkaFollowCamera's behind-the-target spring arm — the player
    /// sees the dog's back as it trots into the scene. Legacy Input (activeInputHandler
    /// = Both). Coexists with CorgiStateAnimator on a child mesh (that reads our velocity).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CorgiPlayerController : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField] private float walkSpeed = 2.6f;
        [SerializeField] private float runSpeed = 4.6f;
        [SerializeField] private float turnSpeedDeg = 160f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float gravity = 12f;

        [Header("Animation")]
        [SerializeField] private string isWalkingParam = "IsWalking";
        [SerializeField] private float walkAnimThreshold = 0.15f;

        private CharacterController _cc;
        private Animator _animator;
        private float _speed;
        private float _vy;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>(); // on the corgi mesh child
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float h = Input.GetAxisRaw("Horizontal"); // A/D + arrows → turn
            // D17 flipped this sign after a bug report, but Tim's live playtest (6 Jul
            // evening) showed the flip inverted a correct axis: S drove forward, W back.
            // Restored to the original +Vertical mapping — W is forward again.
            float v = Input.GetAxisRaw("Vertical");   // W/S + arrows → forward/back
            bool run = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Mathf.Abs(h) > 0.01f)
                transform.Rotate(0f, h * turnSpeedDeg * dt, 0f, Space.World);

            float target = Mathf.Clamp(v, -1f, 1f) * (run ? runSpeed : walkSpeed);
            _speed = Mathf.MoveTowards(_speed, target, acceleration * dt);

            if (_cc.isGrounded && _vy < 0f) _vy = -2f;
            else _vy -= gravity * dt;

            Vector3 motion = transform.forward * _speed + Vector3.up * _vy;
            _cc.Move(motion * dt);

            if (_animator != null)
                _animator.SetBool(isWalkingParam, Mathf.Abs(_speed) > walkAnimThreshold);
        }
    }
}
