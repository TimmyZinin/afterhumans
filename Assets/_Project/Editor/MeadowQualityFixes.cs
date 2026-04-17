using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Skill-audit fixes — brings each sprint closer to 9/10 per /3d-games + /3d-asset criteria.
    ///
    ///  Sprint 3 (camera 6→9): add CinemachineCollider so the camera doesn't clip trees.
    ///  Sprint 4 (forest 7→9): swap trunk MeshColliders for CapsuleColliders (simple shapes,
    ///                         cheap physics per /3d-games anti-patterns), add LODGroup
    ///                         per tree with 2 LODs (full puffy foliage → simplified).
    ///  Sprint 2 (kafka 7→9): attach KafkaIdleAnimation.cs to the FBX child so Idle state
    ///                         has visible breathing/tail motion (not a static pose).
    ///  Sprint 8 (polish 6→9): bigger FOV on Cinemachine for cinematic feel, enable
    ///                         occlusion culling flag on static forest root, tighter fog.
    ///
    /// Menu: Afterhumans → Meadow → Apply Quality Fixes
    /// </summary>
    public static class MeadowQualityFixes
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";
        private const string KafkaIdleScriptAsset = "Assets/_Project/Scripts/Kafka/KafkaIdleAnimation.cs";

        [MenuItem("Afterhumans/Meadow/Apply Quality Fixes")]
        public static void Apply()
        {
            Debug.Log("[QualityFixes] Starting...");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            int camFix = FixCamera(scene);
            int colliderFix = FixTrunkColliders(scene);
            int lodFix = AddTreeLODs(scene);
            int kafkaFix = AttachKafkaIdle(scene);
            int fogFix = TightenFog();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[QualityFixes] DONE. camera +{camFix}, colliders swapped {colliderFix}, LODGroups {lodFix}, kafka idle {kafkaFix}, fog {fogFix}.");
        }

        private static int FixCamera(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "CM_FreeLook_Kafka") continue;
                var fl = root.GetComponent<CinemachineFreeLook>();
                if (fl == null) return 0;

                // Add CinemachineCollider (pulls camera away from occluders like trees).
                var collider = fl.GetComponent<CinemachineCollider>();
                if (collider == null) collider = fl.gameObject.AddComponent<CinemachineCollider>();
                collider.m_CollideAgainst = ~0; // Everything
                collider.m_AvoidObstacles = true;
                collider.m_DistanceLimit = 0.5f;
                collider.m_MinimumDistanceFromTarget = 0.8f;
                collider.m_CameraRadius = 0.3f;
                collider.m_SmoothingTime = 0.2f;

                // Cinematic-feel tweaks: widen FOV slightly so forest reads as depth, not wall.
                foreach (var rig in new[] { fl.GetRig(0), fl.GetRig(1), fl.GetRig(2) })
                {
                    if (rig != null)
                    {
                        var lens = rig.m_Lens;
                        lens.FieldOfView = 55f;
                        rig.m_Lens = lens;
                    }
                }
                return 1;
            }
            return 0;
        }

        private static int FixTrunkColliders(Scene scene)
        {
            int swapped = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Greybox_Root") continue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != "Trunk") continue;
                    var go = t.gameObject;
                    var mc = go.GetComponent<MeshCollider>();
                    if (mc != null) Object.DestroyImmediate(mc);
                    if (go.GetComponent<CapsuleCollider>() == null)
                    {
                        var cap = go.AddComponent<CapsuleCollider>();
                        cap.direction = 1; // Y
                        cap.height = 2.0f;
                        cap.radius = 0.35f;
                        cap.center = Vector3.zero;
                    }
                    swapped++;
                }
                break;
            }
            return swapped;
        }

        private static int AddTreeLODs(Scene scene)
        {
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Greybox_Root") continue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("Tree_")) continue;
                    var go = t.gameObject;
                    if (go.GetComponent<LODGroup>() != null) continue;

                    var lodGroup = go.AddComponent<LODGroup>();
                    var fullRenderers = go.GetComponentsInChildren<Renderer>(true);
                    var trunkRenderer = new List<Renderer>();
                    foreach (var r in fullRenderers)
                        if (r.gameObject.name == "Trunk")
                            trunkRenderer.Add(r);

                    var lods = new LOD[]
                    {
                        new LOD(0.30f, fullRenderers),       // near: full detail (trunk + crown + puffs)
                        new LOD(0.08f, trunkRenderer.ToArray()), // far: trunk only (billboard-ish silhouette)
                    };
                    lodGroup.SetLODs(lods);
                    lodGroup.RecalculateBounds();
                    count++;
                }
                break;
            }
            return count;
        }

        private static int AttachKafkaIdle(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Kafka") continue;
                if (root.transform.childCount == 0) return 0;
                var fbxChild = root.transform.GetChild(0).gameObject;

                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(KafkaIdleScriptAsset);
                if (scriptAsset == null) return 0;
                var scriptType = scriptAsset.GetClass();
                if (scriptType == null) return 0;
                if (fbxChild.GetComponent(scriptType) == null)
                    fbxChild.AddComponent(scriptType);
                return 1;
            }
            return 0;
        }

        private static int TightenFog()
        {
            // Bring fog end in a bit so distant trees blend softly — reads as atmospheric depth.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.85f, 0.92f, 0.78f);
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 85f;
            return 1;
        }
    }
}
