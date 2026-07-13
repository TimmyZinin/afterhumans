using UnityEngine;

namespace Afterhumans.Art
{
    /// <summary>
    /// E-sprint (12 июл, приказ Тима): purge — this used to also drive Стас's arms (cross/
    /// uncross sweep on top of a T-pose-correcting base). Tim's 5-frame 1s-interval screenshot
    /// series caught what a 2-frame comparison missed: the swing amplitude needed to escape the
    /// raw T-pose bind still swept back UP through horizontal at its own extremum every cycle
    /// («вдоль тела → горизонталь (T) → обратно»). Order: «плечи не трогает НИЧЕГО
    /// процедурное». Arm bind-pose correction now lives in NpcRestPose (Awake-only, static, no
    /// oscillation) — this component keeps ONLY the micro life Tim explicitly still allows:
    /// torso sway capped at 2°, a head turn capped at 10° on a slow 6-10s period. No limbs.
    /// </summary>
    public class NpcFidget : MonoBehaviour
    {
        [Header("Torso sway (micro — Tim's 12 июл cap: <=2 deg)")]
        public float hipSwayDeg = 1.6f;
        public float hipSwayPeriodSeconds = 8f;   // slow — no relation to the old 4s arm cycle

        [Header("Head turn (micro — Tim's 12 июл cap: <=10 deg, 6-10s period)")]
        public float headYawDeg = 8f;
        public float headTurnEverySeconds = 8f;   // base; randomised, clamped into 6-10s
        public float headTurnJitterSeconds = 1.5f;
        public float headTurnDuration = 1.6f;

        private Transform _hip, _spine, _head;
        private Quaternion _hipBase, _spineBase, _headBase;
        private float _t;
        private float _headTimer;
        private float _headNextAt;
        private float _headPhase;
        private float _headTargetYaw;
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
            _head = FindBone("Head");
            if (_hip != null) _hipBase = _hip.localRotation;
            if (_spine != null) _spineBase = _spine.localRotation;
            if (_head != null) _headBase = _head.localRotation;
            _headNextAt = Rand(headTurnEverySeconds);
        }

        private float Rand(float baseSeconds) =>
            Mathf.Clamp(baseSeconds + ((float)_rng.NextDouble() * 2f - 1f) * headTurnJitterSeconds, 6f, 10f);

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _t += dt;

            // torso: a barely-visible weight-shift, well under the 2 deg cap — no position
            // shift (that was arm-adjacent "life", dropped along with the limb code).
            float phase = Mathf.Sin(_t * (Mathf.PI * 2f / hipSwayPeriodSeconds));
            if (_hip != null)
                _hip.localRotation = _hipBase * Quaternion.Euler(0f, 0f, phase * hipSwayDeg);
            if (_spine != null)
                _spine.localRotation = _spineBase * Quaternion.Euler(0f, -phase * (hipSwayDeg * 0.6f), 0f);

            // head: an occasional slow glance, hold, return — not continuous, so it doesn't
            // read as a tic. Capped at headYawDeg (<=10 per order).
            _headTimer += dt;
            if (_headTimer >= _headNextAt && _headPhase <= 0f)
            {
                _headPhase = 0.0001f;
                _headTargetYaw = ((float)_rng.NextDouble() * 2f - 1f) * headYawDeg;
            }
            if (_headPhase > 0f)
            {
                _headPhase += dt / headTurnDuration;
                float eased = _headPhase < 1f
                    ? Mathf.SmoothStep(0f, 1f, _headPhase)
                    : Mathf.SmoothStep(1f, 0f, _headPhase - 1f);
                if (_head != null)
                    _head.localRotation = _headBase * Quaternion.Euler(0f, _headTargetYaw * eased, 0f);
                if (_headPhase >= 2f)
                {
                    _headPhase = 0f;
                    _headTimer = 0f;
                    _headNextAt = Rand(headTurnEverySeconds);
                }
            }
            else if (_head != null)
            {
                _head.localRotation = _headBase;
            }
        }
    }
}
