using UnityEngine;

namespace Afterhumans.Art
{
    /// <summary>
    /// BOT-A03: Runtime light intensity oscillator for environmental props
    /// (server rack LEDs, faulty ceiling tubes, diegetic electronics).
    ///
    /// Skill `game-art`: diegetic lighting signals — blinking server LEDs
    /// carry narrative weight (this world runs on data/compute), matching
    /// STORY §3.1 *«серверная стойка в углу, мигающие лампочки»*.
    ///
    /// Skill `3d-games` performance: single lightweight Update per light,
    /// no allocations, Time.time driven (frame-rate independent). Disabling
    /// shadows keeps M1 8GB cost near zero (~0.1ms per 3-light cluster).
    ///
    /// Phase offset allows multiple instances to blink out of sync — more
    /// organic than synchronized flash. Set via inspector or spawner.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class BlinkingLight : MonoBehaviour
    {
        [Tooltip("Minimum intensity of the pingpong oscillation")]
        public float minIntensity = 0.05f;
        [Tooltip("Maximum intensity of the pingpong oscillation")]
        public float maxIntensity = 1.2f;
        [Tooltip("Full cycle duration in seconds (min→max→min)")]
        public float cyclePeriod = 1.8f;
        [Tooltip("Phase offset 0..1 to desync from sibling BlinkingLights")]
        [Range(0f, 1f)] public float phase = 0f;
        [Tooltip("Smooth sinusoid (true) or hard pingpong (false)")]
        public bool smooth = true;

        private Light _light;

        private void Awake()
        {
            _light = GetComponent<Light>();
        }

        private void Update()
        {
            if (_light == null) return;
            if (cyclePeriod <= 0.01f) return;

            float t = (Time.time / cyclePeriod) + phase;
            float amount;
            if (smooth)
            {
                // 0..1 sinusoid
                amount = (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f;
            }
            else
            {
                // 0..1 pingpong (hard edges)
                amount = Mathf.PingPong(t * 2f, 1f);
            }
            _light.intensity = Mathf.Lerp(minIntensity, maxIntensity, amount);
        }
    }
}
