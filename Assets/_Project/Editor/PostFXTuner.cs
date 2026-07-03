using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint 4 / BOT-A06+: Tunes VP_Botanika.asset for stylized-realistic
    /// painterly look (Sable / Tchia / Firewatch reference).
    ///
    /// Re-applies all overrides idempotently: looks up existing component, or
    /// adds it if missing. Marks asset dirty + SaveAssets at end.
    ///
    /// Mapping vs ART_BIBLE §5:
    ///   Bloom:        intensity 1.1, threshold 1.0, scatter 0.7, warm tint
    ///   Tonemapping:  ACES (mode=2 = NeutralOrACES; ACES preset path)
    ///   ColorAdj:     postExposure 0, saturation +10, contrast +5, warm filter
    ///   SMH:          shadows cool #6B7A85 / highlights warm #F5D8A3
    ///   FilmGrain:    Thin (type=1), intensity 0.18, response 0.8
    ///   Vignette:     intensity 0.25, smoothness 0.4, roundness 0.85
    ///   DoF:          Bokeh, focusDistance 3.0, aperture 4.0, focalLength 50
    /// </summary>
    public static class PostFXTuner
    {
        private const string ProfilePath =
            "Assets/_Project/Settings/URP/VolumeProfiles/VP_Botanika.asset";

        [MenuItem("Afterhumans/Sprint4/Tune VP_Botanika PostFX")]
        public static void ApplyMenu()
        {
            Apply();
        }

        public static void Apply()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                ProfilePath);
            if (profile == null)
            {
                Debug.LogError(
                    $"[PostFXTuner] VolumeProfile not found at {ProfilePath}");
                return;
            }

            ApplyBloom(profile);
            ApplyTonemapping(profile);
            ApplyColorAdjustments(profile);
            ApplyShadowsMidtonesHighlights(profile);
            ApplyFilmGrain(profile);
            ApplyVignette(profile);
            ApplyDepthOfField(profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[PostFXTuner] VP_Botanika tuned: Bloom 1.1, ACES tonemap, " +
                "Sat+10 Con+5 warm filter, SMH cool/warm split, Grain 0.18, " +
                "Vignette 0.25, DoF Bokeh 3m f/4 50mm.");
        }

        private static T GetOrAdd<T>(VolumeProfile profile)
            where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing))
            {
                return existing;
            }
            return profile.Add<T>(true);
        }

        private static void ApplyBloom(VolumeProfile profile)
        {
            var b = GetOrAdd<Bloom>(profile);
            b.active = true;
            b.intensity.overrideState = true; b.intensity.value = 1.1f;
            b.threshold.overrideState = true; b.threshold.value = 1.0f;
            b.scatter.overrideState = true;   b.scatter.value = 0.7f;
            b.tint.overrideState = true;
            // #FFE6C8
            b.tint.value = new Color(1f, 0.902f, 0.784f, 1f);
            b.highQualityFiltering.overrideState = true;
            b.highQualityFiltering.value = false; // M1 8GB perf
        }

        private static void ApplyTonemapping(VolumeProfile profile)
        {
            var t = GetOrAdd<Tonemapping>(profile);
            t.active = true;
            t.mode.overrideState = true;
            t.mode.value = TonemappingMode.ACES;
        }

        private static void ApplyColorAdjustments(VolumeProfile profile)
        {
            var c = GetOrAdd<ColorAdjustments>(profile);
            c.active = true;
            c.postExposure.overrideState = true; c.postExposure.value = 0f;
            // Day 2 fix: saturation 10 → 0. Sat+10 на flat Kenney тинты
            // даёт disco neon (mint/cyan tint в probe Day 1). Нейтральный
            // saturation позволяет SMH split (cool shadows / warm highlights)
            // делать всю работу по color theming.
            c.saturation.overrideState = true;   c.saturation.value = 0f;
            c.contrast.overrideState = true;     c.contrast.value = 5f;
            c.colorFilter.overrideState = true;
            // #FFF0DC warm cream filter
            c.colorFilter.value = new Color(1f, 0.941f, 0.863f, 1f);
            c.hueShift.overrideState = true;     c.hueShift.value = 0f;
        }

        private static void ApplyShadowsMidtonesHighlights(
            VolumeProfile profile)
        {
            var s = GetOrAdd<ShadowsMidtonesHighlights>(profile);
            s.active = true;
            // Shadows cool #6B7A85 → normalized RGB (0.42, 0.478, 0.522)
            s.shadows.overrideState = true;
            s.shadows.value = new Vector4(0.42f, 0.478f, 0.522f, 0f);
            // Midtones default neutral.
            s.midtones.overrideState = true;
            s.midtones.value = new Vector4(1f, 1f, 1f, 0f);
            // Highlights warm #F5D8A3 → (0.961, 0.847, 0.639)
            s.highlights.overrideState = true;
            s.highlights.value = new Vector4(0.961f, 0.847f, 0.639f, 0f);
            // Default ranges.
            s.shadowsStart.overrideState = true;   s.shadowsStart.value = 0f;
            s.shadowsEnd.overrideState = true;     s.shadowsEnd.value = 0.3f;
            s.highlightsStart.overrideState = true;
            s.highlightsStart.value = 0.55f;
            s.highlightsEnd.overrideState = true;
            s.highlightsEnd.value = 1f;
        }

        private static void ApplyFilmGrain(VolumeProfile profile)
        {
            var g = GetOrAdd<FilmGrain>(profile);
            g.active = true;
            g.type.overrideState = true;
            g.type.value = FilmGrainLookup.Thin1; // Thin variant
            g.intensity.overrideState = true; g.intensity.value = 0.18f;
            g.response.overrideState = true;  g.response.value = 0.8f;
        }

        private static void ApplyVignette(VolumeProfile profile)
        {
            var v = GetOrAdd<Vignette>(profile);
            v.active = true;
            v.color.overrideState = true;
            v.color.value = Color.black;
            v.intensity.overrideState = true;  v.intensity.value = 0.25f;
            v.smoothness.overrideState = true; v.smoothness.value = 0.4f;
            // URP Vignette has no `roundness` float (HDRP-only). Use the
            // `rounded` boolean: true = perfectly circular (roundness 1.0),
            // false = aspect-ratio aware (roundness 0.0). Spec asked for
            // 0.85 → closer to circular than rectangular → use rounded=true.
            v.rounded.overrideState = true;    v.rounded.value = true;
        }

        private static void ApplyDepthOfField(VolumeProfile profile)
        {
            var d = GetOrAdd<DepthOfField>(profile);
            d.active = true;
            d.mode.overrideState = true;
            d.mode.value = DepthOfFieldMode.Bokeh;
            d.focusDistance.overrideState = true;
            d.focusDistance.value = 3.0f;
            d.aperture.overrideState = true;
            d.aperture.value = 4.0f;
            d.focalLength.overrideState = true;
            d.focalLength.value = 50f;
            d.bladeCount.overrideState = true;
            d.bladeCount.value = 5;
        }
    }
}
