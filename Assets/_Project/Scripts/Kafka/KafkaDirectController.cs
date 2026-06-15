using UnityEngine;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// Player-as-Kafka 3rd-person controller for the sandbox meadow scene.
    /// Camera-relative WASD: W goes WHERE THE CAMERA LOOKS (modern 3rd-person feel),
    /// the dog turns TO FACE its move direction. A/D additionally yaw the body so it
    /// still works if the camera never moves. Shift = sprint.
    ///
    /// POINTER-LOCK (WebGL fix): the Cinemachine FreeLook reads Mouse X/Y, but in a
    /// browser the cursor isn't captured until the canvas is clicked → the camera
    /// stays glued to Kafka's nape (recenter) and the game looks "in your face / from
    /// above, unplayable". First left-click locks the cursor (Mouse X/Y start flowing
    /// → FreeLook orbits); Esc releases it.
    ///
    /// Uses the legacy Input Manager (Input.GetAxis / Input.GetKey) because the
    /// project is configured with ProjectSettings.activeInputHandlers = 0 (Old).
    /// Don't replace with the New Input System wrapper here — it silently fails
    /// in standalone builds when that setting is 0.
    ///
    /// Compat with CorgiStateAnimator: that animator reads the CharacterController's
    /// .velocity (NOT _currentSpeed) and projects it onto transform.forward to decide
    /// gait cadence + direction. Because the dog rotates to face its move vector, that
    /// projection is positive while moving → gait plays forward, paws plant correctly.
    /// We still keep _currentSpeed (used here for the Animator IsWalking / clip speed).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class KafkaDirectController : MonoBehaviour
    {
        [Header("Locomotion")]
        // Tim: "топает-семенит" + контроль хуже. walkSpeed 2.5 ÷ strideLength = ~6 шагов/сек =
        // семенит. Сбавил скорость → каденс падает до уверенного шага, контроль спокойнее.
        [SerializeField] private float walkSpeed = 1.4f;
        [SerializeField] private float runSpeed = 3.0f;
        [SerializeField] private float turnSpeedDeg = 180f;
        [SerializeField] private float acceleration = 12f;

        [Header("3rd-person camera-relative")]
        [Tooltip("How fast the dog rotates to face its (camera-relative) move direction.")]
        [SerializeField] private float faceTurnSpeedDeg = 360f;
        [Tooltip("Lock the cursor on first click so Cinemachine FreeLook gets Mouse X/Y (WebGL fix).")]
        [SerializeField] private bool lockCursorOnClick = true;

        [Header("Physics")]
        [SerializeField] private float gravity = 9.81f;

        [Header("Animator")]
        [SerializeField] private string isWalkingParam = "IsWalking";
        [SerializeField] private float walkAnimThreshold = 0.1f;

        private CharacterController _cc;
        private Animator _animator;
        private float _currentSpeed;
        private float _verticalVelocity;

        // CAMERA TELEMETRY (acceptance evidence, not eyeballing a screenshot). Logs the
        // ACTUAL follow-camera distance + downward pitch to the dog once per second so the
        // fix can be CONFIRMED with numbers via the browser console (CDP). Expect a healthy
        // 3rd-person: dist ~6-7 m, pitchDeg ~10-25 (NOT <2.5 m в упор, NOT >55 top-down).
        private float _probeTimer;
        private Camera _probeCam;

        // SCRIPTED DETERMINISTIC FOLLOW CAMERA. The Cinemachine FreeLook recenter kept
        // parking the camera on the NOSE side (measured camSide=-0.93) because its heading
        // logic fought the 180° mesh offset — three different FreeLook settings failed. So we
        // drive Camera.main ourselves: ALWAYS behind the TAIL (+transform.forward; nose is at
        // -transform.forward, measured) at a normal ~3 m, looking at the dog. Mouse X orbits.
        // The CinemachineBrain is disabled so it can't fight our transform writes.
        [Header("Follow camera (scripted)")]
        [SerializeField] private float camDistance = 3.0f;   // metres behind the tail
        [SerializeField] private float camHeight = 1.35f;    // metres above the dog
        [SerializeField] private float camLookHeight = 0.45f;// look-at point above the dog's root
        [SerializeField] private float camLerp = 9f;         // position smoothing
        [SerializeField] private float camMouseYawSpeed = 2.4f;
        private float _camYaw;            // mouse-orbit yaw offset around the dog
        private Behaviour _camBrain;      // CinemachineBrain, disabled so we own the transform
        private bool _camInit;

#if UNITY_EDITOR
        // Debug scene fly-around (press C to enter, X to exit). EDITOR-ONLY — stripped from
        // the WebGL/standalone player so a stray C never detaches the camera from the dog.
        // Additive, OFF by default — disables the Cinemachine brain and drives Camera.main by
        // keys so we can inspect the ceiling/festoon/NPCs/god-rays from any angle.
        private bool _fly;
        private Camera _flyCam;
        private Behaviour _brain;
        private float _fYaw = 180f, _fPitch = 16f, _fRad = 10f, _fH = 2.6f;
        private Vector3 _fCenter = new Vector3(0f, 1.1f, -3f);
#endif

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
                Debug.LogWarning("[KafkaDirectController] No Animator found in children.");
        }

        private void Update()
        {
            HandleCursorLock();

#if UNITY_EDITOR
            // EDITOR-ONLY debug fly toggle. In the player this whole block is stripped, so the
            // camera is ALWAYS the Cinemachine follow rig on the dog — the player can't fly off.
            if (Input.GetKeyDown(KeyCode.C)) { EnterFly(); }
            if (Input.GetKeyDown(KeyCode.X)) { ExitFly(); }
            if (_fly) { FlyUpdate(); return; }
#endif

            float dt = Time.deltaTime;

            // TANK steering (proven scheme — dog faces NOSE-forward). The corgi mesh is
            // parented with a 180° visual offset, so the nose reads as -root.forward; the
            // negated Vertical makes W drive the dog along its visible nose. (The earlier
            // camera-relative LookRotation rotated root.forward to moveDir → nose pointed at
            // the camera → "собака ходит попой вперёд". Reverted.) Camera is still freely
            // mouse-orbited via Cinemachine FreeLook + pointer-lock above.
            float horizontal = Input.GetAxisRaw("Horizontal"); // A/D → turn
            float vertical = -Input.GetAxisRaw("Vertical");    // W/S → forward/back (negated to match mesh facing)
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
            {
                bool moving = Mathf.Abs(_currentSpeed) > walkAnimThreshold;
                _animator.SetBool(isWalkingParam, moving);
                // FOOT-SLIDE FIX (G2): clip playback ∝ ground speed so paws plant, not skate.
                // SIGNED so walking BACKWARD (S) plays the cycle in REVERSE → no moonwalk
                // (Tim: "назад идёт лунной походкой"). Forward → +speed, back → -speed.
                if (moving)
                {
                    float s = _currentSpeed / Mathf.Max(0.1f, walkSpeed);          // signed ratio
                    float mag = Mathf.Clamp(Mathf.Abs(s), 0.7f, 2.5f);
                    _animator.speed = mag * Mathf.Sign(s == 0f ? 1f : s);
                }
                else _animator.speed = 1f;
            }
        }

        // POINTER-LOCK (WebGL fix). Until the browser captures the cursor, Cinemachine
        // FreeLook receives no Mouse X/Y → camera can't orbit and recenter pins it to the
        // dog's nape ("в упор/сверху, непонятно как играть"). First click on the canvas
        // locks + hides the cursor → Mouse X/Y flow → FreeLook works. Esc releases it
        // (also auto-released by the browser when leaving fullscreen). Debug-fly (C/X)
        // doesn't need the lock, so we skip while flying to keep the cursor usable there.
        private void HandleCursorLock()
        {
#if UNITY_EDITOR
            // Debug-fly frees the cursor; skip the click-lock while flying (editor only).
            if (!lockCursorOnClick || _fly) return;
#else
            if (!lockCursorOnClick) return;
#endif

            if (Input.GetMouseButtonDown(0) && UnityEngine.Cursor.lockState != CursorLockMode.Locked)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }
        }

