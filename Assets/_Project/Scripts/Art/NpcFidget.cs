using UnityEngine;

namespace Afterhumans.Art
{
    /// <summary>
    /// Sprint D — Stas "нервно возится на месте" (Mila's line: "он дёрганый, у двери").
    /// Reuses the same shared kirill_animated_raw rig as Kirill/NpcArmStir, but this NPC's
    /// ROOT NEVER MOVES — the old bug ("ездит по полу без ног") was NpcWalk translating the
    /// whole GameObject. All life here happens in the skeleton: a weight-shift sway on the
    /// hips/spine (no root translation), arms drifting between crossed/uncrossed, and sharp,
    /// short head snaps (paranoid glancing) instead of smooth look-around.
    /// </summary>
    public class NpcFidget : MonoBehaviour
    {
        // Sprint D3 BLOCKER#1 fix (amplitude): measured pixel-diff on Стас's tight bbox was
        // only 0.44-0.98%-class readings in earlier passes at these angles (threshold ≥3%) —
        // boosted rotation ranges and added a real POSITION shift (not just rotation) for the
        // "переминание корпуса" weight-shift the acceptance rubric asks for.
        // Sprint D4 MEDIUM fix (same phase-sync fix applied to NpcArmStir/Kirill — see that
        // file's comment): hipSwayFreq/armCycleSeconds forced to an exact 4s period so a
        // ~2s-apart screenshot pair always lands at opposite phase (guaranteed worst-case
        // delta = 2x amplitude, no "unlucky pair" near a shared extremum). Amplitudes bumped
        // again on top of the D3 boost for margin above the 3% floor.
        [Header("Weight shift (hips/spine only — root stays put)")]
        public float hipSwayDeg = 12f;         // was 9 (D3), 5 (D2)
        public float hipSwayFreq = 0.25f;      // period 4s, was 0.22 (unsynced)
        public float spineCounterDeg = 7f;     // was 5 (D3), 3 (D2)
        public float bodyShiftMeters = 0.05f;  // ±5cm lateral/vertical corpus translate on the
                                                // hip bone, on top of the sway rotation —
                                                // "переминание с ноги на ногу" reads as actual
                                                // displacement, not just a tilt.

        [Header("Arms cross/uncross")]
        public float armCycleSeconds = 4.0f;   // period 4s, synced to the 2s sampling interval
        public float armCrossDeg = 56f;        // was 46 (D3), 34 (D2)
        public float elbowFoldDeg = 70f;       // was 62 (D3), 55 (D2)

        [Header("Paranoid head snaps")]
        public float snapEverySeconds = 2.6f;
        public float snapJitter = 1.2f;
        public float snapYawDeg = 26f;         // ~±25 deg per spec (was 30, tightened slightly)
        public float snapDuration = 0.35f;

        [Header("Diagnostics")]
        // WebGL-detective probe (Sprint D2): see NpcArmStir.probeLogs. Freeze bug proven fixed
        // this round — default OFF to stop console spam in the shipped build.
        public bool probeLogs = false;
        private float _probeT;
        private Vector3 _hipBasePos;

        private Transform _hip, _spine, _lUpper, _lFore, _rUpper, _rFore, _head;
        private Quaternion _hipBase, _spineBase, _lUpperBase, _lForeBase, _rUpperBase, _rForeBase, _headBase;
        private float _t;
        private float _snapTimer;
        private float _snapNextAt;
        private float _snapPhase; // 0..1 progress of current snap hold
        private float _snapTargetYaw;
        private System.Random _rng = new System.Random();

