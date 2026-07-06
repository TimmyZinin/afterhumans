using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint A (sound): wires scene-level audio — greenhouse ambient loop, quiet music bed,
    /// and a spatial kitchen loop near Кирилл's stove. Separate file from BotanikaBuilder on
    /// purpose: the scene-artist owns BotanikaBuilder.cs edits this sprint, so audio wiring
    /// must not touch that file. Run AFTER WireBotanikaNpcs:
    ///   -executeMethod Afterhumans.EditorTools.BotanikaAudioWiring.WireSceneAudio
    ///
    /// Robust to missing assets: each source is wired only if a clip is found in its folder
    /// (the sound-designer fills Ambient/, Music/, SFX/ independently) — no clip, no object,
    /// no errors. Idempotent via GameObject.Find guards.
    /// </summary>
    public static class BotanikaAudioWiring
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_Botanika.unity";

        public static void WireSceneAudio()
        {
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int wired = 0;

            // 1) Greenhouse ambient — muffled birds/wind. 2D bed, audible from spawn (the
            //    acceptance gate requires sound BEFORE any interaction, once the browser
            //    unlocks the AudioContext on first input).
            var ambient = FirstClip("Assets/_Project/Audio/Ambient");
            if (ambient != null)
            {
                var src = EnsureSource("Audio_Ambient", Vector3.zero, spatial: false);
                src.clip = ambient; src.volume = 0.38f; src.loop = true; src.playOnAwake = true;
                wired++;
            }
            else Debug.LogWarning("[AudioWiring] no ambient clip in Audio/Ambient — skipped");

            // 2) Music bed — very quiet, must sit UNDER the voices (VO ducking is overkill for
            //    the demo; a low static level reads as score, not competition).
            // Prefer the chillwave bed over alphabetical-first (anamalie drone): Tim's
            // 6 Jul report — «музыка пропала» — the 0.16 bed under a 0.38 ambient was
            // inaudible in the live build, so the score must sit clearly above the floor.
            var music = ClipMatching("Assets/_Project/Audio/Music", new[] { "chillwave" })
                        ?? FirstClip("Assets/_Project/Audio/Music");
            if (music != null)
            {
                var src = EnsureSource("Audio_Music", Vector3.zero, spatial: false);
                src.clip = music; src.volume = 0.34f; src.loop = true; src.playOnAwake = true;
                wired++;
            }
            else Debug.LogWarning("[AudioWiring] no music clip in Audio/Music — skipped");

            // 3) Kitchen pot near Кирилл's stove (x≈-4.5, z≈2) — spatial, so walking up to the
            //    kitchen literally sounds like walking up to a boiling pot.
            var kitchen = ClipMatching("Assets/_Project/Audio/SFX",
                                       new[] { "boil", "pot", "kitchen", "bubbl", "cook" });
            if (kitchen != null)
            {
                var src = EnsureSource("Audio_Kitchen", new Vector3(-4.6f, 0.9f, 2.0f), spatial: true);
                src.clip = kitchen; src.volume = 0.85f; src.loop = true; src.playOnAwake = true;
                src.minDistance = 1.2f; src.maxDistance = 9f;
                wired++;
            }
            else Debug.LogWarning("[AudioWiring] no kitchen loop in Audio/SFX — skipped");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[AudioWiring] DONE wired={wired}/3 (ambient/music/kitchen)");
        }

        private static AudioSource EnsureSource(string name, Vector3 pos, bool spatial)
        {
            // NO `??` with UnityEngine.Object: GetComponent returns a fake-null wrapper that
            // `??` treats as non-null → AddComponent never ran → MissingComponentException on
            // the first property write (hit live in chain A). Explicit == null uses Unity's
            // overloaded operator and is the only safe form.
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            go.transform.position = pos;
            var src = go.GetComponent<AudioSource>();
            if (src == null) src = go.AddComponent<AudioSource>();
            src.spatialBlend = spatial ? 1f : 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            return src;
        }

        private static AudioClip FirstClip(string dir)
        {
            if (!Directory.Exists(dir)) return null;
            return Directory.GetFiles(dir, "*.ogg").Concat(Directory.GetFiles(dir, "*.mp3"))
                .Concat(Directory.GetFiles(dir, "*.wav"))
                .OrderBy(f => f)
                .Select(f => AssetDatabase.LoadAssetAtPath<AudioClip>(f.Replace('\\', '/')))
                .FirstOrDefault(c => c != null);
        }

        private static AudioClip ClipMatching(string dir, string[] keywords)
        {
            if (!Directory.Exists(dir)) return null;
            return Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".ogg") || f.EndsWith(".mp3") || f.EndsWith(".wav"))
                .Where(f => keywords.Any(k => Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains(k)))
                .OrderBy(f => f)
                .Select(f => AssetDatabase.LoadAssetAtPath<AudioClip>(f.Replace('\\', '/')))
                .FirstOrDefault(c => c != null);
        }
    }
}
