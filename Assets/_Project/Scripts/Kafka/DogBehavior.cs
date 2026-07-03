using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Afterhumans.Kafka
{
    /// <summary>
    /// LIVING-DOG behaviour layer on top of the procedural <see cref="CorgiStateAnimator"/>.
    /// When the player leaves the dog alone for a while it stops "just walking" and does dog
    /// things — sits, scratches an ear, sniffs the floor, lies down, shakes off — with sounds.
    /// The moment WASD is pressed (or the body starts moving) it blends straight back so control
    /// is NEVER blocked.
    ///
    /// ── LATE-UPDATE OWNERSHIP (the main risk, resolved explicitly) ──────────────────────────
    /// CorgiStateAnimator already writes EVERY bone + the mesh transform every LateUpdate. Two
    /// scripts writing the same bones would race (last writer wins, order undefined). So we use a
    /// SINGLE-OWNER handshake instead of fighting:
    ///   • DogBehavior decides state + blend weight in Update() and raises
    ///     CorgiStateAnimator.ExternalPoseActive whenever it has ANY influence (weight &gt; 0).
    ///   • CorgiStateAnimator early-returns (yields the whole skeleton) while that flag is up.
    ///   • DogBehavior then writes the pose in its own LateUpdate — the sole writer for the frame.
    ///   • [DefaultExecutionOrder(200)] guarantees our LateUpdate runs AFTER the animator's, so
    ///     even on the toggle frame there is no double-write.
    /// Each pose is written ABSOLUTELY every frame as Slerp(restPose, poseTarget, weight): no
    /// incremental drift, blends cleanly from/into the rest pose, so hand-off both ways is seamless.
    ///
    /// Poses reuse the EXACT technique of CorgiStateAnimator: start from the bone's captured base
    /// localRotation, then Rotate() about the movement-root world axes (root.right = sagittal
    /// pitch / fore-aft, Vector3.up = yaw, root.forward = roll) — coordinate-agnostic, so the
    /// arbitrary Tripo bone orientations don't fight us. No Animator controller, no clips.
    ///
    /// This component lives on the ROOT (Hero_Corgi) next to KafkaDirectController + NpcInteractor;
    /// it reaches into the child mesh (Hero_CorgiMesh) for the CorgiStateAnimator + skeleton.
    /// Integration notes for BotanikaBuilder are at the very bottom of this file.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class DogBehavior : MonoBehaviour
    {
        public enum DogState { Idle, Sit, Scratch, Sniff, LieDown, Shake, Sneeze }

        [Header("Idle → auto-behaviour")]
        [Tooltip("After this many seconds of no input & no movement, the dog picks a random behaviour.")]
        [SerializeField] private Vector2 idleTriggerDelay = new Vector2(6f, 10f);
        [Tooltip("|axis| above this counts as a WASD press → instant return to Idle/movement.")]
        [SerializeField] private float inputDeadzone = 0.1f;
        [Tooltip("Body speed (m/s) above which the dog counts as moving (also interrupts a pose).")]
        [SerializeField] private float moveVelThreshold = 0.2f;

        [Header("Pose blend (seconds)")]
        [Tooltip("Ease INTO a pose over this long.")]
        [SerializeField] private float enterBlend = 0.5f;
        [Tooltip("Ease BACK to idle/movement over this long. Short → control feels instant.")]
        [SerializeField] private float exitBlend = 0.28f;

        [Header("Pose hold durations (seconds)")]
        [SerializeField] private Vector2 sitHold = new Vector2(3.5f, 6f);
        [SerializeField] private Vector2 sniffHold = new Vector2(3f, 5f);
        [SerializeField] private Vector2 scratchHold = new Vector2(2f, 3f);
        [SerializeField] private Vector2 lieHold = new Vector2(5f, 9f);
        [SerializeField] private float shakeHold = 1.0f;

        [Header("Auto-pick weights (relative — sum 100 → these ARE the percentages)")]
        [SerializeField] private float wSit = 27f;
        [SerializeField] private float wSniff = 23f;
        [SerializeField] private float wScratch = 18f;
        [SerializeField] private float wShake = 14f;
        [SerializeField] private float wLie = 9f;
        [SerializeField] private float wSneeze = 9f;

        [Header("Sit (haunches down, chest up)")]
        [SerializeField] private float sitHindHipDeg = 42f;    // thigh tucks back/under
        [SerializeField] private float sitHindKneeDeg = 55f;   // stifle folds
        [SerializeField] private float sitBodyPitchDeg = 10f;  // torso tilts nose-up
        [SerializeField] private float sitBodyDrop = 0.06f;    // rear settles (metres)
        [SerializeField] private float sitHeadUpDeg = 6f;

        [Header("Scratch (sits + a hind leg drums behind the ear)")]
        [SerializeField] private float scratchLegLiftDeg = 38f;
        [SerializeField] private float scratchKneeFoldDeg = 52f;
        [SerializeField] private float scratchDrumDeg = 22f;
        [SerializeField] private float scratchDrumHz = 9f;
        [SerializeField] private float scratchHeadTiltDeg = 18f;

        [Header("Sniff (nose to the floor, head sweeps)")]
        [SerializeField] private float sniffHeadDownDeg = 34f;
        [SerializeField] private float sniffHeadYawDeg = 20f;
        [SerializeField] private float sniffYawHz = 1.1f;
        [SerializeField] private float sniffBodyPitchDeg = 3f;
        [SerializeField] private float sniffBodyDrop = 0.03f;

        [Header("Lie down (belly to floor, chin on paws)")]
        [SerializeField] private float lieBodyDrop = 0.17f;
        [SerializeField] private float lieBodyPitchDeg = 4f;
        [SerializeField] private float lieFrontExtendDeg = 30f; // front paws reach forward
        [SerializeField] private float lieHindFoldDeg = 48f;
        [SerializeField] private float lieHeadDownDeg = 26f;

        [Header("Shake (whole-body dry-off, ~1 s)")]
        [SerializeField] private float shakeRollDeg = 12f;
        [SerializeField] private float shakeHz = 12f;
        [SerializeField] private float shakeHeadYawDeg = 14f;
        [SerializeField] private float shakeEarDeg = 22f;

        [Header("Sneeze (windup → sharp nod → damped shake, ~1 s)")]
        [SerializeField] private Vector2 sneezeDuration = new Vector2(0.8f, 1.2f);
        [SerializeField] private float sneezeWindupDeg = 14f;   // nose lifts up-back before the sneeze
        [SerializeField] private float sneezeNodDeg = 30f;      // sharp downward nod = the sneeze
        [SerializeField] private float sneezeShakeDeg = 10f;    // damped head shake after
        [SerializeField] private float sneezeShakeCycles = 3f;
        [SerializeField] private float sneezeEarDeg = 16f;
        [Tooltip("Snappy in/out — the motion itself starts & ends at the rest pose, so a fast blend is safe.")]
        [SerializeField] private float sneezeBlend = 0.08f;
        [Tooltip("Chance to sneeze right after finishing a Sniff (the cute 'sniffed → sneezed' beat).")]
        [SerializeField] private float postSniffSneezeChance = 0.35f;
        [Tooltip("On-the-move sneeze fires no more often than this (seconds). Head-only — never stops movement.")]
        [SerializeField] private Vector2 moveSneezeInterval = new Vector2(60f, 90f);

        [Header("Audio")]
        [Tooltip("0 = pure 2D, 1 = full 3D. 0.4 keeps footsteps present but slightly positioned.")]
        [SerializeField] private float audioSpatialBlend = 0.4f;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private AudioClip[] sniffClips;
        [SerializeField] private AudioClip[] barkClips;
        [SerializeField] private AudioClip[] sneezeClips;
        [SerializeField] private AudioClip yawnClip;
        [SerializeField] private AudioClip shakeClip;
        [SerializeField] private float footstepVolume = 0.35f;
        [SerializeField] private float sfxVolume = 0.7f;
        [Tooltip("Debounce so the 4-beat gait doesn't machine-gun footstep sounds.")]
        [SerializeField] private float footstepMinInterval = 0.13f;
        [Tooltip("Rare NPC yip probability per behaviour-pick. Hero dog = 0 (stays quiet unless provoked).")]
        [SerializeField] private float barkChance = 0f;
        [SerializeField] private float barkMinInterval = 30f;

        // TELEMETRY GATE. The orchestrator flips this to false before a release build so the
        // acceptance logs don't spam devtools. Static so one switch silences every dog.
        [Header("Telemetry")]
        public static bool ProbeLogs = false;   // release: sprints A/B/C accepted, telemetry off

        // ── refs ──────────────────────────────────────────────────────────────────────────────
        private CharacterController _cc;
        private CorgiStateAnimator _anim;
        private Transform _mesh, _root;
        private AudioSource _audio;
        private Quaternion _meshBaseRot;
        private Vector3 _meshBasePos;

        // ── bones ─────────────────────────────────────────────────────────────────────────────
        private Transform _head, _tail, _earA, _earB;
        private Quaternion _headBase, _tailBase, _earABase, _earBBase;

        private struct Leg { public Transform hip, knee; public Quaternion hipBase, kneeBase; }
        private Leg _flL, _flR, _blL, _blR;   // front-left, front-right, back-left, back-right
        private bool _bonesOk;

        // ── runtime state ─────────────────────────────────────────────────────────────────────
        private DogState _state = DogState.Idle;
        private bool _exiting, _exitByInput;
        private float _idleTimer, _idleTarget, _holdTimer, _poseClock, _poseDuration, _weight;
        private float _lastFootstep, _lastBark, _sniffSndTimer, _probeTimer;
        private bool _sneezePlayed;
        // On-the-move sneeze — a HEAD-ONLY overlay that lives OUTSIDE the pose state machine (it
        // must not yield the skeleton / stop the gait). Gated so it fires only rarely while walking.
        private bool _moveSneezing, _moveSneezePlayed;
        private float _moveSneezeClock, _moveSneezeDur, _moveSneezeTimer;
        private static readonly System.Random _rng = new System.Random();

        private void Start()
        {
            _root = transform;
            _cc = GetComponent<CharacterController>();
            _anim = GetComponentInChildren<CorgiStateAnimator>();
            if (_anim == null)
            {
                Debug.LogWarning("[DogBehavior] No CorgiStateAnimator under this root — disabling (nothing to drive).");
                enabled = false;
                return;
            }
            _mesh = _anim.transform;
            _meshBaseRot = _mesh.localRotation;
            _meshBasePos = _mesh.localPosition;

            // Same skeleton the animator finds (see CorgiStateAnimator.Start). Start() writes no
            // bones anywhere, so our bases == the animator's bases (identical rest pose).
            _head = Find("Head_1") ?? Find("Head_2") ?? Find("head") ?? Find("neck");
            _tail = Find("Tail_1") ?? Find("Tail_0") ?? Find("tail");
            _earA = Find("bone_7");
            _earB = Find("bone_8");
            if (_head) _headBase = _head.localRotation;
            if (_tail) _tailBase = _tail.localRotation;
            if (_earA) _earABase = _earA.localRotation;
            if (_earB) _earBBase = _earB.localRotation;

            _flL = MakeLeg("0_Left_Limb_0", "0_Left_Limb_1");
            _flR = MakeLeg("0_Right_Limb_0", "0_Right_Limb_1");
            _blL = MakeLeg("1_Left_Limb_0", "1_Left_Limb_1");
            _blR = MakeLeg("1_Right_Limb_0", "1_Right_Limb_1");
            _bonesOk = _head || _tail || _flL.hip || _blL.hip;

            // Audio sink on the dog. Missing clips are TOLERATED everywhere (null-checked) → silent,
            // no errors, no spam, until the sound designer drops files in and they get wired.
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.loop = false;
            _audio.spatialBlend = audioSpatialBlend;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            _audio.minDistance = 2f;
            _audio.maxDistance = 18f;
            _anim.OnFootstep += PlayFootstep;   // gait-synced steps

            _idleTarget = Rand(idleTriggerDelay);
            _moveSneezeTimer = Rand(moveSneezeInterval);   // first on-the-move sneeze no sooner than this
            Log("Idle");
        }

        private void OnDestroy()
        {
            if (_anim != null) _anim.OnFootstep -= PlayFootstep;
        }

        private Leg MakeLeg(string hipName, string kneeName)
        {
            var l = new Leg { hip = Find(hipName), knee = Find(kneeName) };
            if (l.hip) l.hipBase = l.hip.localRotation;
            if (l.knee) l.kneeBase = l.knee.localRotation;
            return l;
        }

        private Transform Find(string nameContains)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name.Contains(nameContains)) return t;
            return null;
        }

        // ── BRAIN (Update): input, timers, weight, ownership flag ────────────────────────────────
        private void Update()
        {
            float dt = Time.deltaTime;

            bool input = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > inputDeadzone
                      || Mathf.Abs(Input.GetAxisRaw("Vertical")) > inputDeadzone;
            bool moving = _cc != null && _cc.velocity.sqrMagnitude > moveVelThreshold * moveVelThreshold;
            bool interrupted = input || moving;

            if (_state == DogState.Idle)
            {
                if (interrupted) _idleTimer = 0f;
                else
                {
                    _idleTimer += dt;
                    if (_idleTimer >= _idleTarget) BeginPose(PickPose());
                }
            }
            else
            {
                _poseClock += dt;
                if (!_exiting)
                {
                    // ANY input/movement → immediately start blending out. The body itself keeps
                    // moving (KafkaDirectController owns it) — we only bring the visual pose back.
                    if (interrupted) BeginExit(true);
                    else
                    {
                        _holdTimer -= dt;
                        if (_holdTimer <= 0f) BeginExit(false);
                    }
                }
            }

            // integrate blend weight toward the target (sneeze snaps — its motion self-returns to base)
            float target = (_state != DogState.Idle && !_exiting) ? 1f : 0f;
            float enterB = _state == DogState.Sneeze ? sneezeBlend : enterBlend;
            float exitB = _state == DogState.Sneeze ? sneezeBlend : exitBlend;
            float blend = target > _weight ? enterB : exitB;
            _weight = Mathf.MoveTowards(_weight, target, dt / Mathf.Max(0.01f, blend));
            if (_exiting && _weight <= 0.001f) EndPose();

            // sneeze SOUND fires at the nod (~⅓ in), not at pose entry
            if (_state == DogState.Sneeze && !_sneezePlayed && _poseDuration > 0f
                && _poseClock / _poseDuration >= 0.33f)
            {
                PlayClips(sneezeClips, sfxVolume);
                _sneezePlayed = true;
            }

            // periodic snuffle while sniffing
            if (_state == DogState.Sniff && !_exiting && _weight > 0.5f)
            {
                _sniffSndTimer -= dt;
                if (_sniffSndTimer <= 0f) { PlayClips(sniffClips, sfxVolume); _sniffSndTimer = RandF(1.0f, 1.6f); }
            }

            // ON-THE-MOVE SNEEZE (head-only, rare). Fires while walking, never touches the state
            // machine, so ExternalPoseActive stays down and the gait keeps running — only the head
            // sneezes (applied as an overlay in LateUpdate). Gated by moveSneezeInterval.
            _moveSneezeTimer -= dt;
            if (!_moveSneezing && _state == DogState.Idle && moving && _moveSneezeTimer <= 0f)
            {
                _moveSneezing = true;
                _moveSneezeClock = 0f;
                _moveSneezeDur = Rand(sneezeDuration);
                _moveSneezePlayed = false;
                _moveSneezeTimer = Rand(moveSneezeInterval);
                Log("Sneeze");
            }
            if (_moveSneezing)
            {
                _moveSneezeClock += dt;
                if (!_moveSneezePlayed && _moveSneezeDur > 0f && _moveSneezeClock / _moveSneezeDur >= 0.33f)
                {
                    PlayClips(sneezeClips, sfxVolume);
                    _moveSneezePlayed = true;
                }
                if (_moveSneezeClock >= _moveSneezeDur) _moveSneezing = false;
            }

            // OWNERSHIP: raise the yield flag whenever we have any bone influence this frame, so
            // CorgiStateAnimator steps aside (single writer). Set here in Update() → visible to the
            // animator's LateUpdate no matter the component order.
            if (_anim != null) _anim.ExternalPoseActive = _state != DogState.Idle || _weight > 0.001f;

            ProbeTick(dt);
        }

        // ── POSE WRITER (LateUpdate): sole writer while ExternalPoseActive ────────────────────────
        private void LateUpdate()
        {
            if (!_bonesOk) return;

            // Stationary poses — DogBehavior owns the whole (yielded) skeleton.
            if (!(_state == DogState.Idle && _weight <= 0.001f))
            {
                float w = _weight * _weight * (3f - 2f * _weight);   // smoothstep for a softer blend
                switch (_state)
                {
                    case DogState.Sit:     ApplySit(w); break;
                    case DogState.Scratch: ApplyScratch(w); break;
                    case DogState.Sniff:   ApplySniff(w); break;
                    case DogState.LieDown: ApplyLieDown(w); break;
                    case DogState.Shake:   ApplyShake(w); break;
                    case DogState.Sneeze:  ApplySneezeStationary(w); break;
                }
            }

            // On-the-move sneeze — HEAD-ONLY additive overlay ON TOP of the walk pose the animator
            // already wrote this frame (we run after it via DefaultExecutionOrder). Never yields the
            // skeleton, so the gait keeps playing and the body keeps moving — only head/ears sneeze.
            if (_moveSneezing && _state == DogState.Idle)
                ApplySneezeMovingOverlay();
        }

        // ── pose implementations ─────────────────────────────────────────────────────────────────
        private void ApplySit(float w)
        {
            BlendMesh(w, -sitBodyPitchDeg, 0f, sitBodyDrop);       // nose-up tilt + rear settles
            BlendLeg(_flL, w, 0f, 0f);                             // fronts planted
            BlendLeg(_flR, w, 0f, 0f);
            BlendLeg(_blL, w, sitHindHipDeg, sitHindKneeDeg);      // hinds fold under
            BlendLeg(_blR, w, sitHindHipDeg, sitHindKneeDeg);
            BlendBone(_head, _headBase, w, _root.right, -sitHeadUpDeg, Vector3.up, 0f);
            BlendBone(_tail, _tailBase, w, Vector3.up, 0f, Vector3.up, 0f);
            BlendBone(_earA, _earABase, w, _root.right, 0f, Vector3.up, 0f);
            BlendBone(_earB, _earBBase, w, _root.right, 0f, Vector3.up, 0f);
        }

        private void ApplyScratch(float w)
        {
            float ph = _poseClock * scratchDrumHz * Mathf.PI * 2f;
            float drum = Mathf.Sin(ph);
            BlendMesh(w, -sitBodyPitchDeg * 0.8f, 0f, sitBodyDrop * 0.9f);
            BlendLeg(_flL, w, 0f, 0f);
            BlendLeg(_flR, w, 0f, 0f);
            BlendLeg(_blL, w, sitHindHipDeg, sitHindKneeDeg);      // left hind still folded (supporting)
            // right hind lifts toward the ear and drums fast
            BlendBone(_blR.hip, _blR.hipBase, w, _root.right, -scratchLegLiftDeg, Vector3.up, 0f);
            BlendBone(_blR.knee, _blR.kneeBase, w, _root.right, scratchKneeFoldDeg + drum * scratchDrumDeg, Vector3.up, 0f);
            // head tilts to the scratched side and shivers a little
            BlendBone(_head, _headBase, w, Vector3.up, scratchHeadTiltDeg + drum * 4f, _root.forward, scratchHeadTiltDeg * 0.4f);
            BlendBone(_tail, _tailBase, w, Vector3.up, 0f, Vector3.up, 0f);
            BlendBone(_earA, _earABase, w, _root.right, drum * scratchDrumDeg * 0.5f, Vector3.up, 0f);
            BlendBone(_earB, _earBBase, w, _root.right, -drum * scratchDrumDeg * 0.5f, Vector3.up, 0f);
        }

        private void ApplySniff(float w)
        {
            float yaw = Mathf.Sin(_poseClock * sniffYawHz * Mathf.PI * 2f) * sniffHeadYawDeg;
            BlendMesh(w, sniffBodyPitchDeg, 0f, sniffBodyDrop);    // slight front dip
            BlendLeg(_flL, w, 0f, 0f);
            BlendLeg(_flR, w, 0f, 0f);
            BlendLeg(_blL, w, 0f, 0f);
            BlendLeg(_blR, w, 0f, 0f);
            BlendBone(_head, _headBase, w, _root.right, sniffHeadDownDeg, Vector3.up, yaw);
            BlendBone(_tail, _tailBase, w, Vector3.up, 0f, Vector3.up, 0f);
            BlendBone(_earA, _earABase, w, _root.right, 0f, Vector3.up, 0f);
            BlendBone(_earB, _earBBase, w, _root.right, 0f, Vector3.up, 0f);
        }

        private void ApplyLieDown(float w)
        {
            BlendMesh(w, lieBodyPitchDeg, 0f, lieBodyDrop);
            BlendLeg(_flL, w, -lieFrontExtendDeg, 0f);            // front paws reach forward
            BlendLeg(_flR, w, -lieFrontExtendDeg, 0f);
            BlendLeg(_blL, w, lieHindFoldDeg, lieHindFoldDeg);    // hinds tucked
            BlendLeg(_blR, w, lieHindFoldDeg, lieHindFoldDeg);
            BlendBone(_head, _headBase, w, _root.right, lieHeadDownDeg, Vector3.up, 0f);
            BlendBone(_tail, _tailBase, w, Vector3.up, 0f, Vector3.up, 0f);
            BlendBone(_earA, _earABase, w, _root.right, 0f, Vector3.up, 0f);
            BlendBone(_earB, _earBBase, w, _root.right, 0f, Vector3.up, 0f);
        }

        private void ApplyShake(float w)
        {
            float ph = _poseClock * shakeHz * Mathf.PI * 2f;
            float roll = Mathf.Sin(ph) * shakeRollDeg;
            float hy = Mathf.Sin(ph * 1.1f) * shakeHeadYawDeg;
            float ef = Mathf.Sin(ph) * shakeEarDeg;
            BlendMesh(w, 0f, roll, 0f);                           // corpus rolls fast side-to-side
            BlendLeg(_flL, w, 0f, 0f);                            // paws stay planted
            BlendLeg(_flR, w, 0f, 0f);
            BlendLeg(_blL, w, 0f, 0f);
            BlendLeg(_blR, w, 0f, 0f);
            BlendBone(_head, _headBase, w, Vector3.up, hy, Vector3.up, 0f);
            BlendBone(_tail, _tailBase, w, Vector3.up, Mathf.Sin(ph * 0.9f) * shakeEarDeg, Vector3.up, 0f);
            BlendBone(_earA, _earABase, w, _root.right, ef, Vector3.up, 0f);
            BlendBone(_earB, _earBBase, w, _root.right, -ef, Vector3.up, 0f);
        }

        private void ApplySneezeStationary(float w)
        {
            float u = _poseDuration > 0f ? Mathf.Clamp01(_poseClock / _poseDuration) : 1f;
            float pitch = SneezePitch(u), yaw = SneezeYaw(u), ear = SneezeEar(u);
            BlendMesh(w, 0f, 0f, 0f);                              // body neutral — it's all in the head
            BlendLeg(_flL, w, 0f, 0f);
            BlendLeg(_flR, w, 0f, 0f);
            BlendLeg(_blL, w, 0f, 0f);
            BlendLeg(_blR, w, 0f, 0f);
            BlendBone(_head, _headBase, w, _root.right, pitch, Vector3.up, yaw);
            BlendBone(_tail, _tailBase, w, Vector3.up, 0f, Vector3.up, 0f);
            BlendBone(_earA, _earABase, w, _root.right, ear, Vector3.up, 0f);
            BlendBone(_earB, _earBBase, w, _root.right, -ear, Vector3.up, 0f);
        }

        // Head-only sneeze ADDED on top of whatever the animator wrote this frame (the walk head).
        // The envelope starts & ends at 0, so there is no pop against the running gait.
        private void ApplySneezeMovingOverlay()
        {
            float u = _moveSneezeDur > 0f ? Mathf.Clamp01(_moveSneezeClock / _moveSneezeDur) : 1f;
            float pitch = SneezePitch(u), yaw = SneezeYaw(u), ear = SneezeEar(u);
            if (_head)
            {
                _head.Rotate(_root.right, pitch, Space.World);
                _head.Rotate(Vector3.up, yaw, Space.World);
            }
            if (_earA) _earA.Rotate(_root.right, ear, Space.World);
            if (_earB) _earB.Rotate(_root.right, -ear, Space.World);
        }

        // Sneeze head envelope over normalised time u∈[0,1]: windup (nose up-back) → sharp nod
        // down → damped head/ear shake, returning to the rest pose. Starts & ends at ~0 so it
        // blends cleanly whether the skeleton is yielded (stationary) or overlaid (on the move).
        private float SneezePitch(float u)
        {
            if (u < 0.33f) { float t = u / 0.33f; return Mathf.Lerp(0f, -sneezeWindupDeg, t * t * (3f - 2f * t)); } // ease nose up-back (− = up)
            if (u < 0.50f) { float t = (u - 0.33f) / 0.17f; return Mathf.Lerp(-sneezeWindupDeg, sneezeNodDeg, t * t); } // snap down (+ = nose down)
            float s = (u - 0.5f) / 0.5f;
            float shake = Mathf.Sin(s * Mathf.PI * 2f * sneezeShakeCycles) * Mathf.Exp(-s * 4f) * sneezeShakeDeg;
            return Mathf.Lerp(sneezeNodDeg, 0f, s) + shake;
        }
        private float SneezeYaw(float u)
        {
            if (u < 0.5f) return 0f;
            float s = (u - 0.5f) / 0.5f;
            return Mathf.Sin(s * Mathf.PI * 2f * sneezeShakeCycles) * Mathf.Exp(-s * 4f) * sneezeShakeDeg * 0.6f;
        }
        private float SneezeEar(float u)
        {
            if (u < 0.4f) return 0f;
            float s = (u - 0.4f) / 0.6f;
            return Mathf.Sin(s * Mathf.PI * 2f * sneezeShakeCycles) * Mathf.Exp(-s * 3.5f) * sneezeEarDeg;
        }

        // ── blend helpers (same convention as CorgiStateAnimator: base + world-axis Rotate) ──────
        private void BlendLeg(Leg leg, float w, float hipDeg, float kneeDeg)
        {
            BlendBone(leg.hip, leg.hipBase, w, _root.right, hipDeg, Vector3.up, 0f);
            BlendBone(leg.knee, leg.kneeBase, w, _root.right, kneeDeg, Vector3.up, 0f);
        }

        private static void BlendBone(Transform b, Quaternion baseRot, float w, Vector3 ax1, float a1, Vector3 ax2, float a2)
        {
            if (!b) return;
            b.localRotation = baseRot;
            if (a1 != 0f) b.Rotate(ax1, a1, Space.World);
            if (a2 != 0f) b.Rotate(ax2, a2, Space.World);
            b.localRotation = Quaternion.Slerp(baseRot, b.localRotation, w);
        }

        private void BlendMesh(float w, float pitchDeg, float rollDeg, float drop)
        {
            _mesh.localRotation = _meshBaseRot;
            if (pitchDeg != 0f) _mesh.Rotate(_root.right, pitchDeg, Space.World);
            if (rollDeg != 0f) _mesh.Rotate(_root.forward, rollDeg, Space.World);
            _mesh.localRotation = Quaternion.Slerp(_meshBaseRot, _mesh.localRotation, w);
            _mesh.localPosition = Vector3.Lerp(_meshBasePos, _meshBasePos + Vector3.down * drop, w);
        }

        // ── state transitions ────────────────────────────────────────────────────────────────────
        private void BeginPose(DogState s)
        {
            _state = s;
            _exiting = false;
            _poseClock = 0f;
            _holdTimer = HoldFor(s);
            _poseDuration = _holdTimer;
            _idleTimer = 0f;
            _sneezePlayed = false;
            _moveSneezing = false;   // a full pose cancels any head-only sneeze in flight
            OnEnterPose(s);
            Log(s.ToString());
        }

        private void BeginExit(bool byInput)
        {
            if (_exiting) return;
            _exiting = true;
            _exitByInput = byInput;
            Log(_state + "→exit");
        }

        private void EndPose()
        {
            var prev = _state;
            // restore mesh explicitly, then hand the skeleton back to CorgiStateAnimator (which
            // resumes on the next frame because ExternalPoseActive drops to false below in Update).
            if (_mesh) { _mesh.localRotation = _meshBaseRot; _mesh.localPosition = _meshBasePos; }
            _state = DogState.Idle;
            _exiting = false;
            _weight = 0f;
            _idleTimer = 0f;
            _idleTarget = Rand(idleTriggerDelay);

            // «принюхалась → чихнула» — the cute must-have beat. Only chain when the sniff ended
            // NATURALLY (not because the player took control), so we never start a stationary
            // sneeze while the dog is already moving.
            if (prev == DogState.Sniff && !_exitByInput
                && (float)_rng.NextDouble() < postSniffSneezeChance)
            {
                BeginPose(DogState.Sneeze);
                return;
            }
            Log("Idle");
        }

        private DogState PickPose()
        {
            float total = wSit + wSniff + wScratch + wShake + wLie + wSneeze;
            if (total <= 0f) return DogState.Sit;
            float r = (float)_rng.NextDouble() * total;
            if ((r -= wSit) < 0f) return DogState.Sit;
            if ((r -= wSniff) < 0f) return DogState.Sniff;
            if ((r -= wScratch) < 0f) return DogState.Scratch;
            if ((r -= wShake) < 0f) return DogState.Shake;
            if ((r -= wSneeze) < 0f) return DogState.Sneeze;
            return DogState.LieDown;
        }

        private float HoldFor(DogState s)
        {
            switch (s)
            {
                case DogState.Sit: return Rand(sitHold);
                case DogState.Sniff: return Rand(sniffHold);
                case DogState.Scratch: return Rand(scratchHold);
                case DogState.LieDown: return Rand(lieHold);
                case DogState.Shake: return shakeHold;
                case DogState.Sneeze: return Rand(sneezeDuration);
                default: return 0f;
            }
        }

        // ── audio ────────────────────────────────────────────────────────────────────────────────
        private void OnEnterPose(DogState s)
        {
            switch (s)
            {
                case DogState.Sniff: PlayClips(sniffClips, sfxVolume); _sniffSndTimer = RandF(1.0f, 1.6f); break;
                case DogState.LieDown: PlayClip(yawnClip, sfxVolume); break;
                case DogState.Shake: PlayClip(shakeClip, sfxVolume); break;
            }
            MaybeBark();
        }

        private void MaybeBark()
        {
            if (barkChance <= 0f || barkClips == null || barkClips.Length == 0) return;
            if (Time.time - _lastBark < barkMinInterval) return;
            if (_rng.NextDouble() >= barkChance) return;
            PlayClips(barkClips, sfxVolume);
            _lastBark = Time.time;
        }

        private void PlayFootstep()
        {
            if (footstepClips == null || footstepClips.Length == 0 || _audio == null) return;
            float t = Time.time;
            if (t - _lastFootstep < footstepMinInterval) return;
            _lastFootstep = t;
            var c = footstepClips[_rng.Next(footstepClips.Length)];
            if (!c) return;
            _audio.pitch = RandF(0.94f, 1.07f);   // tiny variation so steps don't sound identical
            _audio.PlayOneShot(c, footstepVolume);
        }

        private void PlayClips(AudioClip[] clips, float vol)
        {
            if (clips == null || clips.Length == 0 || _audio == null) return;
            var c = clips[_rng.Next(clips.Length)];
            PlayClip(c, vol);
        }

        private void PlayClip(AudioClip c, float vol)
        {
            if (!c || _audio == null) return;
            _audio.pitch = 1f;
            _audio.PlayOneShot(c, vol);
        }

        // ── misc ─────────────────────────────────────────────────────────────────────────────────
        private float Rand(Vector2 r) => Mathf.Lerp(r.x, r.y, (float)_rng.NextDouble());
        private float RandF(float a, float b) => Mathf.Lerp(a, b, (float)_rng.NextDouble());

        private void ProbeTick(float dt)
        {
            if (!ProbeLogs) return;
            _probeTimer += dt;
            if (_probeTimer >= 5f)
            {
                _probeTimer = 0f;
                Debug.Log($"[DOGSTATE] {_state} t={Time.time:F1} w={_weight:F2}");
            }
        }

        private void Log(string state)
        {
            if (ProbeLogs) Debug.Log($"[DOGSTATE] {state} t={Time.time:F1}");
        }

