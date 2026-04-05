using UnityEngine;

namespace Afterhumans.Art
{
    /// <summary>
    /// BOT-N02: Procedural idle animation for NPCs that lack skeletal rigs.
    ///
    /// Kenney blocky-characters pack ships without bones, so we can't use
    /// Mixamo retargeting. Instead we fake breathing + gentle sway via simple
    /// transform offsets — cheaper than imported animations and good enough
    /// to pass the silhouette liveliness test (BOT-T08).
    ///
    /// Each NPC gets a different seed via phase offset so the group doesn't
    /// bob in sync. Parameters tuned for "alive, tranquil, not idle-walk":
    /// - vertical bob: ±2cm at 0.6 Hz (inhale/exhale breathing)
    /// - yaw sway:     ±4° at 0.3 Hz (weight shift)
    /// - head tilt:    ±2° at 0.4 Hz (head movement)
    ///
    /// Skill references:
    /// - `game-art` §silhouette liveliness: static characters read as
    ///   statues; motion makes them feel alive even without full anim
    /// - `3d-games` §perf: transform math, zero allocation, <0.01ms/frame
    ///
    /// Applied via BotanikaNpcPopulator to every spawned NPC. Optional
    /// headTransform override for per-character head tilting.
    /// </summary>
    public class NpcIdleBob : MonoBehaviour
    {
        [Header("Breathing (vertical)")]
        public float bobAmplitude = 0.018f;  // 1.8cm
        public float bobFrequency = 0.6f;    // Hz

        [Header("Sway (yaw)")]
        public float swayAmplitudeDeg = 4f;
        public float swayFrequency = 0.3f;

        [Header("Head tilt")]
        public Transform headTransform;  // optional
        public float tiltAmplitudeDeg = 2f;
        public float tiltFrequency = 0.4f;

        [Header("Desync")]
        [Range(0f, 1f)] public float phase = 0f;

        private Vector3 _basePos;
        private Quaternion _baseRot;
        private Quaternion _baseHeadRot;

        private void Awake()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
            if (headTransform != null)
            {
                _baseHeadRot = headTransform.localRotation;
            }
        }

        private void Update()
        {
            float t = Time.time;
            float bob = Mathf.Sin((t * bobFrequency + phase) * Mathf.PI * 2f) * bobAmplitude;
            float sway = Mathf.Sin((t * swayFrequency + phase * 0.7f) * Mathf.PI * 2f) * swayAmplitudeDeg;

            transform.localPosition = _basePos + Vector3.up * bob;
            transform.localRotation = _baseRot * Quaternion.Euler(0f, sway, 0f);

            if (headTransform != null)
            {
                float tilt = Mathf.Sin((t * tiltFrequency + phase * 1.3f) * Mathf.PI * 2f) * tiltAmplitudeDeg;
                headTransform.localRotation = _baseHeadRot * Quaternion.Euler(tilt, 0f, tilt * 0.5f);
            }
        }

        /// <summary>
        /// Public hook for BotanikaNpcPopulator to set per-NPC phase offset
        /// programmatically so each character desyncs from siblings.
        /// </summary>
        public void SetPhase(float p)
        {
            phase = Mathf.Repeat(p, 1f);
        }
    }
}
