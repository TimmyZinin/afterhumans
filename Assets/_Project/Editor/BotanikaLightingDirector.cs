using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint 4 / BOT-A06: Stylized-realistic painterly lighting director for
    /// Scene_Botanika. Target reference: Sable / Tchia / Firewatch — warm
    /// interior with one cool accent on the server rack corner.
    ///
    /// Day 2 fix: previous version placed 5 warm point lights at hard-coded
    /// Vec3 positions (e.g. (2.2, 2.4, 1.4)) inferred from a non-existent
    /// room layout. Those positions can land outside actual geometry.
    /// Day 2 delegates per-prop point lights to BotanikaHeroComposer (which
    /// places one warm point light next to each instantiated prop) and only
    /// keeps the global directional sun + the cool spot accent here.
    ///
    /// Spot accent: tries to find AH_HeroProps_Botanika/server_rack_retro
    /// first and parents the spot 1.5m above it. If composer hasn't run yet,
    /// falls back to a sensible default position above origin.
    ///
    /// Idempotent: clears AH_HeroLights_Botanika root before placing.
    /// </summary>
    public static class BotanikaLightingDirector
    {
        private const string HeroRootName = "AH_HeroLights_Botanika";
        private const string PropsRootName = "AH_HeroProps_Botanika";
        private const string ServerRackPropName = "server_rack_retro";

        private const string BotanikaScenePath =
            "Assets/_Project/Scenes/Scene_Botanika.unity";

        private static readonly Color WarmDirectional =
            new Color(1.0f, 0.85f, 0.6f, 1f);  // ~3200K

        private static readonly Color CoolSpot =
            new Color(1.0f, 0.95f, 0.85f, 1f); // ~5500K (only cool accent)

        // Ambient trilight — warm sky, ochre equator, brown ground.
        // Hex anchors from ART_BIBLE §3.1: #F5D8A3 / #C8A878 / #6B4F35.
        private static readonly Color AmbientSky =
            new Color(0.961f, 0.847f, 0.639f, 1f);   // #F5D8A3
        private static readonly Color AmbientEquator =
            new Color(0.784f, 0.659f, 0.471f, 1f);   // #C8A878
        private static readonly Color AmbientGround =
            new Color(0.420f, 0.310f, 0.208f, 1f);   // #6B4F35

        [MenuItem("Afterhumans/Sprint4/Apply Botanika Lighting")]
        public static void ApplyMenu()
        {
            Apply();
        }

        public static void Apply()
        {
            // Open Scene_Botanika if not already active.
            var active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid() || active.path != BotanikaScenePath)
            {
                if (EditorSceneManager.GetActiveScene().isDirty)
                {
                    EditorSceneManager.SaveOpenScenes();
                }
                EditorSceneManager.OpenScene(
                    BotanikaScenePath, OpenSceneMode.Single);
            }

            var scene = EditorSceneManager.GetActiveScene();

            // Idempotent: rebuild hero light root from scratch.
            var existingRoot = GameObject.Find(HeroRootName);
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot);
            }

            var heroRoot = new GameObject(HeroRootName);
            SceneManager.MoveGameObjectToScene(heroRoot, scene);

            // 1. Directional sun (warm 3200K, low elevation).
            var sun = SetupDirectionalLight(heroRoot.transform);

            // 2. One cool spot accent attached/aimed at server rack prop.
            var spot = SetupServerRackSpot(heroRoot.transform);

            // 3. Render settings: trilight ambient warmth.
            ApplyAmbientSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                $"[BotanikaLightingDirector] Applied: 1 directional " +
                $"({sun.name}) + 1 cool spot ({spot.name}) + trilight " +
                $"ambient. Per-prop warm points come from BotanikaHeroComposer.");
        }

        private static Light SetupDirectionalLight(Transform parent)
        {
            var go = new GameObject("HL_Sun_Directional_3200K");
            go.transform.SetParent(parent, false);
            // 25° elevation, slight side angle for soft cross-light through
            // glass roof. Y rotation -45° matches existing SceneTheme default.
            go.transform.rotation = Quaternion.Euler(25f, -45f, 0f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = WarmDirectional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            light.shadowResolution = LightShadowResolution.Medium;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
            light.lightmapBakeType = LightmapBakeType.Mixed;
            return light;
        }

        private static Light SetupServerRackSpot(Transform parent)
        {
            var go = new GameObject("HL_ServerRack_Cool_5500K");
            go.transform.SetParent(parent, false);

            // Try to anchor above server_rack_retro placed by HeroComposer.
            Vector3 spotPos = new Vector3(0f, 2.2f, -2f); // fallback
            Vector3 spotLookAt = new Vector3(0f, 0.85f, -2f);

            var propsRoot = GameObject.Find(PropsRootName);
            if (propsRoot != null)
            {
                var rack = FindChildByName(propsRoot.transform, ServerRackPropName);
                if (rack != null)
                {
                    spotPos = rack.position + new Vector3(0f, 1.5f, 0f);
                    spotLookAt = rack.position;
                    Debug.Log(
                        $"[BotanikaLightingDirector] Spot anchored above " +
                        $"server_rack_retro @ {rack.position}");
                }
            }

            go.transform.position = spotPos;
            go.transform.rotation = Quaternion.LookRotation(
                (spotLookAt - spotPos).normalized, Vector3.up);

            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = CoolSpot;
            light.intensity = 0.8f;
            light.range = 2.5f;
            light.spotAngle = 45f;
            light.innerSpotAngle = 30f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.5f;
            light.shadowResolution = LightShadowResolution.Low;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            return light;
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChildByName(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void ApplyAmbientSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            RenderSettings.ambientIntensity = 0.4f;
        }
    }
}
