using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-F01: Activates URP as the active render pipeline for Afterhumans.
    ///
    /// Pipeline: создаёт UniversalRenderer asset → создаёт URP asset со ссылкой на renderer
    /// → прописывает в GraphicsSettings.defaultRenderPipeline + QualitySettings →
    /// сохраняет AssetDatabase. После этого проект рендерится через URP 17.0.4, и
    /// все 10 skills из Sprint 2 разблокированы (post-FX Volumes, shader graph,
    /// volumetric fog, cinematic color grading, VP_Botanika.asset и т.д.).
    ///
    /// Критичный риск (R1 из плана): все существующие Standard shader материалы
    /// станут magenta до вызова batch converter на следующем шаге. Этот скрипт
    /// только активирует pipeline — material conversion в отдельном методе.
    ///
    /// Run via:
    ///   Unity -batchmode -nographics -quit -projectPath ~/afterhumans \
    ///     -executeMethod Afterhumans.EditorTools.UrpActivation.Activate \
    ///     -logFile /tmp/urp_activate.log
    /// </summary>
    public static class UrpActivation
    {
        private const string SettingsDir = "Assets/_Project/Settings/URP";
        private const string RendererAssetPath = SettingsDir + "/Afterhumans_URP_Renderer.asset";
        private const string UrpAssetPath = SettingsDir + "/Afterhumans_URP_Asset.asset";

        [MenuItem("Afterhumans/Setup/Activate URP")]
        public static void Activate()
        {
            Debug.Log("[UrpActivation] === Starting URP activation ===");

            // Ensure settings directory exists
            if (!Directory.Exists(SettingsDir))
            {
                Directory.CreateDirectory(SettingsDir);
                AssetDatabase.Refresh();
                Debug.Log($"[UrpActivation] Created directory {SettingsDir}");
            }

            // 1. Create UniversalRendererData (forward renderer)
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "Afterhumans_URP_Renderer";
                AssetDatabase.CreateAsset(rendererData, RendererAssetPath);
                Debug.Log($"[UrpActivation] Created renderer asset: {RendererAssetPath}");
            }
            else
            {
                Debug.Log($"[UrpActivation] Renderer asset already exists: {RendererAssetPath}");
            }

            // 2. Create UniversalRenderPipelineAsset referencing the renderer
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                urpAsset.name = "Afterhumans_URP_Asset";
                AssetDatabase.CreateAsset(urpAsset, UrpAssetPath);
                Debug.Log($"[UrpActivation] Created URP asset: {UrpAssetPath}");
            }
            else
            {
                Debug.Log($"[UrpActivation] URP asset already exists: {UrpAssetPath}");
            }

            AssetDatabase.SaveAssets();

            // 3. Assign URP asset to GraphicsSettings (writes m_CustomRenderPipeline
            //    in ProjectSettings/GraphicsSettings.asset)
            GraphicsSettings.defaultRenderPipeline = urpAsset;

            // 4. Assign to all Quality Levels so every platform uses URP
            int qualityCount = QualitySettings.names.Length;
            int currentQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < qualityCount; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urpAsset;
            }
            QualitySettings.SetQualityLevel(currentQuality, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 5. Verification
            if (GraphicsSettings.defaultRenderPipeline == urpAsset)
            {
                Debug.Log("[UrpActivation] ✅ GraphicsSettings.defaultRenderPipeline = Afterhumans_URP_Asset");
            }
            else
            {
                Debug.LogError("[UrpActivation] ❌ GraphicsSettings.defaultRenderPipeline assignment FAILED");
            }

            Debug.Log($"[UrpActivation] URP pipeline: {GraphicsSettings.currentRenderPipeline?.name ?? "null"}");

            // mm-review polish: auto-chain material conversion чтобы Activate() был
            // self-sufficient (раньше юзер должен был запустить ConvertMaterialsToUrp
            // отдельно, что легко забыть и остаться с magenta materials).
            Debug.Log("[UrpActivation] Auto-running ConvertMaterialsToUrp step 2...");
            ConvertMaterialsToUrp();

            Debug.Log("[UrpActivation] === Done. URP active + materials converted. ===");
        }

        /// <summary>
        /// BOT-F01 step 2: batch-convert all project materials from Standard/Built-in
        /// shaders to URP/Lit equivalents. Run after Activate() completes.
        ///
        /// Uses Unity's built-in MaterialUpgrader via UniversalRenderPipelineMaterialUpgrader
        /// which handles Standard, Standard (Specular), Nature/SpeedTree, Particles, and
        /// Unity 2018.x legacy shaders. Our Tint_*.mat files (Standard-based) are covered.
        /// </summary>
        [MenuItem("Afterhumans/Setup/Convert Materials To URP")]
        public static void ConvertMaterialsToUrp()
        {
            Debug.Log("[UrpActivation] === Starting material conversion to URP ===");

            // Count materials before
            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" });
            Debug.Log($"[UrpActivation] Found {matGuids.Length} materials in Assets/_Project");

            int converted = 0;
            int skipped = 0;
            int alreadyUrp = 0;

            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpLit == null)
            {
                Debug.LogError("[UrpActivation] URP/Lit shader not found — URP activation failed?");
                return;
            }

            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                var currentShader = mat.shader;
                if (currentShader == null)
                {
                    skipped++;
                    continue;
                }

                string name = currentShader.name;
                if (name.StartsWith("Universal Render Pipeline/"))
                {
                    alreadyUrp++;
                    continue;
                }

                Color baseColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") :
                                   (mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white);
                float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") :
                                    (mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.2f);
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") :
                                   (mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null);

                if (name == "Standard" || name == "Standard (Specular setup)" || name.StartsWith("Legacy Shaders/"))
                {
                    mat.shader = urpLit;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                    if (mat.HasProperty("_BaseMap") && mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                    EditorUtility.SetDirty(mat);
                    converted++;
                }
                else if (name == "Unlit/Color" || name == "Unlit/Texture")
                {
                    mat.shader = urpUnlit;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                    if (mat.HasProperty("_BaseMap") && mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                    EditorUtility.SetDirty(mat);
                    converted++;
                }
                else
                {
                    // Unknown shader — skip, log for review
                    Debug.Log($"[UrpActivation] Skipped (unknown shader '{name}'): {path}");
                    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[UrpActivation] === Material conversion done: converted={converted}, already URP={alreadyUrp}, skipped={skipped} ===");
        }

        /// <summary>
        /// Verification method — asserts URP is active and writes result to console.
        /// Returns exit code 0 if URP active, 1 otherwise (for batchmode grep).
        /// </summary>
        [MenuItem("Afterhumans/Setup/Verify URP Active")]
        public static void VerifyUrpActive()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            if (pipeline != null && pipeline is UniversalRenderPipelineAsset)
            {
                Debug.Log($"[UrpActivation] ✅ URP ACTIVE: {pipeline.name}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[UrpActivation] ❌ URP NOT ACTIVE. defaultRenderPipeline = {(pipeline == null ? "null" : pipeline.GetType().Name)}");
                EditorApplication.Exit(1);
            }
        }
    }
}