#if UNITY_EDITOR
        /// <summary>
        /// EDITOR-ONLY convenience: auto-load the SFX by name from Assets/_Project/Audio/SFX/Dog/
        /// into the serialized clip fields (only fills what's still empty). BotanikaBuilder calls
        /// this right after AddComponent so the sound designer's files get wired automatically as
        /// they land. Runtime never touches AssetDatabase (WebGL-safe): clips are plain serialized
        /// references, and missing ones just stay null → silent.
        /// </summary>
        public void EditorAutoWireAudio()
        {
            const string dir = "Assets/_Project/Audio/SFX/Dog";
            if (footstepClips == null || footstepClips.Length == 0) footstepClips = LoadByPrefix(dir, "dog_step");
            if (sniffClips == null || sniffClips.Length == 0) sniffClips = LoadByPrefix(dir, "dog_sniff");
            if (barkClips == null || barkClips.Length == 0) barkClips = LoadByPrefix(dir, "dog_bark");
            if (sneezeClips == null || sneezeClips.Length == 0) sneezeClips = LoadByPrefix(dir, "dog_sneeze");
            if (yawnClip == null) yawnClip = LoadOne(dir, "dog_yawn");
            if (shakeClip == null) shakeClip = LoadOne(dir, "dog_shake");
        }

        private static AudioClip[] LoadByPrefix(string dir, string prefix)
        {
            var found = new System.Collections.Generic.List<AudioClip>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (file.StartsWith(prefix))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null) found.Add(clip);
                }
            }
            found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return found.ToArray();
        }

        private static AudioClip LoadOne(string dir, string prefix)
        {
            var arr = LoadByPrefix(dir, prefix);
            return arr.Length > 0 ? arr[0] : null;
        }
