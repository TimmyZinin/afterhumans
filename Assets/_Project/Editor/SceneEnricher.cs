using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Afterhumans.Player;
using Afterhumans.Dialogue;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Populates the empty FPS scenes (Botanika, City, Desert) with:
    /// - Player GameObject: CharacterController + SimpleFirstPersonController + child Camera
    /// - 4 invisible walls around 50×50 ground so player can't walk off
    /// - Placeholder NPC cube (tagged, with Interactable pointing to a valid knot)
    /// - Removes the stock "Main Camera" placed by NewScene (replaced by Player's camera)
    ///
    /// Call via:
    ///   Unity -batchmode -nographics -quit -projectPath ~/afterhumans \
    ///     -executeMethod Afterhumans.EditorTools.SceneEnricher.EnrichAllScenes
    /// </summary>
    public static class SceneEnricher
    {
        private const string ScenesDir = "Assets/_Project/Scenes";

        private static readonly (string scene, string npcKnot, string npcName)[] GameScenes = new[]
        {
            ("Scene_Botanika", "sasha_first", "Placeholder_NPC_Sasha"),
            ("Scene_City", "dmitriy_first", "Placeholder_NPC_Dmitriy"),
            ("Scene_Desert", "cursor", "Placeholder_Cursor"),
        };

        [MenuItem("Afterhumans/Setup/Enrich All Scenes")]
        public static void EnrichAllScenes()
        {
            Debug.Log("[SceneEnricher] Starting enrichment...");

            foreach (var (scene, npcKnot, npcName) in GameScenes)
            {
                string path = $"{ScenesDir}/{scene}.unity";
                if (!File.Exists(path))
                {
                    Debug.LogError($"[SceneEnricher] Scene not found: {path}");
                    continue;
                }

                Debug.Log($"[SceneEnricher] Opening {scene}...");
                var sceneObj = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                EnrichScene(sceneObj, scene, npcKnot, npcName);

                bool saved = EditorSceneManager.SaveScene(sceneObj, path);
                if (saved)
                {
                    Debug.Log($"[SceneEnricher] Saved {scene}");
                }
                else
                {
                    Debug.LogError($"[SceneEnricher] Failed to save {scene}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[SceneEnricher] Done.");
        }

        private static void EnrichScene(Scene scene, string sceneName, string npcKnot, string npcName)
        {
            // Remove stock Main Camera — we'll create our own on the Player
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Main Camera")
                {
                    Object.DestroyImmediate(root);
                    break;
                }
            }

            // Find or create Player root
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                player.tag = "Player";
            }

            player.transform.position = new Vector3(0f, 1.1f, -3f);
            player.transform.rotation = Quaternion.identity;

            // CharacterController
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;

            // SimpleFirstPersonController
            var fps = player.GetComponent<SimpleFirstPersonController>();
            if (fps == null) fps = player.AddComponent<SimpleFirstPersonController>();

            // Camera as child of Player
            Transform existingCam = player.transform.Find("PlayerCamera");
            GameObject camGO;
            if (existingCam != null)
            {
                camGO = existingCam.gameObject;
            }
            else
            {
                camGO = new GameObject("PlayerCamera");
                camGO.transform.SetParent(player.transform, worldPositionStays: false);
            }
            camGO.tag = "MainCamera";
            camGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            camGO.transform.localRotation = Quaternion.identity;

            var cam = camGO.GetComponent<Camera>();
            if (cam == null) cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;

            // AudioListener on camera so the player hears things
            if (camGO.GetComponent<AudioListener>() == null)
            {
                camGO.AddComponent<AudioListener>();
            }

            // Walls around 50×50 ground to prevent falling off
            CreateBoundaryWalls(sceneName);

            // Placeholder NPC (a cube with an Interactable)
            CreatePlaceholderNpc(sceneName, npcKnot, npcName);

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void CreateBoundaryWalls(string sceneName)
        {
            string rootName = "Boundary_Walls";
            var existing = GameObject.Find(rootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(rootName);
            // Ground plane from ProjectSetup was scale (5,1,5) = 50m×50m (Unity plane is 10m base)
            float halfSize = 25f;
            float wallHeight = 5f;
            float wallThickness = 1f;

            Vector3[] positions = {
                new Vector3(0f, wallHeight / 2f, halfSize),
                new Vector3(0f, wallHeight / 2f, -halfSize),
                new Vector3(halfSize, wallHeight / 2f, 0f),
                new Vector3(-halfSize, wallHeight / 2f, 0f),
            };
            Vector3[] scales = {
                new Vector3(halfSize * 2f, wallHeight, wallThickness),
                new Vector3(halfSize * 2f, wallHeight, wallThickness),
                new Vector3(wallThickness, wallHeight, halfSize * 2f),
                new Vector3(wallThickness, wallHeight, halfSize * 2f),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall_{i}";
                wall.transform.SetParent(root.transform, worldPositionStays: false);
                wall.transform.position = positions[i];
                wall.transform.localScale = scales[i];

                // Hide walls visually but keep collider — or leave visible as placeholder
                // For debug visibility we keep them rendered with a simple tint
                var rend = wall.GetComponent<Renderer>();
                if (rend != null && rend.sharedMaterial != null)
                {
                    var mat = new Material(rend.sharedMaterial);
                    mat.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    rend.sharedMaterial = mat;
                }
            }
        }

        private static void CreatePlaceholderNpc(string sceneName, string npcKnot, string npcName)
        {
            var existing = GameObject.Find(npcName);
            if (existing != null) Object.DestroyImmediate(existing);

            var npc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            npc.name = npcName;
            npc.transform.position = new Vector3(2f, 1f, 3f);
            npc.transform.localScale = new Vector3(0.7f, 1.8f, 0.7f);

            // Distinctive colour per scene
            var rend = npc.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                var mat = new Material(rend.sharedMaterial);
                switch (sceneName)
                {
                    case "Scene_Botanika":
                        mat.color = new Color(0.85f, 0.65f, 0.3f);
                        break;
                    case "Scene_City":
                        mat.color = new Color(0.9f, 0.92f, 0.95f);
                        break;
                    case "Scene_Desert":
                        mat.color = new Color(0.1f, 0.1f, 0.1f);
                        break;
                }
                rend.sharedMaterial = mat;
            }

            // Ensure collider exists (CreatePrimitive already adds BoxCollider, but mark as trigger off)
            var col = npc.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;

            // Attach Interactable component
            var interactable = npc.AddComponent<Interactable>();
            interactable.knotName = npcKnot;
            interactable.promptText = "говорить";
            interactable.interactRadius = 2.5f;
            interactable.oneTime = false;
        }
    }
}
