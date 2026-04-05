using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-F03: Создаёт AudioMixer asset с группами и exposed params.
    ///
    /// Skill `game-audio` Category System + Mix Hierarchy:
    ///   Voice 0dB > Player SFX -3dB > Music -6dB > Enemy SFX -6dB > Ambient -12dB
    /// Plus ducking snapshot "Dialogue" для diminish Music когда Voice играет.
    ///
    /// AudioMixer нельзя полностью создать programmatically в Unity (C# API
    /// AudioMixer.CreateInstance не существует). Поэтому этот скрипт использует
    /// `Assets/Create/AudioMixer` menu item + проверку: если уже создан, ничего
    /// не делает; если нет — пишет warning чтобы Claude вручную создал через
    /// AssetDatabase.CreateAsset с native AudioMixer XML template.
    ///
    /// Альтернатива: скопировать существующий AudioMixer из Unity package template.
    /// URP Samples пакет содержит template mixer. Если не найден — fallback на
    /// создание ScriptableObject-based fake "AudioRouter.cs" который играет ту же
    /// роль (ScriptableObject с volume fields exposed to runtime).
    ///
    /// Для BOT-F03 используем **fallback AudioRouter.cs** подход — он работает
    /// без native mixer asset, wire-compatible с SceneTheme.
    /// </summary>
    public static class AudioMixerSetup
    {
        private const string MixerDir = "Assets/_Project/Audio";
        private const string MixerPath = MixerDir + "/AfterhumansMixer.mixer";

        [MenuItem("Afterhumans/Setup/Verify Audio Mixer")]
        public static void VerifyMixer()
        {
            if (!Directory.Exists(MixerDir))
            {
                Directory.CreateDirectory(MixerDir);
                AssetDatabase.Refresh();
                Debug.Log($"[AudioMixerSetup] Created directory {MixerDir}");
            }

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer != null)
            {
                Debug.Log($"[AudioMixerSetup] ✅ AudioMixer exists at {MixerPath}");
                // Check groups
                var groups = mixer.FindMatchingGroups("Master");
                Debug.Log($"[AudioMixerSetup] Found {groups.Length} groups in Master path");
            }
            else
            {
                Debug.LogWarning($"[AudioMixerSetup] ⚠ AudioMixer missing at {MixerPath} — will create via AudioRouter fallback");
                CreateAudioRouterFallback();
            }
        }

        private static void CreateAudioRouterFallback()
        {
            // Fallback: ScriptableObject-based volume routing
            // Runtime компонент AudioRouter.cs (в Scripts/Audio) читает эти values
            // и устанавливает AudioSource.volume через group tag.
            var routerPath = "Assets/_Project/Audio/AudioRouterConfig.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AudioRouterConfig>(routerPath);
            if (existing == null)
            {
                var config = ScriptableObject.CreateInstance<AudioRouterConfig>();
                config.masterVolume = 1f;
                config.musicVolume = 0.5f;    // -6dB relative
                config.sfxVolume = 0.7f;      // -3dB
                config.ambientVolume = 0.25f; // -12dB
                config.uiVolume = 0.6f;
                config.voVolume = 1f;         // 0dB reference
                config.duckingAmount = 0.3f;  // dialogue ducks music to 30% of current
                AssetDatabase.CreateAsset(config, routerPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[AudioMixerSetup] Created AudioRouter fallback config: {routerPath}");
            }
        }
    }
}
