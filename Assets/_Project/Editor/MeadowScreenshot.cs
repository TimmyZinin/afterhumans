using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Offscreen render of Scene_MeadowForest_Greybox from a cinematic third-person
    /// angle above Kafka. Saves PNG to a stable path so the host shell can open/read it.
    ///
    /// Runs in batchmode (`-batchmode -nographics` works because we use RenderTexture
    /// offscreen rendering with a Camera — no display required on macOS).
    ///
    /// Menu: Afterhumans → Meadow → Render Screenshot
    /// CLI:
    ///   Unity -batchmode -nographics -quit -projectPath ~/afterhumans \
    ///     -executeMethod Afterhumans.EditorTools.MeadowScreenshot.Render \
    ///     -logFile /dev/stdout
    /// </summary>
    public static class MeadowScreenshot
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";
        private const string OutputPath = "/tmp/meadow_screenshot.png";
        private const int Width = 1600;
        private const int Height = 900;

        [MenuItem("Afterhumans/Meadow/Render Screenshot")]
        public static void Render()
        {
            Debug.Log("[MeadowScreenshot] Starting...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[MeadowScreenshot] Scene missing: {ScenePath}");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var kafka = GameObject.FindWithTag("Player");
            Vector3 focus = kafka != null ? kafka.transform.position : Vector3.zero;

            // Temporarily disable post-processing volumes so the capture reflects raw material colors.
            var volumes = Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None);
            var wasActive = new System.Collections.Generic.Dictionary<UnityEngine.Rendering.Volume, bool>();
            foreach (var v in volumes)
            {
                wasActive[v] = v.enabled;
                v.enabled = false;
            }

            // Tone down ambient light so material albedo is readable (restore after render).
            var savedAmbientMode = RenderSettings.ambientMode;
            var savedAmbientIntensity = RenderSettings.ambientIntensity;
            var savedAmbientLight = RenderSettings.ambientLight;
            var savedFog = RenderSettings.fog;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.42f);
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.fog = false;

            var camGO = new GameObject("__ScreenshotCam");
            try
            {
                var cam = camGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.fieldOfView = 55f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 300f;
                cam.allowHDR = false;

                var urpCam = camGO.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (urpCam != null)
                {
                    urpCam.renderPostProcessing = false;
                    urpCam.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.FastApproximateAntialiasing;
                }

                // Cinematic third-person: elevated behind, looking down at Kafka.
                // Tweak via envvar MEADOW_CAM=x,y,z for quick iteration.
                Vector3 offset = new Vector3(3f, 4.5f, -6f);
                var env = System.Environment.GetEnvironmentVariable("MEADOW_CAM");
                if (!string.IsNullOrEmpty(env))
                {
                    var parts = env.Split(',');
                    if (parts.Length == 3
                        && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x)
                        && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y)
                        && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
                        offset = new Vector3(x, y, z);
                }
                camGO.transform.position = focus + offset;
                camGO.transform.LookAt(focus + new Vector3(0f, 0.4f, 0f));

                var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                rt.antiAliasing = 4;
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                byte[] png = tex.EncodeToPNG();
                File.WriteAllBytes(OutputPath, png);
                Object.DestroyImmediate(tex);

                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);

                Debug.Log($"[MeadowScreenshot] Saved {OutputPath} ({png.Length} bytes, {Width}x{Height})");
            }
            finally
            {
                Object.DestroyImmediate(camGO);
                foreach (var kv in wasActive)
                    if (kv.Key != null) kv.Key.enabled = kv.Value;
                RenderSettings.ambientMode = savedAmbientMode;
                RenderSettings.ambientIntensity = savedAmbientIntensity;
                RenderSettings.ambientLight = savedAmbientLight;
                RenderSettings.fog = savedFog;
            }
        }
    }
}
