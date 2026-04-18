using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Iterate the meadow toward the Quaternius reference (golden-hour painterly forest):
    ///  - Scale Kafka by 3× so she reads as the focal subject.
    ///  - Directional light: warm amber golden-hour, low angle, soft shadows.
    ///  - Ground plane: tiled Grass.png texture from MegaKit (not flat colour).
    ///  - Volume profile: stronger warm ColorAdjustments, stronger Bloom, Vignette.
    ///  - Fog: warm pastel, linear 15→90m for atmospheric depth.
    ///
    /// Menu: Afterhumans → Meadow → Match Reference
    /// </summary>
    public static class MeadowReferenceMatch
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";
        private const string VolumeProfilePath = "Assets/_Project/Settings/URP/VolumeProfiles/VP_MeadowForest.asset";
        private const string GroundMaterialPath = "Assets/_Project/Materials/Nature/Mat_Meadow_Ground_Tiled.mat";
        private const string GrassTexturePath = "Assets/ThirdParty/StylizedNature/Textures/Grass.png";

        [MenuItem("Afterhumans/Meadow/Match Reference")]
        public static void Apply()
        {
            Debug.Log("[RefMatch] Starting...");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int kafka = ScaleKafka(scene, 3.0f);
            int light = TuneDirectionalLight(scene);
            int ground = RetextureGround(scene);
            int vp = TuneVolumeProfile();
            int fog = TuneFog();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RefMatch] DONE. Kafka {kafka}, light {light}, ground {ground}, volume {vp}, fog {fog}.");
        }

        private static int ScaleKafka(Scene scene, float scale)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Kafka") continue;
                // Scale the FBX child (so CharacterController size is unchanged, which
                // would otherwise make Kafka fly). But we DO want her visual larger.
                // Simpler: scale the root (Unity's CC will resize proportionally via
                // height/radius override below).
                root.transform.localScale = Vector3.one * scale;
                var cc = root.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.height = 0.6f;           // keep CC height logical, scale applies world
                    cc.radius = 0.25f;
                    cc.center = new Vector3(0f, 0.3f, 0f);
                }
                return 1;
            }
            return 0;
        }

        private static int TuneDirectionalLight(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Directional Light") continue;
                var light = root.GetComponent<Light>();
                if (light == null) continue;
                light.color = new Color(1.0f, 0.82f, 0.55f); // amber golden-hour
                light.intensity = 1.6f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.65f;
                light.shadowBias = 0.02f;
                light.shadowNormalBias = 0.3f;
                root.transform.rotation = Quaternion.Euler(22f, -50f, 0f); // low sun from side
                return 1;
            }
            return 0;
        }

        private static int RetextureGround(Scene scene)
        {
            var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
            if (grassTex == null)
            {
                Debug.LogWarning($"[RefMatch] Grass texture not found at {GrassTexturePath}");
                return 0;
            }

            var urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (mat == null)
            {
                mat = new Material(urpLit) { name = Path.GetFileNameWithoutExtension(GroundMaterialPath) };
                AssetDatabase.CreateAsset(mat, GroundMaterialPath);
            }
            if (mat.shader != urpLit) mat.shader = urpLit;
            // Grass.png is a vertical blade texture, tiling it on the ground creates
            // stripes. Use solid warm meadow colour instead and rely on scattered
            // Grass FBX props for ground-level variation.
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
            Color meadow = new Color(0.54f, 0.66f, 0.36f); // warm yellow-green, tuned to reference
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", meadow);
            mat.color = meadow;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Ground") continue;
                var r = root.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
                return 1;
            }
            return 0;
        }

        private static int TuneVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null) return 0;

            if (!profile.TryGet(out UnityEngine.Rendering.Universal.Bloom bloom))
                bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.active = true;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.9f;
            bloom.threshold.overrideState = true; bloom.threshold.value = 0.85f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.75f;
            bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.88f, 0.72f);

            if (!profile.TryGet(out UnityEngine.Rendering.Universal.Vignette v))
                v = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            v.active = true;
            v.intensity.overrideState = true; v.intensity.value = 0.32f;
            v.smoothness.overrideState = true; v.smoothness.value = 0.55f;
            v.color.overrideState = true; v.color.value = new Color(0.12f, 0.08f, 0.05f);

            if (!profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments c))
                c = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
            c.active = true;
            c.postExposure.overrideState = true; c.postExposure.value = 0.25f;
            c.contrast.overrideState = true; c.contrast.value = 18f;
            c.saturation.overrideState = true; c.saturation.value = 25f;
            c.colorFilter.overrideState = true; c.colorFilter.value = new Color(1.05f, 0.95f, 0.78f);

            if (!profile.TryGet(out UnityEngine.Rendering.Universal.WhiteBalance wb))
                wb = profile.Add<UnityEngine.Rendering.Universal.WhiteBalance>(true);
            wb.active = true;
            wb.temperature.overrideState = true; wb.temperature.value = 15f;
            wb.tint.overrideState = true; wb.tint.value = 5f;

            EditorUtility.SetDirty(profile);
            return 1;
        }

        private static int TuneFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.93f, 0.86f, 0.68f); // warm pastel cream
            RenderSettings.fogStartDistance = 15f;
            RenderSettings.fogEndDistance = 90f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.95f, 0.90f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.82f, 0.74f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.45f, 0.25f);
            RenderSettings.ambientIntensity = 0.9f;
            return 1;
        }
    }
}
