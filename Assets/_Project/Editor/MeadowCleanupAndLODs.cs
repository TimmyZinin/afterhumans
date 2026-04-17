using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Cleanup leftover primitive-era polish roots that linger alongside the new
    /// MegaKit forest, and attach LODGroup + simple colliders to the MegaKit
    /// FBX tree instances so /3d-games rubric (LOD + simple physics) passes.
    ///
    /// Menu: Afterhumans → Meadow → Cleanup and LOD MegaKit
    /// </summary>
    public static class MeadowCleanupAndLODs
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";

        [MenuItem("Afterhumans/Meadow/Cleanup and LOD MegaKit")]
        public static void Apply()
        {
            Debug.Log("[Cleanup] Starting...");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int wiped = WipeStale(scene);
            int lods = AddLODsToMegaKitTrees(scene);
            int colliders = AddCapsuleCollidersToMegaKitTrees(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Cleanup] DONE. Wiped {wiped} stale roots, LODGroups {lods}, colliders {colliders}.");
        }

        private static int WipeStale(Scene scene)
        {
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Meadow_StylizedPolish_Root"
                 || root.name == "Meadow_PolishV2_Root"
                 || root.name == "Meadow_Greybox_Root") // ensure only MegaKit remains
                {
                    Object.DestroyImmediate(root);
                    n++;
                }
            }
            return n;
        }

        private static int AddLODsToMegaKitTrees(Scene scene)
        {
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Forest_Root") continue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("Tree_")) continue;
                    var go = t.gameObject;
                    if (go.GetComponent<LODGroup>() != null) continue;
                    var renderers = go.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) continue;
                    var lod = go.AddComponent<LODGroup>();
                    var lods = new LOD[]
                    {
                        new LOD(0.35f, renderers),
                        new LOD(0.05f, new Renderer[0]), // cull at distance
                    };
                    lod.SetLODs(lods);
                    lod.RecalculateBounds();
                    count++;
                }
                break;
            }
            return count;
        }

        private static int AddCapsuleCollidersToMegaKitTrees(Scene scene)
        {
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Forest_Root") continue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("Tree_")) continue;
                    var go = t.gameObject;
                    if (go.GetComponent<Collider>() != null) continue;
                    var renderers = go.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) continue;

                    // Remove any MeshCollider that FBX import added (expensive per /3d-games).
                    foreach (var mc in go.GetComponentsInChildren<MeshCollider>(true))
                        Object.DestroyImmediate(mc);

                    // Single CapsuleCollider roughly matching the trunk (Y axis).
                    var cap = go.AddComponent<CapsuleCollider>();
                    cap.direction = 1; // Y
                    Bounds bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                    cap.height = Mathf.Max(bounds.size.y, 0.8f);
                    cap.radius = Mathf.Max(0.2f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.25f);
                    cap.center = go.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y + cap.height * 0.5f, bounds.center.z));
                    count++;
                }
                break;
            }
            return count;
        }
    }
}
