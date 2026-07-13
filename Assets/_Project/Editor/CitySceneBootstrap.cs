using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// E-sprint (E1.5): minimal, idempotent bootstrap for Scene_City so the Botanika→City
    /// transition (CityDoorGate + PersistentPlayer) has somewhere to land — a spawn marker
    /// for the persistent Kafka, a camera so something renders, and enough light that a
    /// batchmode screenshot isn't pitch black.
    ///
    /// Full city dressing (buildings, fountain, Anna, gates — E2's job per SPRINT_E_PLAN.md)
    /// is intentionally NOT done here. Everything below is guarded by name so E2's builder can
    /// add its own props additively without conflict, and can replace the placeholder camera/
    /// light framing once the street actually has geometry to frame.
    /// Headless: -executeMethod Afterhumans.EditorTools.CitySceneBootstrap.Ensure
    /// </summary>
    public static class CitySceneBootstrap
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_City.unity";

        [MenuItem("Afterhumans/Setup/City Scene Bootstrap")]
        public static void Ensure()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            if (GameObject.Find("SpawnPoint_FromBotanika") == null)
            {
                var marker = new GameObject("SpawnPoint_FromBotanika");
                // Edge of the street, facing further into the city (+Z) — matches the
                // orientation CityDresser's old player-spawn used for this scene.
                marker.transform.position = new Vector3(0f, 0f, -14f);
                marker.transform.rotation = Quaternion.identity;
                Debug.Log("[CitySceneBootstrap] created SpawnPoint_FromBotanika at (0,0,-14)");
            }

            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.nearClipPlane = 0.1f;
                cam.transform.position = new Vector3(0f, 1.6f, -17f);
                cam.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                camGo.AddComponent<AudioListener>();
                Debug.Log("[CitySceneBootstrap] created placeholder Main Camera at (0,1.6,-17) — E2 replaces framing once the street is dressed");
            }

            if (GameObject.Find("CityBootstrapLight") == null)
            {
                var lightGo = new GameObject("CityBootstrapLight");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(0.80f, 0.85f, 0.95f); // cool sterile — contrast to Botanika's warm
                light.intensity = 1.1f;
                lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.72f, 0.78f, 0.88f);
                RenderSettings.ambientEquatorColor = new Color(0.55f, 0.58f, 0.62f);
                RenderSettings.ambientGroundColor = new Color(0.35f, 0.35f, 0.38f);
                Debug.Log("[CitySceneBootstrap] created placeholder directional light — E2 owns the final City lighting grade");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[CitySceneBootstrap] saved Scene_City.unity");
        }
    }
}
