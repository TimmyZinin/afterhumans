using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// One-time project bootstrap: creates 5 empty scenes for Episode 0
    /// and registers them in EditorBuildSettings so BuildScript can package them.
    ///
    /// Call via Unity CLI batchmode:
    ///   Unity -batchmode -nographics -quit \
    ///     -projectPath ~/afterhumans \
    ///     -executeMethod Afterhumans.EditorTools.ProjectSetup.CreateInitialScenes \
    ///     -logFile /dev/stdout
    /// </summary>
    public static class ProjectSetup
    {
        private static readonly (string name, bool withFpsRig)[] Scenes = new[]
        {
            ("Scene_MainMenu", false),
            ("Scene_Botanika", true),
            ("Scene_City", true),
            ("Scene_Desert", true),
            ("Scene_Credits", false),
        };

        private const string ScenesDir = "Assets/_Project/Scenes";

        /// <summary>
        /// Reorder BuildSettings so Scene_Botanika is loaded first when .app starts.
        /// Used for walking skeleton testing — MainMenu scene is empty without UI yet.
        /// </summary>
        [MenuItem("Afterhumans/Setup/Set Botanika First For Testing")]
        public static void SetBotanikaFirstForTesting()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var botanika = scenes.Find(s => s.path.Contains("Scene_Botanika"));
            if (botanika == null)
            {
                Debug.LogError("[ProjectSetup] Scene_Botanika not found in build settings");
                return;
            }
            scenes.Remove(botanika);
            scenes.Insert(0, botanika);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();

            Debug.Log("[ProjectSetup] Scene order updated. New order:");
            foreach (var s in EditorBuildSettings.scenes)
            {
                Debug.Log($"  - {s.path}");
            }
        }

        [MenuItem("Afterhumans/Setup/Create Initial Scenes")]
        public static void CreateInitialScenes()
        {
            Debug.Log("[ProjectSetup] Starting scene creation...");

            if (!Directory.Exists(ScenesDir))
            {
                Directory.CreateDirectory(ScenesDir);
                AssetDatabase.Refresh();
            }

            var buildScenes = new List<EditorBuildSettingsScene>();

            foreach (var (name, withFpsRig) in Scenes)
            {
                string path = $"{ScenesDir}/{name}.unity";

                if (File.Exists(path))
                {
                    Debug.Log($"[ProjectSetup] Scene exists: {name}");
                }
                else
                {
                    Debug.Log($"[ProjectSetup] Creating scene: {name}");
                    var scene = EditorSceneManager.NewScene(
                        NewSceneSetup.DefaultGameObjects,
                        NewSceneMode.Single);

                    SetupSceneContent(scene, name, withFpsRig);

                    bool saved = EditorSceneManager.SaveScene(scene, path);
                    if (!saved)
                    {
                        Debug.LogError($"[ProjectSetup] Failed to save scene: {name}");
                        continue;
                    }
                }

                buildScenes.Add(new EditorBuildSettingsScene(path, enabled: true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ProjectSetup] Done. {buildScenes.Count} scenes registered in Build Settings.");
            foreach (var s in EditorBuildSettings.scenes)
            {
                Debug.Log($"  - {s.path} (enabled: {s.enabled})");
            }
        }

        private static void SetupSceneContent(Scene scene, string sceneName, bool withFpsRig)
        {
            // NewSceneSetup.DefaultGameObjects already placed Main Camera and Directional Light.
            // We enhance based on scene type.

            var allRoots = scene.GetRootGameObjects();
            GameObject mainCamera = null;
            GameObject dirLight = null;
            foreach (var root in allRoots)
            {
                if (root.name == "Main Camera") mainCamera = root;
                if (root.name == "Directional Light") dirLight = root;
            }

            // Apply scene-specific lighting presets based on Art Bible
            if (dirLight != null)
            {
                var light = dirLight.GetComponent<Light>();
                if (light != null)
                {
                    switch (sceneName)
                    {
                        case "Scene_Botanika":
                            // Warm 3200K late golden hour
                            light.color = new Color(1f, 0.86f, 0.67f); // ~3200K
                            light.intensity = 1.2f;
                            dirLight.transform.rotation = Quaternion.Euler(25f, -45f, 0f);
                            break;
                        case "Scene_City":
                            // Cool 7500K clinical daylight
                            light.color = new Color(0.86f, 0.92f, 1.0f); // ~7500K
                            light.intensity = 0.8f;
                            dirLight.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
                            break;
                        case "Scene_Desert":
                            // Very warm 2400K sunset
                            light.color = new Color(1.0f, 0.55f, 0.31f); // ~2400K
                            light.intensity = 2.5f;
                            dirLight.transform.rotation = Quaternion.Euler(5f, 170f, 0f);
                            break;
                        case "Scene_MainMenu":
                        case "Scene_Credits":
                            // Minimal ambient lighting (UI scenes)
                            light.intensity = 0.6f;
                            break;
                    }
                }
            }

            // Position the camera at a reasonable starting pose
            if (mainCamera != null)
            {
                if (withFpsRig)
                {
                    mainCamera.transform.position = new Vector3(0f, 1.65f, 0f);
                    mainCamera.transform.rotation = Quaternion.identity;
                    var cam = mainCamera.GetComponent<Camera>();
                    if (cam != null)
                    {
                        cam.fieldOfView = 65f;
                        cam.nearClipPlane = 0.1f;
                        cam.farClipPlane = 1000f;
                    }
                    // Tag as MainCamera — default already, but be sure
                    mainCamera.tag = "MainCamera";
                }
                else
                {
                    // UI scenes: camera slightly back, orthographic for flat UI
                    mainCamera.transform.position = new Vector3(0f, 0f, -10f);
                }
            }

            // Add a placeholder ground plane for FPS scenes so player has something to stand on
            if (withFpsRig)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Placeholder_Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(5f, 1f, 5f);

                // Scene-tinted placeholder material color
                var renderer = ground.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    Material mat = new Material(renderer.sharedMaterial);
                    switch (sceneName)
                    {
                        case "Scene_Botanika":
                            mat.color = new Color(0.45f, 0.38f, 0.25f); // warm earth
                            break;
                        case "Scene_City":
                            mat.color = new Color(0.91f, 0.93f, 0.95f); // cool white
                            break;
                        case "Scene_Desert":
                            mat.color = new Color(0.79f, 0.58f, 0.37f); // ochre sand
                            break;
                    }
                    renderer.sharedMaterial = mat;
                }

                // Placeholder spawn marker (empty GameObject)
                var spawn = new GameObject("Player_SpawnPoint");
                spawn.transform.position = new Vector3(0f, 1.5f, 0f);
            }
        }
    }
}
