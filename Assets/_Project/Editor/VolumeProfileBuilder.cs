using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Afterhumans.Art;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-A01: Создаёт URP VolumeProfile assets с post-FX stack для каждой сцены
    /// по ART_BIBLE §5.1 (Botanika), §5.2 (City), §5.3 (Desert).
    ///
    /// Skill `3d-games` §2: *«Custom shaders for stylized rendering, performance,
    /// unique visual identity»*. Post-FX stack = единое визуальное identity.
    /// Skill `game-art` §3: color theory + mood/warmth per locale.
    /// Skill `frontend-design`: Tim's profile 6/7 colorful, playful mood, NOT retro.
    ///
    /// Each Volume Profile имеет:
    /// - Bloom (intensity/threshold)
    /// - Tonemapping ACES (cinematic)
    /// - ColorAdjustments (saturation/contrast)
    /// - Vignette (intensity/roundness)
    /// - FilmGrain (intensity/response)
    /// - WhiteBalance (temperature/tint)
    /// - ShadowsMidtonesHighlights (3-way color grade)
    /// - DepthOfField (focus distance, aperture)
    ///
    /// После создания — wiring в SceneTheme.postFxProfile через SceneThemeBuilder
    /// для автоматического applying через ThemeLoader.
    /// </summary>
    public static class VolumeProfileBuilder
    {
        private const string ProfilesDir = "Assets/_Project/Settings/URP/VolumeProfiles";

        [MenuItem("Afterhumans/Setup/Build Volume Profiles")]
        public static void BuildAll()
        {
            if (!Directory.Exists(ProfilesDir))
            {
                Directory.CreateDirectory(ProfilesDir);
                AssetDatabase.Refresh();
            }

            var botanika = BuildBotanikaProfile();
            var city = BuildCityProfile();
            var desert = BuildDesertProfile();

            AssetDatabase.SaveAssets();

            // Wire в SceneTheme assets
            WireToTheme("Botanika", botanika);
            WireToTheme("City", city);
            WireToTheme("Desert", desert);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VolumeProfileBuilder] 3 Volume Profiles created and wired to SceneTheme assets");
        }

        private static VolumeProfile BuildBotanikaProfile()
        {
            string path = $"{ProfilesDir}/VP_Botanika.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            // Clear existing overrides for idempotency
            for (int i = profile.components.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(profile.components[i], allowDestroyingAssets: true);
            }
            profile.components.Clear();

            // Bloom — visible sun rays + warm highlights
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.6f);
            bloom.threshold.Override(0.95f);
            bloom.scatter.Override(0.75f);
            bloom.tint.Override(new Color(1f, 0.92f, 0.78f));

            // Tonemapping ACES — cinematic highlight rolloff
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            // Color Adjustments — warm + saturated
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(12f);
            color.contrast.Override(8f);
            color.postExposure.Override(0.2f);
            color.colorFilter.Override(new Color(1f, 0.96f, 0.88f));

            // White Balance — push warm
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(15f);
            wb.tint.Override(-5f);

            // Shadows/Midtones/Highlights 3-way — cool shadows, warm highlights
            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.85f, 0.92f, 1.02f, 0f));
            smh.midtones.Override(new Vector4(1.04f, 1.02f, 0.95f, 0f));
            smh.highlights.Override(new Vector4(1.1f, 1.0f, 0.82f, 0f));

            // Vignette — subtle edge darkening
            var vign = profile.Add<Vignette>(true);
            vign.intensity.Override(0.22f);
            vign.smoothness.Override(0.5f);
            // URP 17.0.4 Vignette only has intensity + smoothness (no roundness param)

            // Film Grain — 0.15 intensity (ART_BIBLE §5)
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin2);
            grain.intensity.Override(0.18f);
            grain.response.Override(1.0f);

            // Depth of Field — cinematic 3m focus f/5.6
            var dof = profile.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(3f);
            dof.aperture.Override(5.6f);
            dof.focalLength.Override(50f);

            EditorUtility.SetDirty(profile);
            Debug.Log($"[VolumeProfileBuilder] Botanika VolumeProfile: 9 overrides");
            return profile;
        }

        private static VolumeProfile BuildCityProfile()
        {
            string path = $"{ProfilesDir}/VP_City.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            for (int i = profile.components.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(profile.components[i], true);
            profile.components.Clear();

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.3f);
            bloom.threshold.Override(1.1f);
            bloom.tint.Override(new Color(0.92f, 0.96f, 1f));

            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);

            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(-30f);
            color.contrast.Override(10f);
            color.colorFilter.Override(new Color(0.95f, 0.98f, 1.05f));

            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(-20f);
            wb.tint.Override(0f);

            // Chromatic Aberration 0.1 — glitch hint (ART_BIBLE §5.2)
            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.1f);

            // Lens distortion -0.05
            var ld = profile.Add<LensDistortion>(true);
            ld.intensity.Override(-0.05f);

            var vign = profile.Add<Vignette>(true);
            vign.intensity.Override(0.3f);
            // URP 17.0.4 Vignette only has intensity + smoothness (no roundness param)

            profile.Add<FilmGrain>(true).intensity.Override(0.12f);

            EditorUtility.SetDirty(profile);
            Debug.Log($"[VolumeProfileBuilder] City VolumeProfile: 8 overrides");
            return profile;
        }

        private static VolumeProfile BuildDesertProfile()
        {
            string path = $"{ProfilesDir}/VP_Desert.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            for (int i = profile.components.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(profile.components[i], true);
            profile.components.Clear();

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.3f);
            bloom.threshold.Override(0.8f);
            bloom.scatter.Override(0.85f);
            bloom.tint.Override(new Color(1f, 0.7f, 0.4f));

            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);

            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(22f);
            color.contrast.Override(20f);
            color.postExposure.Override(0.35f);
            color.colorFilter.Override(new Color(1.05f, 0.88f, 0.72f));

            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(30f);
            wb.tint.Override(10f);

            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.7f, 0.55f, 0.9f, 0f));  // purple shadows
            smh.midtones.Override(new Vector4(1.08f, 0.92f, 0.7f, 0f));
            smh.highlights.Override(new Vector4(1.15f, 0.85f, 0.6f, 0f));

            var vign = profile.Add<Vignette>(true);
            vign.intensity.Override(0.28f);
            // URP 17.0.4 Vignette only has intensity + smoothness

            profile.Add<FilmGrain>(true).intensity.Override(0.2f);

            // DoF — longer focus, epic
            var dof = profile.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(15f);
            dof.aperture.Override(2.8f);

            EditorUtility.SetDirty(profile);
            Debug.Log($"[VolumeProfileBuilder] Desert VolumeProfile: 8 overrides");
            return profile;
        }

        private static void WireToTheme(string themeName, VolumeProfile profile)
        {
            var themePath = $"Assets/_Project/Art/Themes/{themeName}.asset";
            var theme = AssetDatabase.LoadAssetAtPath<SceneTheme>(themePath);
            if (theme == null)
            {
                Debug.LogWarning($"[VolumeProfileBuilder] SceneTheme {themePath} not found");
                return;
            }
            theme.postFxProfile = profile;
            EditorUtility.SetDirty(theme);
            Debug.Log($"[VolumeProfileBuilder] Wired {themeName}.postFxProfile → {profile.name}");
        }
    }
}
