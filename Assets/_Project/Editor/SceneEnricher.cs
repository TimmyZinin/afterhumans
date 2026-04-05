using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
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

        // NPC knot MUST be a top-level knot (=== name ===) in dataland.ink, not a stitch (= name).
        // Ink.Story.ChoosePathString() only resolves top-level paths.
        private static readonly (string scene, string npcKnot, string npcName)[] GameScenes = new[]
        {
            ("Scene_Botanika", "sasha", "Placeholder_NPC_Sasha"),
            ("Scene_City", "dmitriy", "Placeholder_NPC_Dmitriy"),
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

                // Force flush all changes before save
                EditorSceneManager.MarkSceneDirty(sceneObj);
                EditorSceneManager.SaveOpenScenes();

                bool saved = EditorSceneManager.SaveScene(sceneObj, path);
                if (saved)
                {
                    Debug.Log($"[SceneEnricher] Saved {scene}");

                    // Verify by reopening the scene and counting SimpleFirstPersonController
                    var reopened = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    int fpsCount = 0;
                    foreach (var root in reopened.GetRootGameObjects())
                    {
                        if (root.GetComponentInChildren<SimpleFirstPersonController>(includeInactive: true) != null)
                        {
                            fpsCount++;
                        }
                    }
                    Debug.Log($"[SceneEnricher] VERIFY {scene}: {fpsCount} SimpleFirstPersonController found after save+reopen");
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

            // Spawn 2m in front of the placeholder NPC (which sits at (2, 1, 3))
            // so the player can see and reach it immediately in the walking skeleton.
            player.transform.position = new Vector3(2f, 1.1f, 0f);
            player.transform.rotation = Quaternion.identity;

            // CharacterController — use Undo.AddComponent for proper Editor serialization
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = Undo.AddComponent<CharacterController>(player);
            if (cc != null)
            {
                cc.height = 1.8f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.3f;
            }

            // SimpleFirstPersonController — Undo.AddComponent ensures proper serialization
            // in batchmode (regular AddComponent<T>() has a known issue where the component
            // is added to runtime memory but not persisted when scene is saved immediately after).
            var fps = player.GetComponent<SimpleFirstPersonController>();
            if (fps == null)
            {
                // Use explicit System.Type to avoid generic type lookup issues across assemblies
                System.Type fpsType = typeof(SimpleFirstPersonController);
                if (fpsType == null)
                {
                    Debug.LogError("[SceneEnricher] typeof(SimpleFirstPersonController) returned null!");
                }
                else
                {
                    fps = Undo.AddComponent(player, fpsType) as SimpleFirstPersonController;
                }
            }
            if (fps == null)
            {
                Debug.LogError($"[SceneEnricher] FAILED to add SimpleFirstPersonController in {sceneName}!");
            }
            else
            {
                Debug.Log($"[SceneEnricher] Added SimpleFirstPersonController to Player in {sceneName} (instance id: {fps.GetInstanceID()})");
            }
            EditorUtility.SetDirty(player);

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

            // PlayerInteraction — raycasts from camera forward, listens for E press
            var interaction = player.GetComponent<PlayerInteraction>();
            if (interaction == null)
            {
                interaction = Undo.AddComponent<PlayerInteraction>(player);
            }
            if (interaction != null)
            {
                var soInt = new SerializedObject(interaction);
                var camProp = soInt.FindProperty("playerCamera");
                if (camProp != null) camProp.objectReferenceValue = cam;
                var distProp = soInt.FindProperty("maxDistance");
                if (distProp != null) distProp.floatValue = 5f;
                var hudProp = soInt.FindProperty("showDebugHud");
                if (hudProp != null) hudProp.boolValue = true;
                soInt.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[SceneEnricher] Wired PlayerInteraction in {sceneName} (cam+maxDistance=5+HUD)");
            }
            else
            {
                Debug.LogError($"[SceneEnricher] FAILED to add PlayerInteraction in {sceneName}");
            }
            EditorUtility.SetDirty(player);

            // Walls around 50×50 ground to prevent falling off
            CreateBoundaryWalls(sceneName);

            // Placeholder NPC (a cube with an Interactable)
            CreatePlaceholderNpc(sceneName, npcKnot, npcName);

            // Dialogue system: DialogueManager singleton + Canvas UI
            CreateDialogueSystem();

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void CreateDialogueSystem()
        {
            // Remove any existing DialogueSystem for clean re-run
            var existing = GameObject.Find("DialogueSystem");
            if (existing != null) Object.DestroyImmediate(existing);

            // Root GameObject
            var root = new GameObject("DialogueSystem");

            // DialogueManager component
            var dm = Undo.AddComponent<DialogueManager>(root);
            // Wire Ink JSON TextAsset reference
            var inkJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Dialogues/dataland.json");
            if (inkJson != null)
            {
                var so = new SerializedObject(dm);
                var prop = so.FindProperty("inkJsonAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = inkJson;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                Debug.Log($"[SceneEnricher] Wired dataland.json to DialogueManager ({inkJson.text.Length} chars)");
            }
            else
            {
                Debug.LogError("[SceneEnricher] dataland.json not found! Run ForceInkCompile first.");
            }

            // Canvas child
            var canvasGO = new GameObject("DialogueCanvas");
            canvasGO.transform.SetParent(root.transform, worldPositionStays: false);
            var canvas = Undo.AddComponent<Canvas>(canvasGO);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            Undo.AddComponent<CanvasScaler>(canvasGO).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
            Undo.AddComponent<GraphicRaycaster>(canvasGO);

            // Background panel (dimmed rect at bottom third)
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var panelRect = Undo.AddComponent<RectTransform>(panelGO);
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0.35f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImg = Undo.AddComponent<Image>(panelGO);
            panelImg.color = new Color(0f, 0f, 0f, 0.7f);
            panelGO.SetActive(false); // hidden until dialogue starts

            // Line text (TextMeshProUGUI)
            var textGO = new GameObject("LineText");
            textGO.transform.SetParent(panelGO.transform, worldPositionStays: false);
            var textRect = Undo.AddComponent<RectTransform>(textGO);
            textRect.anchorMin = new Vector2(0.1f, 0.4f);
            textRect.anchorMax = new Vector2(0.9f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var lineText = Undo.AddComponent<TextMeshProUGUI>(textGO);
            lineText.text = "";
            lineText.fontSize = 28;
            lineText.color = Color.white;
            lineText.alignment = TextAlignmentOptions.TopLeft;
            lineText.enableWordWrapping = true;

            // Choices container (vertical layout)
            var choicesGO = new GameObject("ChoicesContainer");
            choicesGO.transform.SetParent(panelGO.transform, worldPositionStays: false);
            var choicesRect = Undo.AddComponent<RectTransform>(choicesGO);
            choicesRect.anchorMin = new Vector2(0.1f, 0.05f);
            choicesRect.anchorMax = new Vector2(0.9f, 0.38f);
            choicesRect.offsetMin = Vector2.zero;
            choicesRect.offsetMax = Vector2.zero;
            var vlg = Undo.AddComponent<VerticalLayoutGroup>(choicesGO);
            vlg.spacing = 4;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.LowerLeft;

            // Choice button prefab (reusable template, saved as child under an inactive holder)
            var btnPrefabGO = new GameObject("ChoiceButton_Prefab");
            btnPrefabGO.transform.SetParent(root.transform, worldPositionStays: false);
            btnPrefabGO.SetActive(false);
            var btnRect = Undo.AddComponent<RectTransform>(btnPrefabGO);
            btnRect.sizeDelta = new Vector2(0, 40);
            var btnImg = Undo.AddComponent<Image>(btnPrefabGO);
            btnImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
            var btn = Undo.AddComponent<Button>(btnPrefabGO);
            btn.targetGraphic = btnImg;
            var btnTextGO = new GameObject("Text");
            btnTextGO.transform.SetParent(btnPrefabGO.transform, worldPositionStays: false);
            var btnTextRect = Undo.AddComponent<RectTransform>(btnTextGO);
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = new Vector2(10, 0);
            btnTextRect.offsetMax = new Vector2(-10, 0);
            var btnText = Undo.AddComponent<TextMeshProUGUI>(btnTextGO);
            btnText.text = "> choice";
            btnText.fontSize = 22;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.MidlineLeft;

            // DialogueUI component on root, wire references
            var dialogueUI = Undo.AddComponent<DialogueUI>(root);
            var soDui = new SerializedObject(dialogueUI);
            var panelProp = soDui.FindProperty("panel");
            if (panelProp != null) panelProp.objectReferenceValue = panelGO;
            var lineProp = soDui.FindProperty("lineText");
            if (lineProp != null) lineProp.objectReferenceValue = lineText;
            var choicesProp = soDui.FindProperty("choicesContainer");
            if (choicesProp != null) choicesProp.objectReferenceValue = choicesGO.transform;
            var btnPrefabProp = soDui.FindProperty("choiceButtonPrefab");
            if (btnPrefabProp != null) btnPrefabProp.objectReferenceValue = btn;
            soDui.ApplyModifiedPropertiesWithoutUndo();

            // Wire dialogueUI back to DialogueManager
            var dmSo = new SerializedObject(dm);
            var uiProp = dmSo.FindProperty("dialogueUI");
            if (uiProp != null)
            {
                uiProp.objectReferenceValue = dialogueUI;
                dmSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // EventSystem (needed for UI input)
            if (GameObject.Find("EventSystem") == null)
            {
                var esGO = new GameObject("EventSystem");
                Undo.AddComponent<UnityEngine.EventSystems.EventSystem>(esGO);
                Undo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>(esGO);
            }

            EditorUtility.SetDirty(root);
            Debug.Log("[SceneEnricher] DialogueSystem created with DialogueManager + DialogueUI + Canvas");
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
