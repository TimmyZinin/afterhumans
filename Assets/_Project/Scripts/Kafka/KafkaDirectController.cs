using UnityEngine;
using UnityEngine.InputSystem;
using Afterhumans.InputSystems;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Player-as-Kafka direct WASD controller for the sandbox meadow scene.
    /// W/S = forward/back, A/D = turn, Shift = sprint. Drives the KafkaAnimator
    /// IsWalking bool so Idle ↔ Walk plays correctly.
    ///
    /// This is separate from KafkaFollowSimple/KafkaFollow (companion behavior).
    /// When this component is attached, Kafka is the player — not a follower.
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
        private Vector2 _moveInput;
        private bool _sprinting;
        private float _currentSpeed;
        private float _verticalVelocity;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
                Debug.LogWarning("[KafkaDirectController] No Animator found in children.");
        }

        private void OnEnable()
        {
            var input = AfterhumansInputWrapper.Instance;
            input.EnableGameplay();
            if (input.Move != null)
            {
                input.Move.performed += OnMove;
                input.Move.canceled += OnMove;
            }
            if (input.Sprint != null)
            {
                input.Sprint.performed += OnSprint;
                input.Sprint.canceled += OnSprint;
            }
        }

        private void OnDisable()
        {
            var input = AfterhumansInputWrapper.Instance;
            if (input.Move != null)
            {
                input.Move.performed -= OnMove;
                input.Move.canceled -= OnMove;
            }
            if (input.Sprint != null)
            {
                input.Sprint.performed -= OnSprint;
                input.Sprint.canceled -= OnSprint;
            }
        }

        private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
        private void OnSprint(InputAction.CallbackContext ctx) => _sprinting = ctx.ReadValueAsButton();

        private void Update()
        {
            float dt = Time.deltaTime;

            float turn = _moveInput.x * turnSpeedDeg * dt;
            transform.Rotate(0f, turn, 0f, Space.World);

            float forwardInput = Mathf.Clamp(_moveInput.y, -1f, 1f);
            float targetSpeed = forwardInput * (_sprinting ? runSpeed : walkSpeed);
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * dt);

            Vector3 horizontal = transform.forward * _currentSpeed;

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity -= gravity * dt;

            Vector3 motion = horizontal + Vector3.up * _verticalVelocity;
            _cc.Move(motion * dt);

            if (_animator != null)
                _animator.SetBool(isWalkingParam, Mathf.Abs(_currentSpeed) > walkAnimThreshold);
        }
    }
}