#if UNITY_EDITOR
        private void EnterFly()
        {
            // Free the cursor for debug-fly so Tim can still use the mouse / leave easily.
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            _fly = true;
            if (_flyCam == null) _flyCam = Camera.main;
            if (_flyCam != null && _brain == null)
                _brain = _flyCam.GetComponent("CinemachineBrain") as Behaviour;
            if (_brain != null) _brain.enabled = false;
            Debug.Log("[KafkaDirectController] DEBUG FLY ON (J/L yaw, I/K pitch, U/O zoom, Y/H height, WASD pan; X to exit)");
        }

        private void ExitFly()
        {
            _fly = false;
            if (_brain != null) _brain.enabled = true;
            Debug.Log("[KafkaDirectController] DEBUG FLY OFF (Cinemachine restored)");
        }
#endif

        private void LateUpdate()
        {
#if UNITY_EDITOR
            // Re-assert camera transform AFTER the brain's own LateUpdate, in case it ran
            // before we disabled it on the toggle frame. EDITOR-ONLY (debug fly).
            if (_fly && _flyCam != null)
            {
                Quaternion q = Quaternion.Euler(_fPitch, _fYaw, 0f);
                _flyCam.transform.position = _fCenter + Vector3.up * _fH + q * new Vector3(0f, 0f, -_fRad);
                _flyCam.transform.LookAt(_fCenter + Vector3.up * 0.6f);
            }
#endif

            // ---- SCRIPTED FOLLOW CAMERA (deterministic, behind the TAIL) ----
            if (!_camInit)
            {
                _probeCam = Camera.main;
                if (_probeCam != null)
                {
                    _camBrain = _probeCam.GetComponent("CinemachineBrain") as Behaviour;
                    if (_camBrain != null) _camBrain.enabled = false; // we own the transform now
                }
                _camInit = true;
            }
#if UNITY_EDITOR
            if (_fly) { /* debug-fly owns the camera; skip scripted follow */ }
            else
#endif
            if (_probeCam != null)
            {
                // Mouse orbit (only while pointer-locked so a stray cursor doesn't spin it).
                if (UnityEngine.Cursor.lockState == CursorLockMode.Locked)
                    _camYaw += Input.GetAxis("Mouse X") * camMouseYawSpeed;

                Vector3 dog = transform.position;
                // TAIL direction = +transform.forward (nose is -forward, measured via camSide).
                // Add the mouse-orbit yaw around world-up so the player can look around.
                Vector3 backDir = Quaternion.AngleAxis(_camYaw, Vector3.up) * transform.forward;
                Vector3 targetPos = dog + backDir * camDistance + Vector3.up * camHeight;
                float t = 1f - Mathf.Exp(-camLerp * Time.deltaTime);
                _probeCam.transform.position = Vector3.Lerp(_probeCam.transform.position, targetPos, t);
                _probeCam.transform.rotation = Quaternion.LookRotation(
                    (dog + Vector3.up * camLookHeight) - _probeCam.transform.position, Vector3.up);
            }

            // RUNTIME camera telemetry (runs in the BUILD too, not just editor). Emitted
            // after our scripted follow has positioned the camera this frame.
            _probeTimer += Time.deltaTime;
            if (_probeTimer >= 1f)
            {
                _probeTimer = 0f;
                if (_probeCam == null) _probeCam = Camera.main;
                if (_probeCam != null)
                {
                    Vector3 cam = _probeCam.transform.position;
                    Vector3 dog = transform.position;
                    Vector3 to = dog - cam;
                    float dist = to.magnitude;
                    // downward pitch of the camera's forward axis (0 = horizontal, 90 = straight down)
                    float pitchDeg = -_probeCam.transform.eulerAngles.x;
                    if (pitchDeg < -180f) pitchDeg += 360f;
                    pitchDeg = -pitchDeg; // positive = looking down
                    // FACING CHECK: the corgi mesh nose = -transform.forward, so the TAIL is at
                    // +transform.forward. camSide = which side of the dog the camera sits on:
                    //   >0 → camera on the TAIL side (we see the dog's back/tail — CORRECT)
                    //   <0 → camera on the NOSE side (we see the dog's FACE — WRONG, the bug)
                    float camSide = Vector3.Dot((cam - dog).normalized, transform.forward);
                    Debug.Log($"[CAMPROBE] dist={dist:F2} pitchDeg={pitchDeg:F1} camY={cam.y:F2} dogY={dog.y:F2} camSide={camSide:F2}");
                }
            }
        }

