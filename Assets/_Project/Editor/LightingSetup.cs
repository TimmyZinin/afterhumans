using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Applies an Art Bible lighting preset to the currently open scene:
    /// fog color + distance, ambient light, sun direction + color, skybox tint.
    ///
    /// Three presets match the narrative beats:
    /// - Botanika: warm 3200K golden hour, tight sun rays through glass roof vibe
    /// - City: cool 7500K clinical overcast, high ambient, low contrast
    /// - Desert: 2400K sunset, orange fog horizon, long hard shadows
    /// </summary>
    public static class LightingSetup
    {
        public enum Preset { Botanika, City, Desert }

        public static void Apply(Preset preset)
        {
            // Base render settings — start from known-good state every run
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

            switch (preset)
            {
                case Preset.Botanika:
                    ApplyBotanika();
                    break;
                case Preset.City:
                    ApplyCity();
                    break;
                case Preset.Desert:
                    ApplyDesert();
                    break;
            }

            // Find the main directional light in the scene and tune it to match
            var sun = FindSceneDirectionalLight();
            if (sun != null)
            {
                ApplySunForPreset(sun, preset);
                RenderSettings.sun = sun;
            }

            // Open-world presets (City/Desert) set camera to SolidColor with fog tint so the
            // horizon blends into the fog. Botanika is an interior and keeps Skybox so the
            // windows show sky beyond the walls.
            var playerCam = FindPlayerCamera();
            if (playerCam != null)
            {
                if (preset == Preset.Botanika)
                {
                    playerCam.clearFlags = CameraClearFlags.Skybox;
                }
                else
                {
                    playerCam.clearFlags = CameraClearFlags.SolidColor;
                    playerCam.backgroundColor = RenderSettings.fogColor;
                }
            }

            Debug.Log($"[LightingSetup] Applied {preset} preset. fogColor={RenderSettings.fogColor}, density={RenderSettings.fogDensity}");
        }

        private static Camera FindPlayerCamera()
        {
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in cams)
            {
                if (c.gameObject.name == "PlayerCamera" || c.CompareTag("MainCamera")) return c;
            }
            return cams.Length > 0 ? cams[0] : null;
        }

        private static void ApplyBotanika()
        {
            // Interior greenhouse — warm golden hour, LOW ambient for shadow contrast
            RenderSettings.fogColor = new Color(0.95f, 0.80f, 0.58f, 1f);
            RenderSettings.fogDensity = 0.015f;  // Art Bible: 0.015 (visible haze)
            RenderSettings.ambientLight = new Color(0.55f, 0.45f, 0.32f);  // warm but dim
            RenderSettings.ambientIntensity = 0.5f;  // low — let sun create contrast
        }

        private static void ApplyCity()
        {
            // Cool clinical Homo Deus
            RenderSettings.fogColor = new Color(0.82f, 0.88f, 0.95f, 1f);
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.ambientLight = new Color(0.50f, 0.56f, 0.66f);  // cool blue-grey ambient
            RenderSettings.ambientIntensity = 1.1f;
        }

        private static void ApplyDesert()
        {
            // Sunset Dune wasteland
            RenderSettings.fogColor = new Color(0.98f, 0.55f, 0.25f, 1f);
            RenderSettings.fogDensity = 0.040f;
            RenderSettings.ambientLight = new Color(0.72f, 0.42f, 0.22f);  // deep orange ambient
            RenderSettings.ambientIntensity = 0.9f;
        }

        private static Light FindSceneDirectionalLight()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) return l;
            }
            return null;
        }

        private static void ApplySunForPreset(Light sun, Preset preset)
        {
            switch (preset)
            {
                case Preset.Botanika:
                    sun.color = new Color(1.0f, 0.86f, 0.67f);  // 3200K
                    sun.intensity = 1.5f;  // Art Bible: 1.2-1.5
                    sun.transform.rotation = Quaternion.Euler(25f, -45f, 0f);  // Art Bible: 25° above horizon
                    sun.shadows = LightShadows.Soft;
                    break;
                case Preset.City:
                    sun.color = new Color(0.86f, 0.92f, 1.0f);  // 7500K
                    sun.intensity = 0.9f;
                    sun.transform.rotation = Quaternion.Euler(60f, 20f, 0f);
                    sun.shadows = LightShadows.Soft;
                    break;
                case Preset.Desert:
                    sun.color = new Color(1.0f, 0.55f, 0.22f);  // 2400K
                    sun.intensity = 2.8f;
                    sun.transform.rotation = Quaternion.Euler(8f, 170f, 0f);  // low horizon sun
                    sun.shadows = LightShadows.Soft;
                    break;
            }
        }
    }
}
