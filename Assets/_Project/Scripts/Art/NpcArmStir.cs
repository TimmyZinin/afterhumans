using UnityEngine;

namespace Afterhumans.Art
{
    /// <summary>
    /// Sprint D — Kirill "cooking" POC of the skeletal procedural pipeline (same recipe as
    /// CorgiStateAnimator/CorgiProceduralAnimator: no baked clip, drive the imported Tripo
    /// skeleton's bone Transforms directly in LateUpdate). Finds the right-arm chain by name
    /// on the shared kirill_animated_raw rig (39 Tripo bones, R_Upperarm/R_Forearm/R_Hand) and
    /// sweeps the hand through a small ellipse above the pot, with a light torso sway and an
    /// occasional head dip toward the stove. Fingers are left as imported (closed/neutral) —
    /// the auto-rig's hand weights are not something we're grading here.
    /// </summary>
    public class NpcArmStir : MonoBehaviour
    {
        [Header("Stir loop")]
        public float cycleSeconds = 1.8f;
        public float shoulderSwingDeg = 22f;
        public float elbowSwingDeg = 16f;
        public float wristSwingDeg = 10f;
        [Header("Body life")]
        public float torsoSwayDeg = 3.5f;
        public float torsoSwayFreq = 0.35f;
        public float headDipDeg = 6f;
        public float headDipEverySeconds = 5.5f;
        public float headDipDuration = 1.2f;

        private Transform _upperarm, _forearm, _hand, _spine, _head;
        private Quaternion _upperarmBase, _forearmBase, _handBase, _spineBase, _headBase;
        private float _t;
        private float _headTimer;

        private Transform FindBone(string exact)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == exact) return t;
            return null;
        }

        private void Awake()
        {
            _upperarm = FindBone("R_Upperarm");
            _forearm = FindBone("R_Forearm");
            _hand = FindBone("R_Hand");
            _spine = FindBone("Spine01");
            _head = FindBone("Head");

            if (_upperarm != null) _upperarmBase = _upperarm.localRotation;
            if (_forearm != null) _forearmBase = _forearm.localRotation;
            if (_hand != null) _handBase = _hand.localRotation;
            if (_spine != null) _spineBase = _spine.localRotation;
            if (_head != null) _headBase = _head.localRotation;

            _headTimer = Random.Range(0f, headDipEverySeconds);

            if (_upperarm == null || _forearm == null)
                Debug.LogWarning("[NpcArmStir] R_Upperarm/R_Forearm bone not found — stir arm will not animate.");
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _t += dt;
            float phase = (_t / cycleSeconds) * Mathf.PI * 2f;

            // elliptical stir: shoulder drives the wide sweep, elbow folds opposite phase so the
            // hand traces a small ellipse over the pot rather than a straight pendulum swing.
            if (_upperarm != null)
                _upperarm.localRotation = _upperarmBase * Quaternion.Euler(
                    Mathf.Sin(phase) * shoulderSwingDeg * 0.5f,
                    Mathf.Cos(phase) * shoulderSwingDeg,
                    0f);
            if (_forearm != null)
                _forearm.localRotation = _forearmBase * Quaternion.Euler(
                    -Mathf.Abs(Mathf.Sin(phase)) * elbowSwingDeg * 0.6f - elbowSwingDeg * 0.4f,
                    Mathf.Sin(phase + Mathf.PI * 0.5f) * elbowSwingDeg * 0.5f,
                    0f);
            if (_hand != null)
                _hand.localRotation = _handBase * Quaternion.Euler(0f, 0f, Mathf.Sin(phase) * wristSwingDeg);

            if (_spine != null)
                _spine.localRotation = _spineBase * Quaternion.Euler(
                    0f, Mathf.Sin(_t * torsoSwayFreq * Mathf.PI * 2f) * torsoSwayDeg, 0f);

            if (_head != null)
            {
                _headTimer += dt;
                float dipT = 0f;
                if (_headTimer > headDipEverySeconds)
                {
                    float local = _headTimer - headDipEverySeconds;
                    dipT = Mathf.Sin(Mathf.Clamp01(local / headDipDuration) * Mathf.PI);
                    if (local > headDipDuration) _headTimer = 0f;
                }
                _head.localRotation = _headBase * Quaternion.Euler(dipT * headDipDeg, 0f, 0f);
            }
        }
    }
}
