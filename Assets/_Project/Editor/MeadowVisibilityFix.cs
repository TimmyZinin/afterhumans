using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Rescue: make the MegaKit forest actually visible at runtime.
    ///  - Wide Cinemachine orbit (8/10/7 m) + higher top rig so trees fit in frame.
    ///  - Remove the aggressive LOD cull tier on every tree (replace 2-tier with
    ///    single LOD at threshold 0.0) so distant trees never disappear.
    ///  - Move all Tree_* closer to centre: squash each tree radius to 80% of its
    ///    current distance, so the inner ring starts ~10 m from Kafka.
    ///
    /// Menu: Afterhumans → Meadow → Fix Visibility
    /// </summary>
    public static class MeadowVisibilityFix
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";

        [MenuItem("Afterhumans/Meadow/Fix Visibility")]
        public static void Apply()
        {
            Debug.Log("[VisibilityFix] Starting...");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int cam = FixCamera(scene);
            int lods = FixLODs(scene);
            int moved = MoveTreesCloser(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[VisibilityFix] DONE. camera {cam}, LODs fixed {lods}, trees moved {moved}.");
        }

        private static int FixCamera(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "CM_FreeLook_Kafka") continue;
                var fl = root.GetComponent<CinemachineFreeLook>();
                if (fl == null) return 0;

                // Wide orbit so Kafka + surrounding trees fit in frame.
                fl.m_Orbits[0] = new CinemachineFreeLook.Orbit(6.0f, 4.5f);  // top
                fl.m_Orbits[1] = new CinemachineFreeLook.Orbit(2.5f, 6.0f);  // middle (default)
                fl.m_Orbits[2] = new CinemachineFreeLook.Orbit(0.8f, 4.5f);  // bottom

                foreach (var rig in new[] { fl.GetRig(0), fl.GetRig(1), fl.GetRig(2) })
                {
                    if (rig != null)
                    {
                        var lens = rig.m_Lens;
                        lens.FieldOfView = 60f;
                        lens.FarClipPlane = 300f;
                        rig.m_Lens = lens;
                    }
                }
                return 1;
            }
            return 0;
        }

        private static int FixLODs(Scene scene)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Forest_Root") continue;
                foreach (var lg in root.GetComponentsInChildren<LODGroup>(true))
                {
                    var renderers = lg.gameObject.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) continue;
                    // Single LOD at threshold 0 — never culled.
                    lg.SetLODs(new[] { new LOD(0.0001f, renderers) });
                    lg.RecalculateBounds();
                    n++;
                }
                break;
            }
            return n;
        }

        private static int MoveTreesCloser(Scene scene)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Forest_Root") continue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("Tree_") && !t.name.StartsWith("Rocks_")
                        && !t.name.StartsWith("Bushes_") && !t.name.StartsWith("Mushrooms_")
                        && !t.name.StartsWith("Ferns_") && !t.name.StartsWith("Flowers_")
                        && !t.name.StartsWith("Grass_"))
                        continue;
                    if (t.name == "Trees" || t.name == "Rocks" || t.name == "Bushes"
                        || t.name == "Mushrooms" || t.name == "Ferns"
                        || t.name == "Flowers" || t.name == "Grass")
                        continue;

                    var pos = t.position;
                    Vector3 flat = new Vector3(pos.x, 0f, pos.z);
                    float d = flat.magnitude;
                    if (d < 2f) continue;
                    float newD = d * 0.55f;
                    Vector3 dir = flat / d;
                    t.position = dir * newD + new Vector3(0f, pos.y, 0f);
                    n++;
                }
                break;
            }
            return n;
        }
    }
}
