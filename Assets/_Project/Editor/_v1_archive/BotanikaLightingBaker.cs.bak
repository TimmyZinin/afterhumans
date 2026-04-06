using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-A09: Baked global illumination for Scene_Botanika.
    ///
    /// Creates a per-scene LightingSettings asset with conservative CPU
    /// Progressive Lightmapper parameters tuned for M1 8GB (low samples,
    /// small atlas, moderate texel density) and triggers a synchronous bake.
    ///
    /// Skill references:
    /// - `3d-games` §lighting: baked GI is the single biggest lift from
    ///   flat Kenney box look to Journey-level interior feel
    /// - `game-art` §4.1: warm indirect bounce reinforces the golden-hour
    ///   palette across non-sun-facing surfaces
    /// - `3d-web-experience`: realistic shadowing under furniture sells
    ///   volume and groundedness
    ///
    /// Bake settings (conservative for M1 8GB batch mode):
    /// - Mode: Baked Indirect (realtime direct, baked indirect + shadows)
    /// - Lightmapper: Progressive CPU
    /// - Resolution: 10 texels/unit
    /// - Atlas size: 512
    /// - Direct samples: 16, Indirect: 64, Environment: 16
    /// - AO enabled, intensity 1.0, max distance 1.0m
    /// - Compress: true (shrinks atlas to DXT5)
    ///
    /// Expected bake time: 2-6 minutes on M1 8GB.
    /// Prerequisite: BOT-F10 ContributeGI flags on props (done).
    /// </summary>
    public static class BotanikaLightingBaker
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_Botanika.unity";
        private const string LightingSettingsPath = "Assets/_Project/Settings/Lighting_Botanika.lighting";

        [MenuItem("Afterhumans/Art/Setup Botanika Lighting Settings")]
        public static void SetupLightingSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (settings == null)
            {
                settings = new LightingSettings { name = "Lighting_Botanika" };
                EnsureDir(Path.GetDirectoryName(LightingSettingsPath));
                AssetDatabase.CreateAsset(settings, LightingSettingsPath);
                Debug.Log($"[BotanikaLightingBaker] Created LightingSettings asset at {LightingSettingsPath}");
            }

            // Lighting mode
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly;

            // Atlas + resolution (M1 8GB conservative)
            settings.lightmapResolution = 10f;   // texels per unit
            settings.lightmapPadding = 2;
            settings.lightmapMaxSize = 512;
            settings.lightmapCompression = LightmapCompression.LowQuality;  // Unity 6 API

            // Sample counts (lower = faster, slightly noisier)
            settings.directSampleCount = 16;
            settings.indirectSampleCount = 64;
            settings.environmentSampleCount = 16;
            settings.maxBounces = 2;  // Unity 6 API (was .bounces)
            settings.filteringMode = LightingSettings.FilterMode.Auto;

            // Ambient Occlusion
            settings.ao = true;
            settings.aoMaxDistance = 1.0f;
            settings.aoExponentIndirect = 1.0f;
            settings.aoExponentDirect = 1.0f;

            // Realtime GI off — we want pure baked for controlled warmth
            settings.realtimeGI = false;
            settings.bakedGI = true;

            // Ambient only source
            // (ThemeLoader applies RenderSettings.ambientLight/Flat mode — baker honors it)

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[BotanikaLightingBaker] Lighting settings configured: ProgressiveCPU, res=10, atlas=512, samples 16/64/16, AO on");
        }

        [MenuItem("Afterhumans/Art/Bake Botanika Lighting")]
        public static void Bake()
        {
            SetupLightingSettings();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[BotanikaLightingBaker] Failed to open scene {ScenePath}");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (settings == null)
            {
                Debug.LogError($"[BotanikaLightingBaker] LightingSettings asset not found after SetupLightingSettings");
                return;
            }
            Lightmapping.lightingSettings = settings;

            // Ensure no stale lightmaps from previous bakes
            Lightmapping.Clear();

            // Count static ContributeGI GameObjects for sanity check
            int giObjects = 0;
            var allGos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGos)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                if ((flags & StaticEditorFlags.ContributeGI) != 0) giObjects++;
            }
            Debug.Log($"[BotanikaLightingBaker] Pre-bake: {giObjects} GameObjects with ContributeGI flag.");
            if (giObjects < 10)
            {
                Debug.LogWarning("[BotanikaLightingBaker] Very few ContributeGI objects — run BotanikaDresser.Dress first");
            }

            Debug.Log("[BotanikaLightingBaker] Starting synchronous bake (ProgressiveCPU, 10 tex/unit, 512 atlas). Expected 2-6 min on M1 8GB...");
            var startTime = System.DateTime.Now;
            bool ok = Lightmapping.Bake();
            var elapsed = (System.DateTime.Now - startTime).TotalSeconds;

            if (ok)
            {
                Debug.Log($"[BotanikaLightingBaker] Bake completed in {elapsed:F1}s. Saving scene.");
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogError($"[BotanikaLightingBaker] Bake FAILED after {elapsed:F1}s");
            }
        }

        [MenuItem("Afterhumans/Art/Verify Botanika Lighting")]
        public static void VerifyMenu()
        {
            // LightmapSettings.lightmaps is a per-scene struct populated at scene
            // load. Batch-mode entry points must open the scene first, otherwise
            // the verify will falsely report "no baked lightmaps" even when the
            // .exr/.png/LightingData.asset files exist on disk.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[BotanikaLightingBaker.Verify] Failed to open scene {ScenePath}");
                return;
            }
            string reason;
            bool ok = Verify(out reason);
            if (ok) Debug.Log($"[BotanikaLightingBaker.Verify] PASS — {reason}");
            else Debug.LogWarning($"[BotanikaLightingBaker.Verify] FAIL — {reason}");
        }

        public static bool Verify(out string reason)
        {
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (settings == null)
            {
                reason = $"LightingSettings asset missing at {LightingSettingsPath}";
                return false;
            }
            if (!settings.bakedGI)
            {
                reason = "LightingSettings.bakedGI is disabled";
                return false;
            }
            if (!settings.ao)
            {
                reason = "LightingSettings.ao is disabled";
                return false;
            }
            // Check that scene lightmap data exists (at least one baked lightmap)
            var lightmaps = LightmapSettings.lightmaps;
            if (lightmaps == null || lightmaps.Length == 0)
            {
                reason = "No baked lightmaps in LightmapSettings — run Afterhumans/Art/Bake Botanika Lighting";
                return false;
            }

            // mm-review HIGH: basic atlas sanity check so "lightmap exists but
            // is empty/black" is caught here instead of by screenshot harness.
            // Full visual AO validation is deferred to BOT-T04 screenshot pass
            // which compares rendered scene to ART_BIBLE palette histogram.
            var lmColor = lightmaps[0].lightmapColor;
            if (lmColor == null)
            {
                reason = "Lightmap[0].lightmapColor is null — bake produced no color atlas";
                return false;
            }
            if (lmColor.width < 64 || lmColor.height < 64)
            {
                reason = $"Lightmap atlas too small ({lmColor.width}×{lmColor.height}) — expected >=64×64";
                return false;
            }

            // Sanity: lightmap should not be entirely black or entirely white.
            // Sample 4 corners + center on a readable copy to detect failed bakes.
            if (lmColor.isReadable)
            {
                var samples = new[] {
                    lmColor.GetPixel(0, 0),
                    lmColor.GetPixel(lmColor.width - 1, 0),
                    lmColor.GetPixel(0, lmColor.height - 1),
                    lmColor.GetPixel(lmColor.width - 1, lmColor.height - 1),
                    lmColor.GetPixel(lmColor.width / 2, lmColor.height / 2),
                };
                float totalLuma = 0f;
                foreach (var c in samples) totalLuma += (c.r + c.g + c.b) / 3f;
                float avgLuma = totalLuma / samples.Length;
                if (avgLuma < 0.001f)
                {
                    reason = $"Lightmap appears entirely black (avg luma {avgLuma:F4}) — bake produced no light";
                    return false;
                }
                if (avgLuma > 0.99f)
                {
                    reason = $"Lightmap appears entirely white (avg luma {avgLuma:F4}) — sky/ambient blew out";
                    return false;
                }
            }
            // Note: visual AO-under-sofa validation is deferred to BOT-T04 /
            // BOT-T05 screenshot + histogram checks, which compare rendered
            // pixels against ART_BIBLE §3.1 warm palette expectations. This
            // Verify only asserts bake pipeline produced non-trivial output.

            reason = $"OK ({lightmaps.Length} lightmap atlas pages {lmColor.width}×{lmColor.height}, bakedGI + AO enabled, bake non-trivial)";
            return true;
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
