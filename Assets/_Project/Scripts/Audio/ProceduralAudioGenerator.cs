using UnityEngine;

namespace Afterhumans.Audio
{
    /// <summary>
    /// BOT-S06/S07: Generates simple procedural AudioClips for MVP audio
    /// placeholder. Real CC0 SFX files replace these when Денис delivers
    /// sound design (per docs/DENIS_BRIEF.md).
    ///
    /// Methods generate mono 44100Hz clips:
    /// - AmbientDrone: low warm pad (110Hz + 220Hz overtone)
    /// - FootstepClick: short percussive tap with noise
    /// - DoorChime: simple bell-like tone
    /// </summary>
    public static class ProceduralAudioGenerator
    {
        private const int SampleRate = 44100;

        public static AudioClip CreateAmbientDrone(float durationSec = 10f)
        {
            int sampleCount = (int)(SampleRate * durationSec);
            var clip = AudioClip.Create("ProceduralAmbient", sampleCount, 1, SampleRate, false);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                // Warm pad: fundamental 110Hz + soft overtone + slow LFO
                float lfo = 1f + 0.15f * Mathf.Sin(t * 0.3f * Mathf.PI * 2f);
                float fundamental = Mathf.Sin(t * 110f * Mathf.PI * 2f) * 0.25f;
                float overtone = Mathf.Sin(t * 220f * Mathf.PI * 2f) * 0.08f;
                float sub = Mathf.Sin(t * 55f * Mathf.PI * 2f) * 0.12f;
                data[i] = (fundamental + overtone + sub) * lfo * 0.4f;
                // mm-review HIGH fix: symmetrical crossfade — fade in first 0.5s
                // AND fade out last 0.5s for seamless loop point continuity.
                int fadeLen = SampleRate / 2;
                if (i < fadeLen)
                {
                    data[i] *= (float)i / fadeLen;
                }
                else if (i > sampleCount - fadeLen)
                {
                    data[i] *= (float)(sampleCount - i) / fadeLen;
                }
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateFootstep(int variation = 0)
        {
            int sampleCount = (int)(SampleRate * 0.12f);  // 120ms
            var clip = AudioClip.Create($"ProceduralStep_{variation}", sampleCount, 1, SampleRate, false);
            float[] data = new float[sampleCount];
            var rng = new System.Random(42 + variation);
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Exp(-t * 30f);  // fast decay
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float tone = Mathf.Sin(t * SampleRate * (200f + variation * 30f) * Mathf.PI * 2f / SampleRate);
                data[i] = (noise * 0.6f + tone * 0.4f) * envelope * 0.5f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateChime()
        {
            int sampleCount = (int)(SampleRate * 0.8f);  // 800ms
            var clip = AudioClip.Create("ProceduralChime", sampleCount, 1, SampleRate, false);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-t * 4f);
                float bell = Mathf.Sin(t * 880f * Mathf.PI * 2f) * 0.5f
                           + Mathf.Sin(t * 1320f * Mathf.PI * 2f) * 0.25f
                           + Mathf.Sin(t * 1760f * Mathf.PI * 2f) * 0.12f;
                data[i] = bell * envelope * 0.35f;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
