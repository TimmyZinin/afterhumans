using System.Collections.Generic;
using UnityEngine;
using Afterhumans.EditorTools;

namespace Afterhumans.AudioSystems
{
    /// <summary>
    /// BOT-F03 runtime consumer: reads `AudioRouterConfig` ScriptableObject и применяет
    /// volume hierarchy к AudioSources через tag routing. Альтернатива native
    /// AudioMixer asset которую мы не можем создать programmatically.
    ///
    /// Usage:
    /// - AudioSource должен иметь GameObject tag: `Audio_Music`, `Audio_SFX`,
    ///   `Audio_Ambient`, `Audio_UI`, `Audio_VO` (fallback default = SFX volume)
    /// - Router singleton scans registered sources каждые `updateIntervalSeconds`
    ///   и устанавливает `source.volume = config.{category}Volume * masterVolume`
    /// - Ducking: когда любой Audio_VO AudioSource.isPlaying → Music+Ambient
    ///   volumes *= duckingAmount с `duckingFadeSeconds` crossfade.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public class AudioRouter : MonoBehaviour
    {
        [SerializeField] private AudioRouterConfig config;
        [SerializeField] private float updateIntervalSeconds = 0.5f;

        public static AudioRouter Instance { get; private set; }

        private readonly List<AudioSource> _registered = new List<AudioSource>();
        private float _nextUpdate;
        private float _currentDuckMultiplier = 1f;  // 1 = no duck, config.duckingAmount = full duck

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Register(AudioSource source)
        {
            if (Instance == null) return;
            if (source == null) return;
            if (!Instance._registered.Contains(source)) Instance._registered.Add(source);
        }

        public static void Unregister(AudioSource source)
        {
            if (Instance == null) return;
            Instance._registered.Remove(source);
        }

        private void Update()
        {
            if (config == null) return;
            if (Time.time < _nextUpdate) return;
            _nextUpdate = Time.time + updateIntervalSeconds;

            // Check if any VO AudioSource playing → activate ducking
            bool voActive = false;
            for (int i = _registered.Count - 1; i >= 0; i--)
            {
                var s = _registered[i];
                if (s == null) { _registered.RemoveAt(i); continue; }
                if (s.CompareTag("Audio_VO") && s.isPlaying) { voActive = true; break; }
            }

            float targetDuck = voActive ? config.duckingAmount : 1f;
            _currentDuckMultiplier = Mathf.MoveTowards(
                _currentDuckMultiplier, targetDuck,
                Time.deltaTime * updateIntervalSeconds / Mathf.Max(0.01f, config.duckingFadeSeconds));

            // Apply volumes per category
            foreach (var source in _registered)
            {
                if (source == null) continue;
                float volume = GetVolumeForTag(source.tag);
                if (source.CompareTag("Audio_Music") || source.CompareTag("Audio_Ambient"))
                {
                    volume *= _currentDuckMultiplier;
                }
                source.volume = volume * config.masterVolume;
            }
        }

        private float GetVolumeForTag(string tag)
        {
            switch (tag)
            {
                case "Audio_Music":   return config.musicVolume;
                case "Audio_SFX":     return config.sfxVolume;
                case "Audio_Ambient": return config.ambientVolume;
                case "Audio_UI":      return config.uiVolume;
                case "Audio_VO":      return config.voVolume;
                default:              return config.sfxVolume;
            }
        }
    }
}