#if UNITY_EDITOR
        private void FlyUpdate()
        {
            if (_flyCam == null) { _flyCam = Camera.main; if (_flyCam == null) return; }
            float dt = Time.deltaTime;
            if (Input.GetKey(KeyCode.J)) _fYaw -= 70f * dt;
            if (Input.GetKey(KeyCode.L)) _fYaw += 70f * dt;
            if (Input.GetKey(KeyCode.I)) _fPitch += 45f * dt;
            if (Input.GetKey(KeyCode.K)) _fPitch -= 45f * dt;
            if (Input.GetKey(KeyCode.U)) _fRad -= 7f * dt;
            if (Input.GetKey(KeyCode.O)) _fRad += 7f * dt;
            if (Input.GetKey(KeyCode.Y)) _fH += 3.5f * dt;
            if (Input.GetKey(KeyCode.H)) _fH -= 3.5f * dt;
            Quaternion flat = Quaternion.Euler(0f, _fYaw, 0f);
            if (Input.GetKey(KeyCode.W)) _fCenter += flat * Vector3.forward * 4f * dt;
            if (Input.GetKey(KeyCode.S)) _fCenter -= flat * Vector3.forward * 4f * dt;
            if (Input.GetKey(KeyCode.A)) _fCenter -= flat * Vector3.right * 4f * dt;
            if (Input.GetKey(KeyCode.D)) _fCenter += flat * Vector3.right * 4f * dt;
            _fPitch = Mathf.Clamp(_fPitch, -12f, 82f);
            _fRad = Mathf.Clamp(_fRad, 1.5f, 32f);
            _fH = Mathf.Clamp(_fH, 0.2f, 16f);
        }
#endif
    }
}