#endif
    }
}

/* ─────────────────────────────────────────────────────────────────────────────────────────────
   INTEGRATION (for the orchestrator — BotanikaBuilder.cs; DogBehavior itself changes nothing else)

   DogBehavior goes on the ROOT (Hero_Corgi), next to KafkaDirectController + NpcInteractor. It
   finds CorgiStateAnimator + the Tripo skeleton on the child mesh by itself. Three build paths
   create/patch the dog — add it to all three (each guard makes it idempotent):

   1) Main build — BotanikaBuilder.cs, right after CorgiStateAnimator is added (~line 1662-1663):

        if (corgiMesh.GetComponent<CorgiStateAnimator>() == null)
            corgiMesh.AddComponent<CorgiStateAnimator>();
        // NEW: living-dog behaviour (sit/scratch/sniff/lie/shake + sounds) on the ROOT.
        if (corgiRoot.GetComponent<Afterhumans.Kafka.DogBehavior>() == null)
        {
            var dogBeh = corgiRoot.AddComponent<Afterhumans.Kafka.DogBehavior>();
            dogBeh.EditorAutoWireAudio();   // wire any SFX already present in Audio/SFX/Dog/
        }

   2) EnsurePlayableDog — BotanikaBuilder.cs, right after CorgiStateAnimator is added (~line 4003):

        if (mesh.GetComponent<CorgiStateAnimator>() == null) mesh.AddComponent<CorgiStateAnimator>();
        // NEW:
        if (root.GetComponent<Afterhumans.Kafka.DogBehavior>() == null)
        {
            var dogBeh = root.AddComponent<Afterhumans.Kafka.DogBehavior>();
            dogBeh.EditorAutoWireAudio();
        }

   3) WireBotanikaNpcs — BotanikaBuilder.cs, next to the NpcInteractor add (~line 5018):

        if (dog.GetComponent<Afterhumans.Audio.NpcInteractor>() == null)
            dog.AddComponent<Afterhumans.Audio.NpcInteractor>();
        // NEW:
        if (dog.GetComponent<Afterhumans.Kafka.DogBehavior>() == null)
        {
            var dogBeh = dog.AddComponent<Afterhumans.Kafka.DogBehavior>();
            dogBeh.EditorAutoWireAudio();
        }

   Expected SFX in Assets/_Project/Audio/SFX/Dog/ (auto-wired by prefix in EditorAutoWireAudio):
     dog_step_01..04, dog_sniff_01..02, dog_bark_01..02, dog_yawn_01, dog_shake_01, dog_sneeze_01..02.
   Any that are missing simply stay null → silent, no errors.

   For an NPC dog that should occasionally yip, set its DogBehavior barkChance in the inspector
   (or via code) to e.g. 0.25f; the hero dog stays at 0.

   RELEASE BUILD: set DogBehavior.ProbeLogs = false (and CorgiStateAnimator has no probe logs of
   its own) before building so [DOGSTATE] lines don't reach the browser console.
   ───────────────────────────────────────────────────────────────────────────────────────────── */
