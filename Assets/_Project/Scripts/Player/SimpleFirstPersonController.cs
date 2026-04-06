using UnityEngine;

namespace Afterhumans.Player
{
    /// <summary>
    /// v2: Clean first-person controller. WASD + mouse look + Shift sprint.
    /// No freeze hacks, no cinematic handoff, no OOB recovery.
    /// CharacterController-based, no jumping (narrative walker).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimpleFirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.0f;
        [SerializeField] private float sprintSpeed = 3.5f;
        [SerializeField] private float gravity = 9.81f;

        [Header("Look")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float maxPitchDegrees = 85f;

        [Header("Head Bob")]
        [SerializeField] private float bobFrequency = 8f;
        [SerializeField] private float bobAmplitude = 0.04f;

        private CharacterController _controller;
        private float _pitch;
        private float _velocityY;
        private float _currentSpeed;
        private float _bobTime;
        private Vector3 _cameraBasePos;
        private int _ignoreMouseFrames;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }
            if (cameraTransform != null)
                _cameraBasePos = cameraTransform.localPosition;
        }

        private void Start()
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void OnEnable()
        {
            _ignoreMouseFrames = 5;
            _velocityY = 0;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();

            // Escape toggles cursor
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
            if (UnityEngine.Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
                _ignoreMouseFrames = 5;
            }
        }

        private void HandleLook()
        {
            if (cameraTransform == null) return;

            // Skip mouse input for a few frames after lock to avoid delta burst
            if (_ignoreMouseFrames > 0)
            {
                _ignoreMouseFrames--;
                Input.GetAxisRaw("Mouse X"); // consume
                Input.GetAxisRaw("Mouse Y");
                return;
            }

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up, mouseX, Space.World);

            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -maxPitchDegrees, maxPitchDegrees);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        private void HandleMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 inputDir = new Vector3(h, 0, v).normalized;
            Vector3 worldDir = transform.TransformDirection(inputDir);

            bool sprint = Input.GetKey(KeyCode.LeftShift);
            float targetSpeed = inputDir.sqrMagnitude > 0.1f ? (sprint ? sprintSpeed : walkSpeed) : 0;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, 15f * Time.deltaTime);

            // Gravity
            if (_controller.isGrounded)
                _velocityY = -0.5f;
            else
                _velocityY -= gravity * Time.deltaTime;
            _velocityY = Mathf.Max(_velocityY, -20f);

            Vector3 move = worldDir * _currentSpeed + Vector3.up * _velocityY;
            _controller.Move(move * Time.deltaTime);

            // Head bob
            if (cameraTransform != null)
            {
                if (_currentSpeed > 0.1f)
                {
                    _bobTime += Time.deltaTime * bobFrequency;
                    float bobY = Mathf.Sin(_bobTime) * bobAmplitude;
                    cameraTransform.localPosition = _cameraBasePos + new Vector3(0, bobY, 0);
                }
                else
                {
                    cameraTransform.localPosition = Vector3.Lerp(
                        cameraTransform.localPosition, _cameraBasePos, Time.deltaTime * 6f);
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && enabled)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
                _ignoreMouseFrames = 5;
            }
        }
    }
}
