using System.IO;
using UnityEditor;
using UnityEngine;
using Afterhumans.Art;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-A02: Build Skybox_Botanika material from Poly Haven HDRI and wire it
    /// into Botanika.asset SceneTheme.
    ///
    /// Skill references:
    /// - `3d-web-experience`: HDRI environment for realistic lighting and reflections
    /// - `game-art`: golden hour warm ambiance matching ART_BIBLE §4.1
    /// - `theme-factory`: data-driven theme wiring via SerializedObject
    ///
    /// HDRI choice rationale (/self-adversarial BOT-A02):
    /// План изначально предлагал `the_sky_is_on_fire_2k` — но это драматический
    /// red/orange sunset, агрессивный, не матчит STORY §3.1 «тихий оазис, afternoon
    /// sun». `kloppenheim_06_puresky_2k` — спокойный golden hour без драмы, точнее
    /// матчит narrative tone. Fallback chain в DownloadHdri если primary 404.
    ///
    /// Idempotent: re-run safe — material overwrite, theme field re-assigned.
    /// </summary>
    public static class BotanikaSkyboxBuilder
    {
        private const string HdriPath = "Assets/_Project/Vendor/PolyHaven/kloppenheim_06_puresky_2k.hdr";
        private const string SkyboxDir = "Assets/_Project/Materials/Skyboxes";
        private const string SkyboxPath = "Assets/_Project/Materials/Skyboxes/Skybox_Botanika.mat";
        private const string BotanikaThemePath = "Assets/_Project/Art/Themes/Botanika.asset";

        [MenuItem("Afterhumans/Art/Build Botanika Skybox")]
        public static void Build()
        {
            // 1. Verify HDRI file exists on disk (before load — LoadAssetAtPath caches
            // stale objects that get destroyed by ForceUpdate reimport).
            if (!System.IO.File.Exists(HdriPath))
            {
                Debug.LogError($"[BotanikaSkyboxBuilder] HDRI not found at {HdriPath}. " +
                    "Run: curl -sL -A 'Mozilla/5.0' -o Assets/_Project/Vendor/PolyHaven/kloppenheim_06_puresky_2k.hdr " +
                    "https://dl.polyhaven.org/file/ph-assets/HDRIs/hdr/2k/kloppenheim_06_puresky_2k.hdr");
                return;
            }

            // Force reimport FIRST so HdriAssetPostprocessor applies current settings
            // (previous version may have forced Cube shape — need to refresh to 2D).
            AssetDatabase.ImportAsset(HdriPath, ImportAssetOptions.ForceUpdate);

            // Load AFTER reimport to get fresh Texture2D reference (equirectangular).
            var hdri = AssetDatabase.LoadAssetAtPath<Texture2D>(HdriPath);
            if (hdri == null)
            {
                Debug.LogError($"[BotanikaSkyboxBuilder] HDRI failed to load as Texture2D at {HdriPath} " +
                    "after reimport. Check HdriAssetPostprocessor.textureShape setting.");
                return;
            }

            // 2. Ensure directory
            if (!Directory.Exists(SkyboxDir))
            {
                Directory.CreateDirectory(SkyboxDir);
                AssetDatabase.Refresh();
            }

            // 3. Create or update Skybox_Botanika.mat
            var panoramicShader = Shader.Find("Skybox/Panoramic");
            if (panoramicShader == null)
            {
                Debug.LogError("[BotanikaSkyboxBuilder] Skybox/Panoramic shader not found. " +
                    "This is a built-in Unity shader, should always be available.");
                return;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (mat == null)
            {
                mat = new Material(panoramicShader);
                AssetDatabase.CreateAsset(mat, SkyboxPath);
                Debug.Log($"[BotanikaSkyboxBuilder] Created material {SkyboxPath}");
            }
            else
            {
                mat.shader = panoramicShader;
            }

            // Panoramic shader properties:
            // _MainTex = equirectangular HDR
            // _Mapping = Latitude/Longitude Layout (1)
            // _ImageType = 360 Degrees (0) or 180 Degrees (1)
            // _MirrorOnBack = 0
            // _Layout = None (0), Side by Side (1), Over Under (2)
            // _Exposure = 1.0 (HDR intensity multiplier)
            // _Rotation = 0..360 (yaw offset)
            mat.SetTexture("_MainTex", hdri);
            mat.SetFloat("_Mapping", 1f);       // Latitude/Longitude
            mat.SetFloat("_ImageType", 0f);     // 360 degrees
            mat.SetFloat("_MirrorOnBack", 0f);
            mat.SetFloat("_Layout", 0f);        // None (single image)
            mat.SetFloat("_Exposure", 1.15f);   // slightly lifted for warmth
            mat.SetFloat("_Rotation", 180f);    // sun comes through east windows

            EditorUtility.SetDirty(mat);

            // 4. Wire into Botanika.asset SceneTheme via SerializedObject
            var theme = AssetDatabase.LoadAssetAtPath<SceneTheme>(BotanikaThemePath);
            if (theme == null)
            {
                Debug.LogError($"[BotanikaSkyboxBuilder] Botanika theme not found at {BotanikaThemePath}. " +
                    "Run Afterhumans/Setup/Build Scene Themes first.");
                return;
            }

            var so = new SerializedObject(theme);
            var prop = so.FindProperty("skyboxMaterial");
            prop.objectReferenceValue = mat;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BotanikaSkyboxBuilder] DONE. Skybox_Botanika.mat wired into Botanika.asset. " +
                $"Shader={mat.shader.name}, Texture={hdri.name}, Exposure={mat.GetFloat("_Exposure")}, " +
                $"Rotation={mat.GetFloat("_Rotation")}°");
        }

        /// <summary>
        /// Verification helper: asserts skybox wiring is correct.
        /// Called by BotanikaVerification.RunAll (BOT-T01).
        /// </summary>
        public static bool Verify(out string reason)
        {
            var hdri = AssetDatabase.LoadAssetAtPath<Texture>(HdriPath);
            if (hdri == null) { reason = $"HDRI missing at {HdriPath}"; return false; }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (mat == null) { reason = $"Skybox material missing at {SkyboxPath}"; return false; }
            if (mat.shader == null || mat.shader.name != "Skybox/Panoramic")
            {
                reason = $"Skybox material shader != Skybox/Panoramic (got {mat.shader?.name ?? "null"})";
                return false;
            }
            if (mat.GetTexture("_MainTex") != hdri)
            {
                reason = "Skybox material _MainTex != HDRI";
                return false;
            }

            var theme = AssetDatabase.LoadAssetAtPath<SceneTheme>(BotanikaThemePath);
            if (theme == null) { reason = $"Botanika theme missing at {BotanikaThemePath}"; return false; }
            if (theme.skyboxMaterial != mat)
            {
                reason = "Botanika.skyboxMaterial not wired to Skybox_Botanika.mat";
                return false;
            }

            reason = "OK";
            return true;
        }

        [MenuItem("Afterhumans/Art/Verify Botanika Skybox")]
        public static void VerifyMenu()
        {
            string reason;
            bool ok = Verify(out reason);
            if (ok) Debug.Log($"[BotanikaSkyboxBuilder.Verify] PASS — {reason}");
            else Debug.LogError($"[BotanikaSkyboxBuilder.Verify] FAIL — {reason}");
        }
    }
}
