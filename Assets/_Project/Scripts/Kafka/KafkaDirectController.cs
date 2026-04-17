using UnityEngine;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Player-as-Kafka direct WASD controller for the sandbox meadow scene.
    /// W/S = forward/back, A/D = turn, Shift = sprint.
    ///
    /// Uses the legacy Input Manager (Input.GetAxis / Input.GetKey) because the
    /// project is configured with ProjectSettings.activeInputHandlers = 0 (Old).
    /// Don't replace with the New Input System wrapper here — it silently fails
    /// in standalone builds when that setting is 0.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class KafkaDirectController : MonoBehaviour
    {
        [Header("Locomotion")]
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float runSpeed = 5.0f;
        [SerializeField] private float turnSpeedDeg = 180f;
        [SerializeField] private float acceleration = 12f;

        [Header("Physics")]
        [SerializeField] private float gravity = 9.81f;

        [Header("Animator")]
        [SerializeField] private string isWalkingParam = "IsWalking";
        [SerializeField] private float walkAnimThreshold = 0.1f;

        private CharacterController _cc;
        private Animator _animator;
        private float _currentSpeed;
        private float _verticalVelocity;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
                Debug.LogWarning("[KafkaDirectController] No Animator found in children.");
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // W/S are negated to match the FBX rotation (-90° Y); A/D left alone so
            // A turns left and D turns right when viewing Kafka from behind.
            float horizontal = Input.GetAxisRaw("Horizontal"); // A/D + arrows
            float vertical = -Input.GetAxisRaw("Vertical");    // W/S + arrows
            bool sprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            transform.Rotate(0f, horizontal * turnSpeedDeg * dt, 0f, Space.World);

            float targetSpeed = Mathf.Clamp(vertical, -1f, 1f) * (sprinting ? runSpeed : walkSpeed);
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * dt);

            Vector3 horizontalVel = transform.forward * _currentSpeed;

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity -= gravity * dt;

            Vector3 motion = horizontalVel + Vector3.up * _verticalVelocity;
            _cc.Move(motion * dt);

            if (_animator != null)
                _animator.SetBool(isWalkingParam, Mathf.Abs(_currentSpeed) > walkAnimThreshold);
        }
    }
}
