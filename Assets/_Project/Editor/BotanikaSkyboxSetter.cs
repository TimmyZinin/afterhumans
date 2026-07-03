using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint 4 final: replace Sprint1/3 default Skybox with warm sunset
    /// HDRI without re-running BotanikaBuilder (which sometimes hits Unity
    /// licensing transient errors in batchmode).
    /// </summary>
    public static class BotanikaSkyboxSetter
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Scene_Botanika.unity";

        [MenuItem("Afterhumans/Sprint4/Set Warm Skybox")]
        public static void Apply()
        {
            string[] candidates =
            {
                "Assets/_Project/Vendor/PolyHaven/HDRI/sunset_botanika_4k.exr",
                "Assets/_Project/Vendor/PolyHaven/rogland_sunset_2k.hdr",
            };
            Texture hdri = null;
            string used = null;
            foreach (var p in candidates)
            {
                hdri = AssetDatabase.LoadAssetAtPath<Texture>(p);
                if (hdri != null) { used = p; break; }
            }
            if (hdri == null)
            {
                Debug.LogError("[SkyboxSetter] No HDRI found at any candidate.");
                return;
            }

            var skyShader = Shader.Find("Skybox/Panoramic");
            if (skyShader == null)
            {
                Debug.LogError("[SkyboxSetter] Skybox/Panoramic shader missing.");
                return;
            }

            var skyMat = new Material(skyShader);
            skyMat.SetTexture("_MainTex", hdri);
            skyMat.SetFloat("_Exposure", 1.0f);
            skyMat.SetFloat("_Rotation", 30f);
            skyMat.SetInt("_Mapping", 1);              // Latitude-Longitude
            skyMat.SetInt("_ImageType", 0);            // 360 degrees
            skyMat.SetInt("_MirrorOnBack", 0);
            skyMat.SetInt("_Layout", 0);

            string matPath =
                "Assets/_Project/Settings/SkyboxBotanikaWarm.mat";
            AssetDatabase.CreateAsset(skyMat, matPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Apply to scene
            var active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid() || active.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            RenderSettings.skybox = skyMat;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;
            DynamicGI.UpdateEnvironment();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[SkyboxSetter] Skybox set to {used}");
        }
    }
}
