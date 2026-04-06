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
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float maxFallSpeed = 10f;

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
        private float _lookFreezeTimer;
        private Quaternion _frozenBodyRot;
        private bool _debugLogged;

        /// <summary>Force the camera pitch and freeze look for duration (called by cinematic director after handoff)</summary>
        public void SetPitch(float pitch, float freezeDuration = 1.5f)
        {
            _pitch = pitch;
            _lookFreezeTimer = freezeDuration;
            _frozenBodyRot = transform.rotation; // lock current body rotation
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
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
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void OnEnable()
        {
            // Sync _pitch from current camera rotation so cinematic handoff
            // doesn't snap back to horizontal on first HandleLook() frame.
            if (cameraTransform != null)
            {
                _pitch = cameraTransform.localEulerAngles.x;
                if (_pitch > 180f) _pitch -= 360f;
            }

            // Reset velocity to prevent accumulated gravity from cinematic period
            _velocity = Vector3.zero;
            _debugLogged = false;

            // Re-lock cursor
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && enabled)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
        }

        private void HandleLook()
        {
            if (cameraTransform == null) return;

            // Freeze look after cinematic handoff to prevent cursor lock delta burst
            if (_lookFreezeTimer > 0f)
            {
                _lookFreezeTimer -= Time.deltaTime;
                transform.rotation = _frozenBodyRot; // hold body rotation
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
                // Consume mouse input so it doesn't accumulate
                Input.GetAxisRaw("Mouse X");
                Input.GetAxisRaw("Mouse Y");
                return;
            }

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

            // Gravity with velocity cap
            if (_controller.isGrounded)
            {
                _velocity.y = -0.5f; // push into ground to maintain contact
            }
            else
            {
                _velocity.y -= gravity * Time.deltaTime;
                _velocity.y = Mathf.Max(_velocity.y, -maxFallSpeed);
            }

            Vector3 total = horizMove + new Vector3(0f, _velocity.y, 0f);
            _controller.Move(total * Time.deltaTime);

            // Out-of-bounds recovery: teleport back to spawn if fallen below floor
            if (transform.position.y < -3f)
            {
                Debug.LogWarning($"[FPS] Out of bounds at y={transform.position.y:F1} — teleporting to spawn");
                _controller.enabled = false;
                transform.position = new Vector3(0f, 0.5f, -4f);
                _velocity = Vector3.zero;
                _controller.enabled = true;
            }

            if (!_debugLogged || Time.frameCount % 300 == 0)
            {
                Debug.Log($"[FPS] pos={transform.position}, camLocal={cameraTransform?.localPosition}, camWorld={cameraTransform?.position}, grounded={_controller.isGrounded}, pitch={_pitch}, vel.y={_velocity.y:F2}");
                _debugLogged = true;
            }

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

    }
}
