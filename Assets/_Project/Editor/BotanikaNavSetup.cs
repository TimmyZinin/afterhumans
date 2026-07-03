using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using Afterhumans.Kafka;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Future-movement foundation for the hero corgi: bakes a NavMesh over the
    /// Botanika floor and attaches a NavMeshAgent + CorgiWander to Hero_Corgi so
    /// the dog can pathfind/wander the scene (and follow the player later via
    /// KafkaFollow). Non-visual — does not affect the hero render. Run headless:
    ///   -executeMethod Afterhumans.EditorTools.BotanikaNavSetup.Setup
    /// </summary>
    public static class BotanikaNavSetup
    {
        private const string Scene = "Assets/_Project/Scenes/Scene_Botanika.unity";

        public static void Setup()
        {
            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            // --- NavMeshSurface on a dedicated GO: bake from physics colliders so the
            // floor (MeshCollider) is walkable and the column (CapsuleCollider) carves out.
            var surfGO = GameObject.Find("BotanikaNavMesh");
            if (surfGO == null) surfGO = new GameObject("BotanikaNavMesh");
            var surf = surfGO.GetComponent<NavMeshSurface>();
            if (surf == null) surf = surfGO.AddComponent<NavMeshSurface>();
            surf.collectObjects = CollectObjects.All;
            surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surf.layerMask = ~0;
            // small agent profile for the corgi
            surf.overrideVoxelSize = true; surf.voxelSize = 0.10f;
            try { surf.BuildNavMesh(); } catch (System.Exception e) { Debug.LogError("[NavSetup] bake failed: " + e); }

            var tri = NavMesh.CalculateTriangulation();
            int verts = tri.vertices != null ? tri.vertices.Length : 0;
            Debug.Log($"[NavSetup] NavMesh baked: {verts} verts, {(tri.indices != null ? tri.indices.Length / 3 : 0)} tris");

            // --- Attach NavMeshAgent + CorgiWander to the hero corgi.
            var corgi = GameObject.Find("Hero_Corgi");
            if (corgi == null)
            {
                // search recursively under the real-assets root
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    if (t.name == "Hero_Corgi") { corgi = t.gameObject; break; }
            }
            if (corgi != null)
            {
                var agent = corgi.GetComponent<NavMeshAgent>();
                if (agent == null) agent = corgi.AddComponent<NavMeshAgent>();
                agent.radius = 0.22f;
                agent.height = 0.45f;
                agent.baseOffset = 0f;
                agent.speed = 1.8f;
                agent.angularSpeed = 320f;
                agent.acceleration = 6f;
                agent.stoppingDistance = 0.3f;
                agent.autoBraking = true;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

                if (corgi.GetComponent<CorgiWander>() == null) corgi.AddComponent<CorgiWander>();
                Debug.Log($"[NavSetup] Hero_Corgi: NavMeshAgent + CorgiWander attached (onNavMesh check at runtime). pos={corgi.transform.position}");
            }
            else
            {
                Debug.LogWarning("[NavSetup] Hero_Corgi not found — run BuildFull first so the corgi exists in the scene.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Scene);
            Debug.Log("[NavSetup] DONE — scene saved with NavMesh + corgi agent.");
        }
    }
}
