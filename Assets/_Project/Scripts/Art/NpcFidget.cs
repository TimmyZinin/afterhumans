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
        [Header("Weight shift (hips/spine only — root stays put)")]
        public float hipSwayDeg = 5f;
        public float hipSwayFreq = 0.22f;
        public float spineCounterDeg = 3f;

        [Header("Arms cross/uncross")]
        public float armCycleSeconds = 4.2f;
        public float armCrossDeg = 34f;
        public float elbowFoldDeg = 55f;

        [Header("Paranoid head snaps")]
        public float snapEverySeconds = 2.6f;
        public float snapJitter = 1.2f;
        public float snapYawDeg = 30f;
        public float snapDuration = 0.35f;

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

            if (_hip != null) _hipBase = _hip.localRotation;
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
                _hip.localRotation = _hipBase * Quaternion.Euler(0f, 0f, Mathf.Sin(_t * hipSwayFreq * Mathf.PI * 2f) * hipSwayDeg);
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
        }
    }
}
