using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// v2 scene builder — ONE file, 5 sprint methods.
    /// Replaces v1's 9 fragmented editor scripts.
    /// Each sprint is idempotent (destroys its root, recreates).
    /// </summary>
    public static class BotanikaBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_Botanika.unity";

        // ============================================================
        // SPRINT 1: GREYBOX
        // Grey cubes, floor, walls, furniture silhouettes.
        // Goal: proportions, scale, navigation.
        // ============================================================

        [MenuItem("Afterhumans/v2/Sprint 1 — Greybox")]
        public static void Sprint1_Greybox()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // WIPE ENTIRE SCENE — remove ALL root GameObjects (v1 garbage)
            var roots = scene.GetRootGameObjects();
            foreach (var go in roots)
            {
                Object.DestroyImmediate(go);
            }
            Debug.Log($"[BotanikaBuilder] Cleared {roots.Length} root objects from scene");

            var root = new GameObject("Botanika_Greybox");
            var grey = MakeGreyMaterial();
            var darkGrey = MakeMaterial("DarkGrey", new Color(0.35f, 0.35f, 0.35f));
            var green = MakeMaterial("Plant", new Color(0.25f, 0.45f, 0.2f));

            // === STRUCTURE: floor, walls, glass ceiling ===
            MakeBox(root, "Floor", new Vector3(0, -0.05f, 0), new Vector3(12, 0.1f, 10), grey);
            MakeBox(root, "Wall_North", new Vector3(0, 1.6f, 5), new Vector3(12, 3.2f, 0.2f), grey);
            MakeBox(root, "Wall_South_L", new Vector3(-3.3f, 1.6f, -5), new Vector3(5.4f, 3.2f, 0.2f), grey);
            MakeBox(root, "Wall_South_R", new Vector3(3.3f, 1.6f, -5), new Vector3(5.4f, 3.2f, 0.2f), grey);
            MakeBox(root, "Wall_East", new Vector3(6, 1.6f, 0), new Vector3(0.2f, 3.2f, 10), grey);
            MakeBox(root, "Wall_West", new Vector3(-6, 1.6f, 0), new Vector3(0.2f, 3.2f, 10), grey);
            // Glass ceiling (оранжерея = стеклянная крыша)
            MakeBox(root, "GlassCeiling", new Vector3(0, 3.2f, 0), new Vector3(12, 0.05f, 10), grey);

            // === FURNITURE: два дивана, кофейный стол, лампы ===
            // Sofa 1 (Sasha) — центр-север, лицом к югу
            MakeBox(root, "Sofa_Sasha", new Vector3(0, 0.35f, 3.8f), new Vector3(2.0f, 0.7f, 0.8f), darkGrey);
            // Sofa 2 — у восточной стены
            MakeBox(root, "Sofa_East", new Vector3(4.5f, 0.35f, 0), new Vector3(0.8f, 0.7f, 1.8f), darkGrey);
            // Coffee table перед диваном Саши
            MakeBox(root, "CoffeeTable", new Vector3(0, 0.22f, 2.5f), new Vector3(1.0f, 0.44f, 0.5f), grey);
            // Floor lamp около дивана
            MakeBox(root, "FloorLamp", new Vector3(1.8f, 0.7f, 3.8f), new Vector3(0.2f, 1.4f, 0.2f), darkGrey);

            // === ЗОНЫ NPC ===
            // Mila: стол + стул (западная часть)
            MakeBox(root, "Desk_Mila", new Vector3(-4, 0.38f, 1.5f), new Vector3(1.2f, 0.76f, 0.6f), grey);
            MakeBox(root, "Chair_Mila", new Vector3(-3, 0.35f, 1.5f), new Vector3(0.5f, 0.7f, 0.5f), darkGrey);
            // Kirill: кухонный угол (восточная часть, юг)
            MakeBox(root, "Kitchen_Counter", new Vector3(4.5f, 0.45f, -2.5f), new Vector3(1.5f, 0.9f, 0.6f), grey);
            MakeBox(root, "Kitchen_Stove", new Vector3(4.5f, 0.9f, -2.5f), new Vector3(0.4f, 0.1f, 0.4f), darkGrey);
            // Nikolai: угол с столом и бутылкой (северо-запад)
            MakeBox(root, "Table_Nikolai", new Vector3(-4.5f, 0.3f, 4.2f), new Vector3(0.8f, 0.6f, 0.8f), grey);
            MakeBox(root, "Chair_Nikolai", new Vector3(-3.5f, 0.35f, 4.2f), new Vector3(0.5f, 0.7f, 0.5f), darkGrey);
            // Stas: у двери (юг), ходит туда-сюда
            // (нет мебели — он стоит/ходит)

            // === СТЕЛЛАЖИ С КНИГАМИ ===
            MakeBox(root, "Bookcase_NW", new Vector3(-5.2f, 0.9f, 4.8f), new Vector3(1.0f, 1.8f, 0.4f), darkGrey);
            MakeBox(root, "Bookcase_NE", new Vector3(5.2f, 0.9f, 4.8f), new Vector3(1.0f, 1.8f, 0.4f), darkGrey);
            MakeBox(root, "Bookcase_W", new Vector3(-5.2f, 0.9f, 0), new Vector3(0.6f, 1.8f, 1.2f), darkGrey);

            // === СЕРВЕРНАЯ СТОЙКА (data-элемент) ===
            MakeBox(root, "ServerRack", new Vector3(5.2f, 0.8f, -3.5f), new Vector3(0.5f, 1.6f, 0.4f), darkGrey);

            // === РАСТЕНИЯ — это ОРАНЖЕРЕЯ, зелень везде ===
            // Крупные кусты по углам
            MakeBox(root, "Plant_NW", new Vector3(-5.0f, 0.6f, 3.0f), new Vector3(1.2f, 1.2f, 1.2f), green);
            MakeBox(root, "Plant_NE", new Vector3(5.0f, 0.6f, 3.0f), new Vector3(1.0f, 1.0f, 1.0f), green);
            MakeBox(root, "Plant_SW", new Vector3(-5.0f, 0.5f, -3.5f), new Vector3(1.0f, 1.0f, 1.0f), green);
            MakeBox(root, "Plant_SE", new Vector3(5.0f, 0.4f, -1.0f), new Vector3(0.8f, 0.8f, 0.8f), green);
            // Средние растения вдоль стен
            MakeBox(root, "Plant_W1", new Vector3(-5.3f, 0.4f, -1.0f), new Vector3(0.6f, 0.8f, 0.6f), green);
            MakeBox(root, "Plant_W2", new Vector3(-5.3f, 0.35f, 1.0f), new Vector3(0.5f, 0.7f, 0.5f), green);
            MakeBox(root, "Plant_E1", new Vector3(5.3f, 0.4f, 1.5f), new Vector3(0.6f, 0.8f, 0.6f), green);
            MakeBox(root, "Plant_N1", new Vector3(-2.0f, 0.5f, 4.7f), new Vector3(0.8f, 1.0f, 0.5f), green);
            MakeBox(root, "Plant_N2", new Vector3(2.5f, 0.45f, 4.7f), new Vector3(0.7f, 0.9f, 0.5f), green);
            // Мелкие горшки на столах / рядом с мебелью
            MakeBox(root, "PlantPot_Table", new Vector3(0.3f, 0.55f, 2.5f), new Vector3(0.2f, 0.3f, 0.2f), green);
            MakeBox(root, "PlantPot_Desk", new Vector3(-4.5f, 0.85f, 1.5f), new Vector3(0.15f, 0.25f, 0.15f), green);
            MakeBox(root, "PlantPot_Window", new Vector3(3.0f, 0.3f, 4.5f), new Vector3(0.3f, 0.5f, 0.3f), green);

            // --- PLAYER ---
            SetupPlayer();

            // --- MINIMAL LIGHT (just to see) ---
            var lightGo = new GameObject("Sun_Temp");
            lightGo.transform.SetParent(root.transform);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.0f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.4f);
            RenderSettings.ambientIntensity = 1.0f;

            // Save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BotanikaBuilder] Sprint 1 GREYBOX done — floor, walls, furniture blocks, player");
        }

        // ============================================================
        // SPRINT 2: GAMEPLAY
        // NPC capsules + Kafka + Dialogue + Interaction + Door gate
        // Goal: walk up to NPC, press E, read dialogue, Kafka follows
        // ============================================================

        [MenuItem("Afterhumans/v2/Sprint 2 — Gameplay")]
        public static void Sprint2_Gameplay()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearRoot("Botanika_Gameplay");

            var root = new GameObject("Botanika_Gameplay");

            // --- PLAYER INTERACTION ---
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var pi = player.GetComponent<Afterhumans.Player.PlayerInteraction>();
                if (pi == null) pi = player.AddComponent<Afterhumans.Player.PlayerInteraction>();
                // Enable debug HUD so Tim can see interaction status
                var piSo = new SerializedObject(pi);
                var debugProp = piSo.FindProperty("showDebugHud");
                if (debugProp != null) { debugProp.boolValue = true; piSo.ApplyModifiedPropertiesWithoutUndo(); }
                // Set maxDistance
                var distProp = piSo.FindProperty("maxDistance");
                if (distProp != null) { distProp.floatValue = 5f; piSo.ApplyModifiedPropertiesWithoutUndo(); }
            }

            // --- DIALOGUE SYSTEM ---
            SetupDialogueSystem(root);

            // --- 5 NPCs ---
            var npcYellow = MakeMaterial("NPC_Yellow", new Color(0.85f, 0.75f, 0.3f));
            var npcBlue   = MakeMaterial("NPC_Blue", new Color(0.3f, 0.5f, 0.8f));
            var npcRed    = MakeMaterial("NPC_Red", new Color(0.8f, 0.3f, 0.25f));
            var npcPurple = MakeMaterial("NPC_Purple", new Color(0.6f, 0.3f, 0.7f));
            var npcGreen  = MakeMaterial("NPC_Green", new Color(0.3f, 0.65f, 0.35f));

            // NPC positions: clearly IN FRONT of furniture, 1m+ clearance
            SpawnNpc(root, "Sasha",   new Vector3(0, 0, 2.0f),     180, "sasha",   3.0f, npcYellow);    // in front of sofa
            SpawnNpc(root, "Mila",    new Vector3(-2.5f, 0, 1.5f),  90, "mila",    2.5f, npcBlue);      // in front of desk
            SpawnNpc(root, "Kirill",  new Vector3(3.0f, 0, -2.5f), -90, "kirill",  2.5f, npcRed);       // in front of kitchen
            SpawnNpc(root, "Nikolai", new Vector3(-3.0f, 0, 3.5f), 135, "nikolai", 2.5f, npcPurple);    // in front of corner table
            SpawnNpc(root, "Stas",    new Vector3(1.5f, 0, -4f),     0, "stas",    2.5f, npcGreen);     // near door

            // --- KAFKA ---
            SetupKafka(root);

            // --- DOOR GATE ---
            var door = new GameObject("DoorGate");
            door.transform.SetParent(root.transform);
            door.transform.position = new Vector3(0, 1, -5.2f);
            var doorCol = door.AddComponent<BoxCollider>();
            doorCol.isTrigger = true;
            doorCol.size = new Vector3(3, 3, 1);
            if (player != null)
            {
                var cue = door.AddComponent<Afterhumans.UI.DoorCueUI>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BotanikaBuilder] Sprint 2 GAMEPLAY done — 5 NPCs, Kafka, dialogue, door gate");
        }

        private static void SpawnNpc(GameObject parent, string npcName, Vector3 pos, float yRot,
            string knotName, float interactRadius, Material mat)
        {
            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = $"NPC_{npcName}";
            npc.transform.SetParent(parent.transform, worldPositionStays: false);
            npc.transform.position = pos;
            npc.transform.rotation = Quaternion.Euler(0, yRot, 0);
            npc.isStatic = false;

            // Material (colored so NPCs are visually distinct)
            var rend = npc.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;

            // Collider — replace default with properly sized capsule
            Object.DestroyImmediate(npc.GetComponent<CapsuleCollider>());
            var col = npc.AddComponent<CapsuleCollider>();
            col.radius = 0.35f;
            col.height = 1.8f;
            col.center = new Vector3(0, 0.9f, 0);

            // Scale capsule to human height (default capsule is 2m, we want 1.8m)
            npc.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);

            // Interactable
            var interactable = npc.AddComponent<Afterhumans.Dialogue.Interactable>();
            interactable.knotName = knotName;
            interactable.promptText = "говорить";
            interactable.interactRadius = interactRadius;

            // Idle animation
            npc.AddComponent<Afterhumans.Art.NpcIdleBob>();

            // Interaction prompt: worldspace Canvas + TMP above head
            var promptRoot = new GameObject($"Prompt_{npcName}");
            promptRoot.transform.SetParent(npc.transform, worldPositionStays: false);
            promptRoot.transform.localPosition = new Vector3(0, 2.5f, 0);

            var promptCanvas = promptRoot.AddComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.WorldSpace;
            promptCanvas.sortingOrder = 50;
            var promptRect = promptRoot.GetComponent<RectTransform>();
            promptRect.sizeDelta = new Vector2(200f, 50f);  // wide enough for text
            promptRoot.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);  // scale down to worldspace

            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(promptRoot.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"[E] {interactable.promptText}";
            tmp.fontSize = 36;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            promptRoot.AddComponent<CanvasGroup>();
            var promptUI = promptRoot.AddComponent<Afterhumans.Art.InteractionPromptUI>();
            promptUI.showRadius = interactRadius + 1f;

            Debug.Log($"[BotanikaBuilder] NPC {npcName} at {pos}, knot={knotName}");
        }

        private static void SetupKafka(GameObject parent)
        {
            var kafka = new GameObject("Kafka");
            kafka.transform.SetParent(parent.transform, worldPositionStays: false);
            kafka.transform.position = new Vector3(1, 0, -2.5f);

            // Visual: simple dark capsule for now (Sprint 4 will replace with corgi model)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "KafkaBody";
            body.transform.SetParent(kafka.transform, worldPositionStays: false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.25f, 0.2f, 0.4f);
            body.transform.localRotation = Quaternion.Euler(0, 0, 90); // horizontal
            var kafkaMat = MakeMaterial("Kafka", new Color(0.15f, 0.13f, 0.12f));
            body.GetComponent<Renderer>().sharedMaterial = kafkaMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Follow behavior
            kafka.AddComponent<Afterhumans.Kafka.KafkaFollowSimple>();

            Debug.Log("[BotanikaBuilder] Kafka spawned at (1, 0, -2.5)");
        }

        private static void SetupDialogueSystem(GameObject parent)
        {
            // DialogueManager singleton
            var dmGo = new GameObject("DialogueManager");
            dmGo.transform.SetParent(parent.transform);
            var dm = dmGo.AddComponent<Afterhumans.Dialogue.DialogueManager>();

            // Load ink JSON
            var inkJson = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Dialogues/dataland.json");
            if (inkJson != null)
            {
                // Set inkJsonAsset via serialized field
                var so = new SerializedObject(dm);
                var prop = so.FindProperty("inkJsonAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = inkJson;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                Debug.Log($"[BotanikaBuilder] DialogueManager wired to dataland.json ({inkJson.text.Length} chars)");
            }
            else
            {
                Debug.LogError("[BotanikaBuilder] dataland.json NOT FOUND!");
            }

            // Dialogue UI Canvas
            var canvasGo = new GameObject("DialogueCanvas");
            canvasGo.transform.SetParent(parent.transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Dialogue panel (bottom third)
            var panelGo = new GameObject("DialoguePanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.35f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.7f);
            panelGo.SetActive(false); // hidden until dialogue starts

            // Speaker name text
            var speakerGo = new GameObject("SpeakerText");
            speakerGo.transform.SetParent(panelGo.transform, false);
            var speakerRect = speakerGo.AddComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0.05f, 0.7f);
            speakerRect.anchorMax = new Vector2(0.95f, 0.95f);
            speakerRect.offsetMin = Vector2.zero;
            speakerRect.offsetMax = Vector2.zero;
            var speakerTmp = speakerGo.AddComponent<TextMeshProUGUI>();
            speakerTmp.fontSize = 22;
            speakerTmp.fontStyle = FontStyles.Bold;
            speakerTmp.color = new Color(0.91f, 0.65f, 0.36f); // amber

            // Dialogue line text
            var lineGo = new GameObject("LineText");
            lineGo.transform.SetParent(panelGo.transform, false);
            var lineRect = lineGo.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.05f, 0.05f);
            lineRect.anchorMax = new Vector2(0.95f, 0.65f);
            lineRect.offsetMin = Vector2.zero;
            lineRect.offsetMax = Vector2.zero;
            var lineTmp = lineGo.AddComponent<TextMeshProUGUI>();
            lineTmp.fontSize = 20;
            lineTmp.color = Color.white;
            lineTmp.enableWordWrapping = true;

            // Wire DialogueUI — field names must match DialogueUI.cs exactly
            var dui = canvasGo.AddComponent<Afterhumans.Dialogue.DialogueUI>();
            var duiSo = new SerializedObject(dui);
            var panelProp = duiSo.FindProperty("panel");
            if (panelProp != null) panelProp.objectReferenceValue = panelGo;
            var speakerProp = duiSo.FindProperty("speakerText");
            if (speakerProp != null) speakerProp.objectReferenceValue = speakerTmp;
            var lineProp = duiSo.FindProperty("lineText");
            if (lineProp != null) lineProp.objectReferenceValue = lineTmp;
            duiSo.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[BotanikaBuilder] DialogueUI wired: panel={panelProp?.objectReferenceValue != null}, speaker={speakerProp?.objectReferenceValue != null}, line={lineProp?.objectReferenceValue != null}");

            Debug.Log("[BotanikaBuilder] Dialogue system created (Manager + Canvas + UI)");
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private static void ClearRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        private static Material MakeGreyMaterial()
        {
            return MakeMaterial("Greybox", new Color(0.5f, 0.5f, 0.5f));
        }

        private static Material MakeMaterial(string name, Color color, float smoothness = 0.1f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        private static GameObject MakeBox(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.isStatic = true;
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
            return go;
        }

        private static void SetupPlayer()
        {
            // Remove old player if exists
            var oldPlayer = GameObject.Find("Player");
            if (oldPlayer != null) Object.DestroyImmediate(oldPlayer);

            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0, 0, -3);

            // CharacterController
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.slopeLimit = 45;
            cc.stepOffset = 0.3f;

            // Camera
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform, worldPositionStays: false);
            camGo.transform.localPosition = new Vector3(0, 1.65f, 0);
            camGo.tag = "MainCamera";

            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 65;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.3f, 0.3f, 0.3f); // dark grey for greybox

            camGo.AddComponent<AudioListener>();

            // FPS Controller
            var fps = player.AddComponent<Afterhumans.Player.SimpleFirstPersonController>();

            Debug.Log("[BotanikaBuilder] Player setup at (0, 0, -3) with camera at eye height");
        }
    }
}
