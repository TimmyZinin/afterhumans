using UnityEngine;

namespace Afterhumans.Player
{
    /// <summary>
    /// Minimal first-person controller for walking skeleton.
    /// CharacterController-based, no jumping (narrative walker — no jump).
    /// WASD + mouse look. Shift to sprint.
    /// Camera is a child transform, pitch is applied to it, yaw to the body.
    ///
    /// This is a standalone implementation — we intentionally avoid Unity Starter Assets
    /// dependency because Asset Store packages can't be installed via batchmode.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimpleFirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.0f;
        [SerializeField] private float sprintSpeed = 3.5f;
        [SerializeField] private float acceleration = 15f;
        [SerializeField] private float gravity = 20f;

        [Header("Look")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 2.5f;
        [SerializeField] private float maxPitchDegrees = 85f;

        [Header("Head Bob")]
        [SerializeField] private float bobFrequency = 8f;
        [SerializeField] private float bobAmplitude = 0.04f;

        private CharacterController _controller;
        private Vector3 _velocity;
        private float _currentSpeed;
        private float _pitch;
        private float _bobTime;
        private Vector3 _cameraBasePosition;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }
            if (cameraTransform != null)
            {
                _cameraBasePosition = cameraTransform.localPosition;
            }
        }

        private void Start()
        {
            // Lock cursor and hide — first-person standard
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
        }

        private void HandleLook()
        {
            if (cameraTransform == null) return;

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            // Yaw on body
            transform.Rotate(Vector3.up, mouseX, Space.World);

            // Pitch on camera
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -maxPitchDegrees, maxPitchDegrees);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
            Vector3 worldDir = transform.TransformDirection(inputDir);

            bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float targetSpeed = inputDir.sqrMagnitude > 0.1f ? (sprint ? sprintSpeed : walkSpeed) : 0f;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, acceleration * Time.deltaTime);

            Vector3 horizMove = worldDir * _currentSpeed;

            // Gravity
            if (_controller.isGrounded)
            {
                _velocity.y = -0.1f; // tiny downward to stay grounded
            }
            else
            {
                _velocity.y -= gravity * Time.deltaTime;
            }

            Vector3 total = horizMove + new Vector3(0f, _velocity.y, 0f);
            _controller.Move(total * Time.deltaTime);

            // Head bob when walking
            if (cameraTransform != null)
            {
                if (_currentSpeed > 0.1f)
                {
                    _bobTime += Time.deltaTime * bobFrequency * (_currentSpeed / walkSpeed);
                    float bobY = Mathf.Sin(_bobTime) * bobAmplitude;
                    cameraTransform.localPosition = _cameraBasePosition + new Vector3(0f, bobY, 0f);
                }
                else
                {
                    cameraTransform.localPosition = Vector3.Lerp(
                        cameraTransform.localPosition,
                        _cameraBasePosition,
                        Time.deltaTime * 6f);
                }
            }

            // Escape toggles cursor lock (re-lock on left click)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
            if (UnityEngine.Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }

        // Re-lock cursor when window regains focus
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && enabled)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }
    }
}
