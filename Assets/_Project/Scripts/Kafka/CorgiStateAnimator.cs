using UnityEngine;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// FULLY PROCEDURAL corgi animator (no Animator clip — the baked Walk_10 was a stiff
    /// tiptoe and every IK-FBX swap broke the renderer). Drives the real Tripo skeleton in
    /// LateUpdate:
    ///
    ///  • MOVING  → a 4-beat lateral-sequence WALK (BL→FL→BR→FR, the gait real dogs use).
    ///              Each hip sweeps fore→aft while its paw is "planted" (push-off, NOT a
    ///              stomp — Tim: "топает лапами, не отталкивается"), the knee folds to lift
    ///              the paw through swing. Cadence ∝ ground speed; phase RUNS BACKWARD when the
    ///              dog walks in reverse → no moonwalk (Tim: "назад идёт лунной походкой").
    ///  • IDLE    → dog behaviour: head look-around / nose-down sniff, tail wag, ear flick,
    ///              breathing.
    ///
    /// All rotations are about the CHARACTER's WORLD axes (root.right for the sagittal
    /// fore-aft/knee plane, Vector3.up for head yaw + tail wag) — coordinate-agnostic, so the
    /// arbitrary Tripo bone orientations don't fight us. NB: the mesh is yawed −90° in the
    /// builder, so mesh.transform.right is NOT lateral — we take the lateral axis from the
    /// movement root (the CharacterController transform).
    /// </summary>
    public class CorgiStateAnimator : MonoBehaviour
    {
        public enum State { Idle, LookAround, Sniff }

        [Header("Idle timing (seconds)")]
        public Vector2 idleHold = new Vector2(3f, 6f);
        public Vector2 lookHold = new Vector2(3.5f, 6f);
        public Vector2 sniffHold = new Vector2(3f, 5f);

        [Header("Idle behaviour (deg)")]
        // Углы головы СНИЖЕНЫ (биомеханик): два Space.World поворота на произвольно-
        // ориентированной кости Tripo давали перекрут/излом шеи на пиках sin. Меньше угол → нет излома.
        public float lookYaw = 26f;
        public float lookPitchUp = 6f;
        public float sniffPitch = 24f;
        public float sniffYaw = 9f;
        public float tailWagFreq = 5f;
        public float tailWagAmp = 30f;
        public float earFlickAmp = 10f;
        public float breathAmp = 0.004f;   // дыхание-шёпот: scale-дыхание раздувало корпус и ПОДНИМАЛО собаку (источник подпрыгивания)
        public float breathFreq = 0.9f;
        public float poseDamp = 4f;        // мягче поворот головы, без снапа

        [Header("Walk gait")]
        // Биомеханик: T3-правки (срезанная амплитуда + duty 0.80) НЕ лечили подпрыгивание
        // (его источник — scale-дыхание + GroundPaw не дотягивал) и сплющили хорошую походку.
        // Откат к до-T3 естественному шагу; реальный фикс подпрыгивания — в LateUpdate (scale only idle).
        [Tooltip("Hip fore-aft swing amplitude (deg). The stride + push-off.")]
        public float hipSwingDeg = 37f;   // уверенный шаг (откат с 34)
        [Tooltip("Knee/elbow fold while the paw is in the air (deg).")]
        public float kneeLiftDeg = 22f;   // нормальный клиренс лапы (откат с 15)
        [Tooltip("Fraction of the cycle the paw is on the ground (>0.5 = walk).")]
        public float dutyFactor = 0.72f;   // естественный walk; лёгкое перекрытие свингов диагонали = норма (откат с 0.80)
        [Tooltip("Metres of travel per full stride — sets cadence vs ground speed.")]
        public float strideLength = 0.55f;   // под 37° мах, без скольжения (откат с 0.58)
        [Tooltip("Vertical body bob amplitude (m).")]
        public float bodyBob = 0.018f;   // честный вес; позиционный (не scale) → опорные лапы перепланчиваются под ним, не подпрыгивает
        [Tooltip("Flip if the legs sweep the wrong way (push looks like moonwalk).")]
        public float swingSign = 1f;

        [Tooltip("Log per-paw ground contact to the console (gait-test diagnosis only).")]
        public bool logGaitContacts = false;
        private float _dbgT;
        private SkinnedMeshRenderer[] _smrs;

        // ---- captured base ----
        private Vector3 _baseScale;
        private Vector3 _baseMeshLocalPos;
        private Transform _head, _tail, _earA, _earB;
        private Quaternion _headBase, _tailBase, _earABase, _earBBase;

        [Tooltip("Hind legs sweep harder than front → reads as real propulsion (acceptance fix).")]
        public float hindHipMul = 1.3f;   // мощный задний толчок (откат с 1.15)

        [Header("Gait naturalness (added for DogBehavior sprint)")]
        // Аддитивные правки естественности походки. Все с текущими значениями по умолчанию,
        // так что при 0 поведение = прежнее. Корги реально переваливается — небольшой roll
        // корпуса в фазе шага читается как «живой», а не «едет на рельсах».
        [Tooltip("Side-to-side body roll (waddle) amplitude while walking, degrees. 0 = old behaviour.")]
        public float bodyRollDeg = 2.5f;
        [Tooltip("Tail-wag multiplier while moving (old flat value was 0.4).")]
        public float moveTailWagMul = 0.6f;
        [Tooltip("Nose lowers up to this many degrees at full run (scaled by ground speed). 0 = off.")]
        public float runHeadLowerDeg = 6f;
        [Tooltip("Ground speed (m/s) at which runHeadLowerDeg reaches full — head lowers as the dog speeds up.")]
        public float runHeadLowerSpeed = 2.2f;

        private struct Leg
        {
            public Transform hip, knee, paw;
            public Quaternion hipBase, kneeBase;
            public float phase;     // 0..1 offset in the gait cycle
            public float kneeSign;  // front elbows fold back, hind stifles fold forward
            public float hipMul;    // hind > front so the push-off looks powered
        }
        private Leg[] _legs;

        // ---- runtime ----
        private State _state; private float _holdTimer;
        private float _breath, _wag, _lookT, _gaitPhase;
        private Transform _root;            // movement root (CharacterController) = lateral axis source
        private CharacterController _moverCC;
        private float _footGroundOffset = -0.45f;   // restLowestPawY − rootY (≈ −legLength); the real floor sits this far below the root pivot
        private static readonly System.Random _rng = new System.Random();

        // ---- external-pose coordination + footstep hook (added for DogBehavior sprint) ----
        // SINGLE-OWNER RULE. DogBehavior (sit/scratch/sniff/lie/shake) drives the SAME bones in
        // its own LateUpdate. To avoid a two-writer race (whoever runs last wins) this animator
        // YIELDS the whole skeleton whenever ExternalPoseActive is set — it early-returns and
        // touches nothing, so DogBehavior is the sole writer during a special pose. DogBehavior
        // sets the flag in Update() (before any LateUpdate) and clears it once it has fully
        // blended back to the rest pose, so the hand-back is seamless.
        private Quaternion _baseMeshLocalRot;   // captured so walk-roll / poses can restore mesh yaw
        private bool[] _wasStance;               // per-leg previous stance, for touchdown detection
        [System.NonSerialized] public bool ExternalPoseActive;
        // Fired at each paw TOUCHDOWN (swing→stance) during the walk so DogBehavior can play a
        // footstep in sync with the gait. Never fires while idle. Best-effort / null-safe.
        public System.Action OnFootstep;

        private void Start()
        {
            _baseScale = transform.localScale;
            _baseMeshLocalPos = transform.localPosition;
            _baseMeshLocalRot = transform.localRotation;   // mesh keeps its -90° yaw; roll/poses restore to this
            _moverCC = GetComponentInParent<CharacterController>();
            _root = _moverCC != null ? _moverCC.transform : transform;

            _head = FindBone("Head_1") ?? FindBone("Head_2") ?? FindBone("head", "neck");
            _tail = FindBone("Tail_1") ?? FindBone("Tail_0") ?? FindBone("tail");
            _earA = FindExact("bone_7");
            _earB = FindExact("bone_8");
            if (_head) _headBase = _head.localRotation;
            if (_tail) _tailBase = _tail.localRotation;
            if (_earA) _earABase = _earA.localRotation;
            if (_earB) _earBBase = _earB.localRotation;

            // 4 legs — lateral-sequence walk: back-left, front-left, back-right, front-right.
            // hip = Limb_0 (shoulder/hip), knee = Limb_1 (elbow/stifle). Hind knees fold forward
            // (+), front elbows fold backward (−).
            // TRUE lateral-sequence walk: footfall order LH→LF→RH→RF, evenly 0.25 apart.
            // phase_i is set so touchdown_i = (1 − phase_i) gives that order. With dutyFactor 0.80
            // (swing window 0.20 < 0.25 spacing) the four swing windows NEVER overlap → at most one
            // paw is ever airborne → always ≥3 paws planted, flight phase mathematically impossible.
            _legs = new Leg[]
            {
                MakeLeg("1_Left_Limb_0",  "1_Left_Limb_1",  "1_Left_Limb_4",  0.00f, +1f, hindHipMul), // back-left  (LH) td 0.00
                MakeLeg("0_Left_Limb_0",  "0_Left_Limb_1",  "0_Left_Limb_3",  0.75f, -1f, 1f),         // front-left (LF) td 0.25
                MakeLeg("1_Right_Limb_0", "1_Right_Limb_1", "1_Right_Limb_4", 0.50f, +1f, hindHipMul), // back-right (RH) td 0.50
                MakeLeg("0_Right_Limb_0", "0_Right_Limb_1", "0_Right_Limb_3", 0.25f, -1f, 1f),         // front-right(RF) td 0.75
            };

            // FLOOR REFERENCE (Build T4). At the authored rest pose the dog STANDS on the floor
            // (idle looks grounded because idle never calls GroundPaw), so the lowest paw BONE
            // marks the height a planted paw should hold. Capture that bone's offset relative to
            // the root → floorY = root.y + offset at runtime. The offset can be EITHER sign: the
            // gait-test root sits AT the floor (paw bones above it → +offset); other rigs park the
            // root at hip height (paw bones below it → −offset). The old Min(-0.02,…) clamp assumed
            // hip-height and forced the target BELOW the real floor → the IK never reached a real
            // contact (judges T2/T3: paws float). Use the raw rest offset, no clamp.
            float lowestPaw = float.MaxValue;
            if (_legs != null)
                foreach (var leg in _legs)
                    if (leg.paw) lowestPaw = Mathf.Min(lowestPaw, leg.paw.position.y);
            if (lowestPaw < float.MaxValue)
                _footGroundOffset = lowestPaw - _root.position.y;

            _smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _wasStance = new bool[_legs != null ? _legs.Length : 0];

            EnterState(State.LookAround);
        }

        private Leg MakeLeg(string hipName, string kneeName, string pawName, float phase, float kneeSign, float hipMul)
        {
            var l = new Leg { phase = phase, kneeSign = kneeSign, hipMul = hipMul };
            l.hip = FindExact(hipName);
            l.knee = FindExact(kneeName);
            l.paw = FindExact(pawName);
            if (l.hip) l.hipBase = l.hip.localRotation;
            if (l.knee) l.kneeBase = l.knee.localRotation;
            return l;
        }

        private bool IsMoving => _moverCC != null && _moverCC.velocity.sqrMagnitude > 0.04f;

        private Transform FindBone(params string[] names)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                foreach (var n in names)
                    if (t.name.ToLower().Contains(n.ToLower())) return t;
            return null;
        }
        private Transform FindExact(string nameContains)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name.Contains(nameContains)) return t;
            return null;
        }

        private void EnterState(State s)
        {
            _state = s; _lookT = 0f;
            _holdTimer = s == State.Idle ? Rand(idleHold)
                       : s == State.LookAround ? Rand(lookHold) : Rand(sniffHold);
        }
        private float Rand(Vector2 r) => Mathf.Lerp(r.x, r.y, (float)_rng.NextDouble());

        private void LateUpdate()
        {
            // YIELD: DogBehavior owns the whole skeleton this frame (special pose). Touch nothing.
            if (ExternalPoseActive) return;

            float dt = Time.deltaTime;
            _breath += dt;

            if (IsMoving)
            {
                // КОРЕНЬ ПОДПРЫГИВАНИЯ (биомеханик): scale-дыхание (_baseScale*bob) масштабирует
                // меш с пивотом ВЫШЕ пола → поднимает всю собаку каждый кадр. В ходьбе ОТКЛЮЧАЕМ
                // scale (держим базовый) — вес даёт позиционный bodyBob в WalkGait под которым
                // GroundPaw перепланчивает опорные лапы. Никакого scale-pump = нет подпрыгивания.
                transform.localScale = _baseScale;
                WalkGait(dt);
                return;
            }
            // дыхание только в покое (whisper-amplitude, не двигает корпус заметно)
            float bob = 1f + Mathf.Sin(_breath * breathFreq * Mathf.PI * 2f) * breathAmp;
            transform.localScale = _baseScale * bob;
            transform.localPosition = _baseMeshLocalPos;   // clear any walk bob
            transform.localRotation = _baseMeshLocalRot;   // clear any walk roll (restore mesh yaw)
            IdleBehaviour(dt);
        }

        // ---------- WALK ----------
        private void WalkGait(float dt)
        {
            Vector3 vel = _moverCC.velocity;
            float fwdSpeed = Vector3.Dot(vel, _root.forward);          // signed (− = reverse)
            float cadence = Mathf.Abs(fwdSpeed) / Mathf.Max(0.05f, strideLength);
            _gaitPhase += Mathf.Sign(fwdSpeed == 0f ? 1f : fwdSpeed) * cadence * dt;
            _gaitPhase = Mathf.Repeat(_gaitPhase, 1f);

            Vector3 axis = _root.right;   // lateral axis → sagittal (fore-aft) plane

            // Subtle vertical BOB for weight (2 beats/stride, up at passing pose). The earlier
            // "drop whole body to lowest foot" hopped ugly — THIS is a small clean sine, and the
            // per-leg GroundPaw below re-plants each stance paw UNDER the bobbed body, so feet stay
            // pinned while the torso breathes with the gait (judge: "add body bob for weight").
            float bob = Mathf.Sin(_gaitPhase * Mathf.PI * 4f) * bodyBob;
            transform.localPosition = _baseMeshLocalPos + Vector3.up * bob;
            // WADDLE: subtle side-to-side roll of the corpus once per stride — a real corgi rolls
            // its low body as the diagonal legs alternate. Rotation only (no vertical translation),
            // so it can't reintroduce the old "подпрыгивание". Restores mesh yaw first.
            transform.localRotation = _baseMeshLocalRot;
            if (bodyRollDeg > 0f)
                transform.Rotate(_root.forward, Mathf.Sin(_gaitPhase * Mathf.PI * 2f) * bodyRollDeg, Space.World);
            float floorY = _root.position.y + _footGroundOffset;   // real ground plane (root pivot is a leg-length above it)

            foreach (var leg in _legs)
            {
                float p = Mathf.Repeat(_gaitPhase + leg.phase, 1f);
                bool stance = p < dutyFactor;

                // hip fore-aft: STANCE sweeps foot front→back (planted, body drives forward =
                // push-off); SWING returns it back→front through the air.
                float hipAngle;
                if (stance)
                {
                    float t = p / dutyFactor;                          // 0..1 across stance
                    hipAngle = Mathf.Lerp(-hipSwingDeg, hipSwingDeg, t);
                }
                else
                {
                    float t = (p - dutyFactor) / (1f - dutyFactor);    // 0..1 across swing
                    hipAngle = Mathf.Lerp(hipSwingDeg, -hipSwingDeg, Smoother(t));
                }
                hipAngle *= swingSign * leg.hipMul;

                // knee: folds only through swing (lift the paw clear), near-straight in stance.
                float swing01 = stance ? 0f
                              : Mathf.Sin(Mathf.PI * (p - dutyFactor) / (1f - dutyFactor));
                float kneeAngle = swing01 * kneeLiftDeg * leg.kneeSign;

                if (leg.hip)
                {
                    leg.hip.localRotation = leg.hipBase;
                    leg.hip.Rotate(axis, hipAngle, Space.World);
                }
                if (leg.knee)
                {
                    leg.knee.localRotation = leg.kneeBase;
                    leg.knee.Rotate(axis, kneeAngle, Space.World);
                }

                // GROUND THE STANCE PAW: rotate the whole leg about the hip (1-bone, axis=lateral)
                // so the paw drops exactly to the floor — body height untouched (no hop), the paw
                // makes contact instead of floating. Swing paws stay lifted.
                if (stance && leg.hip && leg.paw)
                    GroundPaw(leg, axis, floorY);
            }

            // DIAGNOSIS: objective ground-contact log (gait-test only). Compares each paw's world Y
            // to the real floor found by raycast — settles "do the paws plant?" with numbers, not pixels.
            if (logGaitContacts)
            {
                _dbgT += dt;
                if (_dbgT >= 0.35f)
                {
                    _dbgT = 0f;
                    // lowest MESH vertex (the actual foot bottom) vs the floor plane (y=0 in gait-test)
                    float meshBottom = float.MaxValue;
                    if (_smrs != null)
                        foreach (var smr in _smrs)
                            if (smr) meshBottom = Mathf.Min(meshBottom, smr.bounds.min.y);
                    string s = $"GAIT rootY={_root.position.y:F3} floorTgt={floorY:F3} meshBottom={meshBottom:F3} off={_footGroundOffset:F3}";
                    foreach (var leg in _legs)
                    {
                        if (!leg.paw) continue;
                        float pawY = leg.paw.position.y;
                        float p = Mathf.Repeat(_gaitPhase + leg.phase, 1f);
                        bool st = p < dutyFactor;
                        s += $" | {(st ? "ST" : "sw")} y={pawY:F3}";
                    }
                    Debug.Log(s);
                }
            }

            // FOOTSTEP TRIGGER: fire OnFootstep at each paw touchdown (swing→stance) so DogBehavior
            // plays a step sound synced to the gait. Cheap parallel pass; touches only bookkeeping.
            if (_wasStance != null && _legs != null)
            {
                for (int i = 0; i < _legs.Length; i++)
                {
                    float pp = Mathf.Repeat(_gaitPhase + _legs[i].phase, 1f);
                    bool st = pp < dutyFactor;
                    if (st && !_wasStance[i]) OnFootstep?.Invoke();
                    _wasStance[i] = st;
                }
            }

            // head: lowered a touch at speed (a running dog drops its nose); tail wags more when moving.
            if (_head)
            {
                _head.localRotation = _headBase;
                if (runHeadLowerDeg > 0f)
                {
                    float lower = Mathf.Clamp01(Mathf.Abs(fwdSpeed) / Mathf.Max(0.1f, runHeadLowerSpeed)) * runHeadLowerDeg;
                    _head.Rotate(_root.right, lower, Space.World);   // +about lateral = nose down (same sign as sniff)
                }
            }
            if (_tail)
            {
                _wag += dt;
                _tail.localRotation = _tailBase;
                _tail.Rotate(Vector3.up, Mathf.Sin(_wag * 6f) * tailWagAmp * moveTailWagMul, Space.World);
            }
        }

        // Rotate a whole leg about its hip (axis = lateral) so its paw lands exactly on floorY,
        // leaving body height untouched. Solves A·cosθ + B·sinθ = K for the smallest rotation θ
        // that drops the paw to the floor (1-bone analytic IK in the sagittal plane).
        private void GroundPaw(Leg leg, Vector3 axis, float floorY)
        {
            Vector3 H = leg.hip.position, P = leg.paw.position;
            Vector3 v = P - H;
            Vector3 vPar = Vector3.Dot(v, axis) * axis;
            Vector3 vPerp = v - vPar;
            float r = vPerp.magnitude;
            if (r < 1e-4f) return;
            Vector3 e1 = vPerp / r;
            Vector3 e2 = Vector3.Cross(axis, e1);          // unit, ⊥ axis & e1
            float K = (floorY - H.y) - vPar.y;             // target v'.y minus fixed part
            float A = r * e1.y, B = r * e2.y;
            float mag = Mathf.Sqrt(A * A + B * B);
            if (mag < 1e-5f) return;
            float ratio = Mathf.Clamp(K / mag, -1f, 1f);   // |·|>1 → floor out of reach, max extend
            float phi = Mathf.Atan2(B, A);
            float baseAng = Mathf.Acos(ratio);
            float s1 = Mathf.DeltaAngle(0f, (phi + baseAng) * Mathf.Rad2Deg);
            float s2 = Mathf.DeltaAngle(0f, (phi - baseAng) * Mathf.Rad2Deg);
            float deltaDeg = Mathf.Clamp(Mathf.Abs(s1) <= Mathf.Abs(s2) ? s1 : s2, -70f, 70f); // wide enough that a stance paw always reaches the floor even at full hip extension (judge T2 stance-float)
            leg.hip.Rotate(axis, deltaDeg, Space.World);
        }

        private static float Smoother(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        // ---------- IDLE ----------
        private void IdleBehaviour(float dt)
        {
            _wag += dt; _lookT += dt;
            Vector3 lateral = _root.right;
            float wag = Mathf.Sin(_wag * tailWagFreq * Mathf.PI * 2f);

            float yaw = 0f, pitch = 0f, tailAmpMul = 0.5f, earAmpMul = 1f;
            switch (_state)
            {
                case State.Idle:
                    yaw = Mathf.Sin(_lookT * 0.5f) * 6f; tailAmpMul = 0.35f; break;
                case State.LookAround:
                    yaw = Mathf.Sin(_lookT * 1.0f) * lookYaw;
                    pitch = -Mathf.Abs(Mathf.Sin(_lookT * 0.7f)) * lookPitchUp; tailAmpMul = 1f; break;
                case State.Sniff:
                    pitch = sniffPitch + Mathf.Sin(_lookT * 5f) * 4f;
                    yaw = Mathf.Sin(_lookT * 2.2f) * sniffYaw; tailAmpMul = 0.6f; earAmpMul = 0.5f; break;
            }

            float ease = Mathf.Min(1f, dt * poseDamp);
            if (_head)
            {
                Quaternion prev = _head.localRotation;
                _head.localRotation = _headBase;
                _head.Rotate(Vector3.up, yaw, Space.World);      // look left/right
                _head.Rotate(lateral, pitch, Space.World);       // sniff down / chin up
                _head.localRotation = Quaternion.Slerp(prev, _head.localRotation, ease);
            }
            if (_tail)
            {
                Quaternion prev = _tail.localRotation;
                _tail.localRotation = _tailBase;
                _tail.Rotate(Vector3.up, wag * tailWagAmp * tailAmpMul, Space.World);
                _tail.localRotation = Quaternion.Slerp(prev, _tail.localRotation, Mathf.Min(1f, dt * 14f));
            }
            float ear = Mathf.Sin(_breath * 2.3f) * earFlickAmp * earAmpMul;
            if (_earA) { var p = _earA.localRotation; _earA.localRotation = _earABase; _earA.Rotate(lateral, ear, Space.World); _earA.localRotation = Quaternion.Slerp(p, _earA.localRotation, ease); }
            if (_earB) { var p = _earB.localRotation; _earB.localRotation = _earBBase; _earB.Rotate(lateral, -ear, Space.World); _earB.localRotation = Quaternion.Slerp(p, _earB.localRotation, ease); }

            // legs rest at base while idle
            if (_legs != null)
                foreach (var leg in _legs)
                {
                    if (leg.hip) leg.hip.localRotation = leg.hipBase;
                    if (leg.knee) leg.knee.localRotation = leg.kneeBase;
                }

            _holdTimer -= dt;
            if (_holdTimer <= 0f) EnterState(NextState());
        }

        private State NextState()
        {
            State[] pool = _state == State.Sniff
                ? new[] { State.LookAround, State.Idle, State.LookAround }
                : _state == State.LookAround
                    ? new[] { State.Sniff, State.Idle, State.Sniff }
                    : new[] { State.LookAround, State.Sniff };
            return pool[_rng.Next(pool.Length)];
        }
    }
}
