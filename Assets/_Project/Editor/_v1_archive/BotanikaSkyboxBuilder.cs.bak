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

        // Golden-hour tuning constants (ART_BIBLE §4.1 Botanika afternoon sun).
        // Expected/asserted by Verify() to catch manual material edits that drift
        // from the authored values. If you intentionally change these, update
        // SceneTheme.Botanika asset AND these constants together.
        private const float ExpectedExposure = 1.15f;
        private const float ExpectedRotation = 180f;
        private const float FloatTolerance = 0.01f;

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

            // Load theme FIRST so we can read SceneTheme.skyboxExposure/Rotation values
            // before creating the material (mm-review LOW: data-driven values).
            var theme = AssetDatabase.LoadAssetAtPath<SceneTheme>(BotanikaThemePath);
            if (theme == null)
            {
                Debug.LogError($"[BotanikaSkyboxBuilder] Botanika theme not found at {BotanikaThemePath}. " +
                    "Run Afterhumans/Setup/Build Scene Themes first.");
                return;
            }

            // Apply golden-hour defaults to theme fields if still at neutral defaults.
            // These values match ART_BIBLE §4.1 warm afternoon sun through east windows.
            if (Mathf.Approximately(theme.skyboxExposure, 1f) &&
                Mathf.Approximately(theme.skyboxRotation, 0f))
            {
                var soTheme = new SerializedObject(theme);
                var expProp = soTheme.FindProperty("skyboxExposure");
                var rotProp = soTheme.FindProperty("skyboxRotation");
                if (expProp != null) expProp.floatValue = ExpectedExposure;
                if (rotProp != null) rotProp.floatValue = ExpectedRotation;
                soTheme.ApplyModifiedPropertiesWithoutUndo();
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            bool createdNew = false;
            if (mat == null)
            {
                mat = new Material(panoramicShader);
                try
                {
                    AssetDatabase.CreateAsset(mat, SkyboxPath);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BotanikaSkyboxBuilder] Failed to create material at {SkyboxPath}: {ex.Message}");
                    return;
                }
                createdNew = true;
                Debug.Log($"[BotanikaSkyboxBuilder] Created material {SkyboxPath}");
            }
            else
            {
                mat.shader = panoramicShader;
            }

            // Panoramic shader properties (Unity 6 URP 17.0.4 built-in Skybox/Panoramic):
            // _MainTex = equirectangular HDR
            // _Mapping = Latitude/Longitude Layout (1)
            // _ImageType = 360 Degrees (0) or 180 Degrees (1)
            // _MirrorOnBack = 0
            // _Layout = None (0), Side by Side (1), Over Under (2)
            // _Exposure = HDR intensity multiplier
            // _Rotation = yaw offset 0..360°
            // _Tint = NOT set here — warmth comes from exposure + HDRI source colors.
            //         Overriding _Tint would fight the HDRI's authored white point.
            mat.SetTexture("_MainTex", hdri);
            mat.SetFloat("_Mapping", 1f);
            mat.SetFloat("_ImageType", 0f);
            mat.SetFloat("_MirrorOnBack", 0f);
            mat.SetFloat("_Layout", 0f);
            mat.SetFloat("_Exposure", theme.skyboxExposure);
            mat.SetFloat("_Rotation", theme.skyboxRotation);

            EditorUtility.SetDirty(mat);

            // 4. Wire material into theme.skyboxMaterial via SerializedObject.
            // mm-review HIGH: null-check FindProperty (field rename would crash).
            // mm-review LOW: warn on silent overwrite of pre-existing artist material.
            if (!createdNew && theme.skyboxMaterial != null && theme.skyboxMaterial != mat)
            {
                Debug.LogWarning($"[BotanikaSkyboxBuilder] Overwriting existing skyboxMaterial " +
                    $"'{theme.skyboxMaterial.name}' on Botanika.asset with Skybox_Botanika.mat. " +
                    $"If that material was hand-curated by an artist, back it up before re-running.");
            }

            var so = new SerializedObject(theme);
            var prop = so.FindProperty("skyboxMaterial");
            if (prop == null)
            {
                Debug.LogError("[BotanikaSkyboxBuilder] SceneTheme.skyboxMaterial SerializedProperty " +
                    "not found — field may have been renamed. Check SceneTheme.cs.");
                return;
            }
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
        /// Verification helper: asserts skybox wiring and tuning is correct.
        /// Called by BotanikaVerification.RunAll (BOT-T01).
        ///
        /// mm-review CRITICAL fix: previously only checked references. Now also
        /// validates _Exposure, _Rotation, _Mapping, _ImageType values so manual
        /// material edits don't silently drift from authored golden-hour tuning.
        /// </summary>
        public static bool Verify(out string reason)
        {
            var hdri = AssetDatabase.LoadAssetAtPath<Texture2D>(HdriPath);
            if (hdri == null) { reason = $"HDRI missing or wrong shape at {HdriPath}"; return false; }

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

            // Value asserts (mm-review CRITICAL fix)
            float exposure = mat.GetFloat("_Exposure");
            if (Mathf.Abs(exposure - ExpectedExposure) > FloatTolerance)
            {
                reason = $"Skybox _Exposure={exposure:F3} expected {ExpectedExposure:F3} (±{FloatTolerance})";
                return false;
            }
            float rotation = mat.GetFloat("_Rotation");
            if (Mathf.Abs(rotation - ExpectedRotation) > FloatTolerance)
            {
                reason = $"Skybox _Rotation={rotation:F1}° expected {ExpectedRotation:F1}°";
                return false;
            }
            if (!Mathf.Approximately(mat.GetFloat("_Mapping"), 1f))
            {
                reason = $"Skybox _Mapping={mat.GetFloat("_Mapping")} expected 1 (Latitude/Longitude)";
                return false;
            }
            if (!Mathf.Approximately(mat.GetFloat("_ImageType"), 0f))
            {
                reason = $"Skybox _ImageType={mat.GetFloat("_ImageType")} expected 0 (360 degrees)";
                return false;
            }

            var theme = AssetDatabase.LoadAssetAtPath<SceneTheme>(BotanikaThemePath);
            if (theme == null) { reason = $"Botanika theme missing at {BotanikaThemePath}"; return false; }
            if (theme.skyboxMaterial != mat)
            {
                reason = "Botanika.skyboxMaterial not wired to Skybox_Botanika.mat";
                return false;
            }
            // Theme data fields must match the material (single source of truth).
            if (Mathf.Abs(theme.skyboxExposure - ExpectedExposure) > FloatTolerance)
            {
                reason = $"SceneTheme.skyboxExposure={theme.skyboxExposure:F3} expected {ExpectedExposure:F3}";
                return false;
            }
            if (Mathf.Abs(theme.skyboxRotation - ExpectedRotation) > FloatTolerance)
            {
                reason = $"SceneTheme.skyboxRotation={theme.skyboxRotation:F1}° expected {ExpectedRotation:F1}°";
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