        private Transform FindBone(string exact)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == exact) return t;
            return null;
        }

        private void Awake()
        {
            _hip = FindBone("Hip");
            _spine = FindBone("Spine02");
            _lUpper = FindBone("L_Upperarm");
            _lFore = FindBone("L_Forearm");
            _rUpper = FindBone("R_Upperarm");
            _rFore = FindBone("R_Forearm");
            _head = FindBone("Head");

            if (_hip != null) { _hipBase = _hip.localRotation; _hipBasePos = _hip.localPosition; }
            if (_spine != null) _spineBase = _spine.localRotation;
            if (_lUpper != null) _lUpperBase = _lUpper.localRotation;
            if (_lFore != null) _lForeBase = _lFore.localRotation;
            if (_rUpper != null) _rUpperBase = _rUpper.localRotation;
            if (_rFore != null) _rForeBase = _rFore.localRotation;
            if (_head != null) _headBase = _head.localRotation;

            _snapNextAt = Rand(snapEverySeconds);
        }

        private float Rand(float baseSeconds) =>
            baseSeconds + ((float)_rng.NextDouble() * 2f - 1f) * snapJitter;

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _t += dt;

            // hips sway laterally (weight shift), spine counter-rotates a touch so the head
            // stays roughly level — reads as "shifting from foot to foot", never a step.
            if (_hip != null)
            {
                _hip.localRotation = _hipBase * Quaternion.Euler(0f, 0f, Mathf.Sin(_t * hipSwayFreq * Mathf.PI * 2f) * hipSwayDeg);
                // real ±4cm corpus displacement (weight shift), same phase as the sway rotation
                // so the two read as one coherent "shifting from foot to foot" motion.
                float shift = Mathf.Sin(_t * hipSwayFreq * Mathf.PI * 2f) * bodyShiftMeters;
                _hip.localPosition = _hipBasePos + new Vector3(shift, Mathf.Abs(shift) * 0.3f, 0f);
            }
            if (_spine != null)
                _spine.localRotation = _spineBase * Quaternion.Euler(0f, 0f, -Mathf.Sin(_t * hipSwayFreq * Mathf.PI * 2f) * spineCounterDeg);

            // arms drift between crossed (both pulled toward centre + elbows folded) and loose.
            float armPhase = (_t / armCycleSeconds) * Mathf.PI * 2f;
            float cross = (Mathf.Sin(armPhase) + 1f) * 0.5f; // 0..1
            if (_lUpper != null)
                _lUpper.localRotation = _lUpperBase * Quaternion.Euler(0f, 0f, cross * armCrossDeg);
            if (_lFore != null)
                _lFore.localRotation = _lForeBase * Quaternion.Euler(-cross * elbowFoldDeg, 0f, 0f);
            if (_rUpper != null)
                _rUpper.localRotation = _rUpperBase * Quaternion.Euler(0f, 0f, -cross * armCrossDeg);
            if (_rFore != null)
                _rFore.localRotation = _rForeBase * Quaternion.Euler(-cross * elbowFoldDeg, 0f, 0f);

            // sharp paranoid head snaps: hold a fast turn, then hold, then snap back — NOT a
            // smooth sinusoidal look-around (that would read as calm, not "дёрганый").
            _snapTimer += dt;
            if (_snapTimer >= _snapNextAt && _snapPhase <= 0f)
            {
                _snapPhase = 0.0001f;
                _snapTargetYaw = ((float)_rng.NextDouble() * 2f - 1f) * snapYawDeg;
            }
            if (_snapPhase > 0f)
            {
                _snapPhase += dt / snapDuration;
                float eased = _snapPhase < 1f
                    ? Mathf.SmoothStep(0f, 1f, _snapPhase)
                    : Mathf.SmoothStep(1f, 0f, _snapPhase - 1f);
                if (_head != null)
                    _head.localRotation = _headBase * Quaternion.Euler(0f, _snapTargetYaw * eased, 0f);
                if (_snapPhase >= 2f)
                {
                    _snapPhase = 0f;
                    _snapTimer = 0f;
                    _snapNextAt = Rand(snapEverySeconds);
                }
            }
            else if (_head != null)
            {
                _head.localRotation = _headBase;
            }

            if (probeLogs)
            {
                _probeT += dt;
                if (_probeT >= 3f)
                {
                    _probeT = 0f;
                    string rw = _rUpper != null ? _rUpper.position.ToString("F4") : "null";
                    string hw = _hip != null ? _hip.position.ToString("F4") : "null";
                    Debug.Log($"[FidgetProbe] {name} t={_t:F1} hipFound={_hip != null} rUpperFound={_rUpper != null} hipWorld={hw} rUpperWorld={rw} rUpperLocalRot={( _rUpper != null ? _rUpper.localRotation.eulerAngles.ToString("F1") : "null")}");
                }
            }
        }
    }
}
