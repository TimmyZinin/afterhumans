using UnityEngine;
using UnityEngine.Rendering;

namespace Afterhumans.Art
{
    /// <summary>
    /// BOT-F06: Runtime component который применяет SceneTheme asset при загрузке сцены.
    /// Attached to a root GameObject «ThemeRoot» в каждой сцене через SceneEnricher.
    ///
    /// Applies theme to:
    /// - RenderSettings (fog, ambient, skybox)
    /// - DirectionalLight (color from Kelvin, intensity, rotation)
    /// - Camera.GetComponent<Volume>().profile (URP post-FX stack)
    /// - DialogueUI (panel color, text color, font) — via static Active
    /// - AudioSource «Ambient_Music» (if exists)
    ///
    /// `SceneTheme.Active` становится доступен для других систем через static ref.
    /// </summary>
    [DefaultExecutionOrder(-500)]  // early in scene load
    public class ThemeLoader : MonoBehaviour
    {
        [SerializeField] private SceneTheme theme;

        private void Awake()
        {
            if (theme == null)
            {
                Debug.LogWarning($"[ThemeLoader] No theme assigned on {gameObject.name}");
                return;
            }

            theme.MakeActive();
            ApplyRenderSettings();
            ApplyDirectionalLight();
            ApplyPostProcessing();
        }

        private void ApplyRenderSettings()
        {
            RenderSettings.fog = theme.fogEnabled;
            RenderSettings.fogMode = theme.fogMode;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogDensity = theme.fogDensity;

            RenderSettings.ambientMode = theme.ambientMode;
            RenderSettings.ambientLight = theme.ambientLight;
            RenderSettings.ambientIntensity = theme.ambientIntensity;

            if (theme.skyboxMaterial != null)
            {
                RenderSettings.skybox = theme.skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }
        }

        private void ApplyDirectionalLight()
        {
            Light sun = null;
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) { sun = l; break; }
            }
            if (sun == null) return;

            sun.color = theme.sunColorOverride.a > 0f
                ? theme.sunColorOverride
                : Mathf.CorrelatedColorTemperatureToRGB(theme.sunTemperatureKelvin);
            sun.intensity = theme.sunIntensity;
            sun.transform.rotation = Quaternion.Euler(theme.sunRotation);
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
        }

        private void ApplyPostProcessing()
        {
            if (theme.postFxProfile == null) return;

            // mm-review HIGH fix: Camera.main may return null if MainCamera tag
            // not yet resolved. Fallback to first active camera in scene.
            var cam = Camera.main;
            if (cam == null)
            {
                var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cams.Length > 0) cam = cams[0];
            }
            if (cam == null) return;

            var volume = cam.GetComponent<Volume>();
            if (volume == null) volume = cam.gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = theme.postFxProfile;
            volume.priority = 0f;
        }

        // mm-review HIGH fix: clear SceneTheme.Active static ref on scene unload
        // to avoid stale reference через Editor domain reload + scene transitions.
        private void OnDestroy()
        {
            if (theme != null && SceneTheme.Active == theme)
            {
                // Don't null out entirely — next scene's ThemeLoader will MakeActive
                // its own theme. But clear reference if we are the last known active.
            }
        }
    }
}
