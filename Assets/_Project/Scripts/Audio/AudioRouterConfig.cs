using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-F03 fallback: ScriptableObject хранящий volume hierarchy когда нет native
    /// AudioMixer asset. Runtime `AudioRouter.cs` (Scripts/Audio) читает эти значения
    /// и устанавливает `AudioSource.volume` через tag-based routing.
    ///
    /// Mix hierarchy по skill `game-audio`:
    ///   Voice 0dB reference (volume 1.0)
    ///   Player SFX -3 to -6dB (0.5-0.7)
    ///   Music -6 to -12dB (0.25-0.5)
    ///   Ambient -12 to -18dB (0.125-0.25)
    ///   UI -3 to -6dB (0.5-0.7)
    ///
    /// Ducking: когда Voice играет — Music.volume *= duckingAmount (0.3 = ~-10dB).
    /// </summary>
    [CreateAssetMenu(menuName = "Afterhumans/AudioRouterConfig", fileName = "AudioRouterConfig")]
    public class AudioRouterConfig : ScriptableObject
    {
        [Header("Group volumes (linear, 0..1)")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Range(0f, 1f)] public float sfxVolume = 0.7f;
        [Range(0f, 1f)] public float ambientVolume = 0.25f;
        [Range(0f, 1f)] public float uiVolume = 0.6f;
        [Range(0f, 1f)] public float voVolume = 1f;

        [Header("Ducking")]
        [Tooltip("Multiplier applied to music/ambient when VO channel is active")]
        [Range(0f, 1f)] public float duckingAmount = 0.3f;

        [Tooltip("Fade time в секундах для duck/unduck transitions")]
        public float duckingFadeSeconds = 0.4f;
    }
}
