using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint 6: Enable ALL URP quality features that were OFF.
    /// This single script transforms rendering from mobile-minimal to desktop-AAA.
    /// Soft shadows, SSAO, HDR color grading, depth texture, light cookies.
    /// </summary>
    public static class UrpQualitySetup
    {
        private const string UrpAssetPath = "Assets/_Project/Settings/URP/Afterhumans_URP_Asset.asset";
        private const string RendererPath = "Assets/_Project/Settings/URP/Afterhumans_URP_Renderer.asset";

        [MenuItem("Afterhumans/v2/Sprint 6 — URP Quality")]
        public static void EnableDesktopQuality()
        {
            EnableUrpAssetFeatures();
            AddRendererFeatures();
            CreateLightCookie();
            Debug.Log("[UrpQualitySetup] Sprint 6 URP REVOLUTION done — all quality features enabled");
        }

        private static void EnableUrpAssetFeatures()
        {
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urpAsset == null)
            {
                Debug.LogError($"[UrpQualitySetup] URP Asset not found at {UrpAssetPath}");
                return;
            }

            var so = new SerializedObject(urpAsset);

            // Shadows
            SetBool(so, "m_SoftShadowsSupported", true);
            SetBool(so, "m_AdditionalLightShadowsSupported", true);
            SetInt(so, "m_ShadowCascadeCount", 2);
            SetInt(so, "m_MainLightShadowmapResolution", 2048);

            // Depth + Opaque textures (required for SSAO, DoF, fog)
            SetBool(so, "m_RequireDepthTexture", true);
            SetBool(so, "m_RequireOpaqueTexture", true);

            // HDR Color Grading (1 = HDR, 0 = LDR)
            SetInt(so, "m_ColorGradingMode", 1);

            // Light features
            SetBool(so, "m_SupportsLightCookies", true);
            SetBool(so, "m_ReflectionProbeBlending", true);
            SetBool(so, "m_ReflectionProbeBoxProjection", true);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssets();

            Debug.Log("[UrpQualitySetup] URP Asset: soft shadows, SSAO-ready, HDR grading, depth tex, cookies — ALL ON");
        }

        private static void AddRendererFeatures()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            if (rendererData == null)
            {
                Debug.LogError($"[UrpQualitySetup] Renderer not found at {RendererPath}");
                return;
            }

            // Check if SSAO already added
            bool hasSSAO = false;
            foreach (var f in rendererData.rendererFeatures)
            {
                if (f != null && f.GetType().Name.Contains("ScreenSpaceAmbientOcclusion"))
                    hasSSAO = true;
            }

            if (!hasSSAO)
            {
                // Add SSAO via SerializedObject
                var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
                ssao.name = "SSAO";

                var ssaoSo = new SerializedObject(ssao);
                var settings = ssaoSo.FindProperty("m_Settings");
                if (settings != null)
                {
                    var intensity = settings.FindPropertyRelative("Intensity");
                    if (intensity != null) intensity.floatValue = 2.0f;
                    var radius = settings.FindPropertyRelative("Radius");
                    if (radius != null) radius.floatValue = 0.04f;
                    var downsample = settings.FindPropertyRelative("Downsample");
                    if (downsample != null) downsample.boolValue = true;
                    ssaoSo.ApplyModifiedPropertiesWithoutUndo();
                }

                AssetDatabase.AddObjectToAsset(ssao, rendererData);

                var rendererSO = new SerializedObject(rendererData);
                var featuresProp = rendererSO.FindProperty("m_RendererFeatures");
                featuresProp.arraySize++;
                featuresProp.GetArrayElementAtIndex(featuresProp.arraySize - 1).objectReferenceValue = ssao;
                rendererSO.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(rendererData);
                AssetDatabase.SaveAssets();
                Debug.Log("[UrpQualitySetup] SSAO renderer feature added");
            }
            else
            {
                Debug.Log("[UrpQualitySetup] SSAO already present");
            }
        }

        private static void CreateLightCookie()
        {
            // Generate greenhouse window frame cookie texture
            var cookiePath = "Assets/_Project/Textures/Procedural/cookie_greenhouse.png";
            // MEDIUM-2 fix: early return with cookie application if already exists
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(cookiePath);
            if (existing != null)
            {
                ApplyCookieToSun(existing);
                Debug.Log("[UrpQualitySetup] Cookie already exists, reusing");
                return;
            }

            System.IO.Directory.CreateDirectory("Assets/_Project/Textures/Procedural");

            int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            int cellSize = 128; // 4x4 grid of window panes
            int frameWidth = 8; // frame thickness in pixels

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int gx = x % cellSize;
                    int gy = y % cellSize;
                    bool isFrame = gx < frameWidth || gy < frameWidth;

                    float alpha;
                    if (isFrame)
                    {
                        alpha = 0.1f; // frames block most light
                    }
                    else
                    {
                        // Glass panes let light through with slight variation
                        float noise = Mathf.PerlinNoise(x / 60f, y / 60f) * 0.15f;
                        alpha = 0.85f + noise;
                    }
                    tex.SetPixel(x, y, new Color(alpha, alpha, alpha, alpha));
                }
            }
            tex.Apply();

            var bytes = tex.EncodeToPNG();
            var fullPath = System.IO.Path.Combine(
                Application.dataPath.Replace("Assets", ""), cookiePath);
            System.IO.File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(cookiePath);

            // Set import as Cookie type
            var importer = AssetImporter.GetAtPath(cookiePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Cookie;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                importer.SaveAndReimport();
            }

            var reimported = AssetDatabase.LoadAssetAtPath<Texture2D>(cookiePath);
            ApplyCookieToSun(reimported);
            Debug.Log("[UrpQualitySetup] Light cookie generated and applied to sun");
        }

        private static void ApplyCookieToSun(Texture2D cookie)
        {
            if (cookie == null) return;
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.cookie = cookie;
                    l.cookieSize = 15f;
                    Debug.Log($"[UrpQualitySetup] Cookie applied to {l.name}, size=15");
                    break;
                }
            }
        }

        // HIGH-1 fix: explicit validation + error level logging
        private static int _propMissing;
        private static void SetBool(SerializedObject so, string prop, bool value)
        {
            var p = so.FindProperty(prop);
            if (p != null) { p.boolValue = value; }
            else { Debug.LogError($"[UrpQualitySetup] MISSING PROPERTY: {prop} — URP version mismatch?"); _propMissing++; }
        }

        private static void SetInt(SerializedObject so, string prop, int value)
        {
            var p = so.FindProperty(prop);
            if (p != null) { p.intValue = value; }
            else { Debug.LogError($"[UrpQualitySetup] MISSING PROPERTY: {prop} — URP version mismatch?"); _propMissing++; }
        }
    }
}
