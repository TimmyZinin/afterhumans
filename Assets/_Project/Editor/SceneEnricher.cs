using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Afterhumans.Player;
using Afterhumans.Dialogue;
using Afterhumans.Kafka;
using Afterhumans.Scenes;
using Afterhumans.Art;

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
        // targetScene is the next scene the exit trigger should load (null = no trigger).
        // gateVar/lockedKnot: if set, the Ink bool must be true before the player can leave.
        // themeName: SceneTheme asset at Assets/_Project/Art/Themes/{name}.asset для BOT-F06.
        private static readonly (string scene, string themeName, string npcKnot, string npcName, string targetScene, Vector3 exitPos, string gateVar, string lockedKnot)[] GameScenes = new[]
        {
            // Botanika → City is gated: player must talk to Nikolai first (sets door_to_city_open)
            ("Scene_Botanika", "Botanika", "sasha",   "Placeholder_NPC_Sasha",    "Scene_City",    new Vector3(0f, 1f, 12f), "door_to_city_open", "door_to_city"),
            // City → Desert and Desert → Credits are ungated
            ("Scene_City",     "City",     "dmitriy", "Placeholder_NPC_Dmitriy",  "Scene_Desert",  new Vector3(0f, 1f, 14f), null, null),
            ("Scene_Desert",   "Desert",   "cursor",  "Placeholder_Cursor",       "Scene_Credits", new Vector3(0f, 1f, 16f), null, null),
        };

        [MenuItem("Afterhumans/Setup/Enrich All Scenes")]
        public static void EnrichAllScenes()
        {
            Debug.Log("[SceneEnricher] Starting enrichment...");

            foreach (var (scene, themeName, npcKnot, npcName, targetScene, exitPos, gateVar, lockedKnot) in GameScenes)
            {
                string path = $"{ScenesDir}/{scene}.unity";
                if (!File.Exists(path))
                {
                    Debug.LogError($"[SceneEnricher] Scene not found: {path}");
                    continue;
                }

                Debug.Log($"[SceneEnricher] Opening {scene}...");
                var sceneObj = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                EnrichScene(sceneObj, scene, themeName, npcKnot, npcName, targetScene, exitPos, gateVar, lockedKnot);

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

        private static void EnrichScene(Scene scene, string sceneName, string themeName, string npcKnot, string npcName, string targetScene, Vector3 exitPos, string gateVar, string lockedKnot)
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
                // BOT-F09: showDebugHud OFF by default for production builds.
                // Use MenuItem Afterhumans/Debug/Toggle PlayerInteraction HUD for QA.
                var hudProp = soInt.FindProperty("showDebugHud");
                if (hudProp != null) hudProp.boolValue = false;
                soInt.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[SceneEnricher] Wired PlayerInteraction in {sceneName} (cam+maxDistance=5+HUD=off)");
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

            // Kafka the corgi — placeholder black/white stretched cube
            CreateKafkaPlaceholder(player);

            // Dialogue system: DialogueManager singleton + Canvas UI
            CreateDialogueSystem();

            // Scene transition fade overlay + exit trigger to next scene
            CreateSceneTransitionAndExit(targetScene, exitPos, gateVar, lockedKnot);

            // BOT-F06 + BOT-A01: ThemeLoader runtime applies SceneTheme (палитра,
            // освещение, fog, Volume Profile post-FX) в Awake при scene load.
            CreateThemeLoader(themeName);

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void CreateThemeLoader(string themeName)
        {
            var existing = GameObject.Find("ThemeRoot");
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject("ThemeRoot");
            var loader = Undo.AddComponent<ThemeLoader>(root);

            var themePath = $"Assets/_Project/Art/Themes/{themeName}.asset";
            var theme = AssetDatabase.LoadAssetAtPath<SceneTheme>(themePath);
            if (theme == null)
            {
                Debug.LogWarning($"[SceneEnricher] SceneTheme not found: {themePath}");
                return;
            }

            var so = new SerializedObject(loader);
            var themeProp = so.FindProperty("theme");
            if (themeProp != null)
            {
                themeProp.objectReferenceValue = theme;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log($"[SceneEnricher] ThemeLoader wired to {themeName}");
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

            // BOT-N06: Speaker name label at top of panel
            var speakerGO = new GameObject("SpeakerText");
            speakerGO.transform.SetParent(panelGO.transform, worldPositionStays: false);
            var speakerRect = Undo.AddComponent<RectTransform>(speakerGO);
            speakerRect.anchorMin = new Vector2(0.1f, 0.85f);
            speakerRect.anchorMax = new Vector2(0.5f, 1.0f);
            speakerRect.offsetMin = Vector2.zero;
            speakerRect.offsetMax = Vector2.zero;
            var speakerText = Undo.AddComponent<TextMeshProUGUI>(speakerGO);
            speakerText.text = "";
            speakerText.fontSize = 24;
            speakerText.color = new Color(0.91f, 0.65f, 0.36f);  // ART_BIBLE §3.1 primary amber
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.alignment = TextAlignmentOptions.BottomLeft;
            speakerText.textWrappingMode = TextWrappingModes.NoWrap;
            speakerGO.SetActive(false);  // hidden until dialogue line has speaker prefix

            // Line text (TextMeshProUGUI)
            var textGO = new GameObject("LineText");
            textGO.transform.SetParent(panelGO.transform, worldPositionStays: false);
            var textRect = Undo.AddComponent<RectTransform>(textGO);
            textRect.anchorMin = new Vector2(0.1f, 0.4f);
            textRect.anchorMax = new Vector2(0.9f, 0.85f);  // below speaker
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var lineText = Undo.AddComponent<TextMeshProUGUI>(textGO);
            lineText.text = "";
            lineText.fontSize = 28;
            lineText.color = Color.white;
            lineText.alignment = TextAlignmentOptions.TopLeft;
            lineText.textWrappingMode = TextWrappingModes.Normal;

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
            var speakerProp = soDui.FindProperty("speakerText");
            if (speakerProp != null) speakerProp.objectReferenceValue = speakerText;
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

        private static void CreateSceneTransitionAndExit(string targetScene, Vector3 exitPos, string gateVar, string lockedKnot)
        {
            // --- 1. SceneTransition GameObject with its own Canvas + fade Image
            var existingTransition = GameObject.Find("SceneTransition");
            if (existingTransition != null) Object.DestroyImmediate(existingTransition);

            var transitionGO = new GameObject("SceneTransition");
            var canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transitionGO.transform, worldPositionStays: false);
            var canvas = Undo.AddComponent<Canvas>(canvasGO);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // above dialogue UI
            Undo.AddComponent<CanvasScaler>(canvasGO).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            Undo.AddComponent<GraphicRaycaster>(canvasGO);

            var fadeGO = new GameObject("FadeImage");
            fadeGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);
            var fadeRect = Undo.AddComponent<RectTransform>(fadeGO);
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            var fadeImg = Undo.AddComponent<Image>(fadeGO);
            fadeImg.color = new Color(0f, 0f, 0f, 0f);
            fadeImg.raycastTarget = false;

            var transition = Undo.AddComponent<SceneTransition>(transitionGO);
            var so = new SerializedObject(transition);
            var fadeProp = so.FindProperty("fadeOverlay");
            if (fadeProp != null)
            {
                fadeProp.objectReferenceValue = fadeImg;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- 2. Exit trigger box placed near the scene's forward edge
            if (!string.IsNullOrEmpty(targetScene))
            {
                var existingExit = GameObject.Find("Scene_Exit_Trigger");
                if (existingExit != null) Object.DestroyImmediate(existingExit);

                var exit = new GameObject("Scene_Exit_Trigger");
                exit.transform.position = exitPos;
                var box = Undo.AddComponent<BoxCollider>(exit);
                box.isTrigger = true;
                box.size = new Vector3(6f, 3f, 2f); // wide enough the player can't miss it
                var trigger = Undo.AddComponent<SceneExitTrigger>(exit);
                var soTrig = new SerializedObject(trigger);
                var tsProp = soTrig.FindProperty("targetScene");
                if (tsProp != null) tsProp.stringValue = targetScene;
                var gateProp = soTrig.FindProperty("gateInkVarName");
                if (gateProp != null) gateProp.stringValue = gateVar ?? "";
                var lockedProp = soTrig.FindProperty("lockedKnot");
                if (lockedProp != null) lockedProp.stringValue = lockedKnot ?? "";
                soTrig.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[SceneEnricher] Exit trigger → {targetScene} at {exitPos} (gate={gateVar ?? "none"})");
            }
        }

        private static void CreateKafkaPlaceholder(GameObject player)
        {
            // Already exists (re-run) — leave it
            var existing = GameObject.Find("Kafka");
            if (existing != null) return;

            var kafka = new GameObject("Kafka");
            kafka.tag = "Untagged";

            // Body: low wide box tinted black-white corgi-ish via simple dark grey tint.
            // Real corgi mesh will replace this later.
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "KafkaBody";
            body.transform.SetParent(kafka.transform, worldPositionStays: false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.35f, 0.25f, 0.55f); // corgi-proportioned stub
            var bodyRend = body.GetComponent<Renderer>();
            if (bodyRend != null && bodyRend.sharedMaterial != null)
            {
                var mat = new Material(bodyRend.sharedMaterial);
                mat.color = new Color(0.18f, 0.18f, 0.20f); // almost black
                bodyRend.sharedMaterial = mat;
            }

            // White chest/collar stripe
            var chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = "KafkaChest";
            chest.transform.SetParent(kafka.transform, worldPositionStays: false);
            chest.transform.localPosition = new Vector3(0f, -0.02f, 0.20f);
            chest.transform.localScale = new Vector3(0.36f, 0.20f, 0.16f);
            var chestRend = chest.GetComponent<Renderer>();
            if (chestRend != null && chestRend.sharedMaterial != null)
            {
                var mat = new Material(chestRend.sharedMaterial);
                mat.color = new Color(0.95f, 0.95f, 0.93f); // off-white
                chestRend.sharedMaterial = mat;
            }
            // Disable child colliders — only Kafka root needs one for physics sanity
            Object.DestroyImmediate(chest.GetComponent<Collider>());
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Position Kafka at player's feet + 1m to the right
            if (player != null)
            {
                kafka.transform.position = player.transform.position + new Vector3(1.2f, -0.9f, 0.3f);
            }
            else
            {
                kafka.transform.position = new Vector3(1.2f, 0.2f, 0.3f);
            }

            // Behaviour
            var follow = Undo.AddComponent<KafkaFollowSimple>(kafka);
            Debug.Log($"[SceneEnricher] Kafka placeholder added ({kafka.name}, follow={follow != null})");
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
