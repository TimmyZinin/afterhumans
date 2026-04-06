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
        // SHARED CONSTANTS — single source of truth for all sprints
        // FIX: CRITICAL-1 from mm-review — positions must match across sprints
        // ============================================================

        // Room structure
        private const float WallHeight = 9.6f;
        private const float RoomWidth = 12f;
        private const float RoomDepth = 10f;

        // Asset paths
        private const string FurnitureFbx = "Assets/_Project/Vendor/Kenney/furniture-kit/Models/FBX format";
        private const string NatureFbx = "Assets/_Project/Vendor/Kenney/nature-kit/Models/FBX format";
        private const string CharacterFbx = "Assets/_Project/Vendor/Kenney/blocky-characters/Models/FBX format";
        private const string CharacterTex = CharacterFbx + "/Textures";

        // Furniture positions — used by BOTH greybox AND FBX placement
        private static readonly Vector3 PosSofa       = new Vector3(0, 0, 3.8f);
        private static readonly Vector3 PosSofaEast    = new Vector3(4.8f, 0, 0);
        private static readonly Vector3 PosCoffeeTable = new Vector3(0, 0, 2.5f);
        private static readonly Vector3 PosFloorLamp   = new Vector3(2.0f, 0, 3.8f);
        private static readonly Vector3 PosDesk        = new Vector3(-4, 0, 1.5f);
        private static readonly Vector3 PosChairMila   = new Vector3(-2.8f, 0, 1.5f);
        private static readonly Vector3 PosKitchen     = new Vector3(4.5f, 0, -2.5f);
        private static readonly Vector3 PosTableNikolai = new Vector3(-4.5f, 0, 4.2f);
        private static readonly Vector3 PosChairNikolai = new Vector3(-3.5f, 0, 4.2f);
        private static readonly Vector3 PosBookcaseNW  = new Vector3(-5.2f, 0, 4.5f);
        private static readonly Vector3 PosBookcaseNE  = new Vector3(5.2f, 0, 4.5f);
        private static readonly Vector3 PosBookcaseW   = new Vector3(-5.2f, 0, 0);
        private static readonly Vector3 PosServerRack  = new Vector3(5.2f, 0, -3.5f);

        // NPC positions
        private static readonly Vector3 PosSasha   = new Vector3(0, 0, 2.0f);
        private static readonly Vector3 PosMila    = new Vector3(-2.2f, 0, 1.5f);
        private static readonly Vector3 PosKirill  = new Vector3(3.0f, 0, -2.5f);
        private static readonly Vector3 PosNikolai = new Vector3(-3.0f, 0, 3.5f);
        private static readonly Vector3 PosStas    = new Vector3(1.5f, 0, -4f);
        private static readonly Vector3 PosKafka   = new Vector3(1, 0, -2.5f);
        private static readonly Vector3 PosPlayer  = new Vector3(0, 0, -3);

        // Art Bible §4.1 lighting values — exact match
        private static readonly Color ArtBibleSunColor = new Color(1.0f, 0.87f, 0.68f); // 3200K
        private const float ArtBibleSunIntensity = 1.2f;
        private static readonly Vector3 ArtBibleSunRotation = new Vector3(25, -45, 0);
        private static readonly Color ArtBibleAmbientColor = new Color(0.96f, 0.85f, 0.64f); // #F5D8A3
        private const float ArtBibleAmbientIntensity = 0.4f;
        private static readonly Color ArtBibleFogColor = new Color(0.96f, 0.85f, 0.64f);
        private const float ArtBibleFogDensity = 0.015f;

        // ============================================================
        // SPRINT 1: GREYBOX
        // Grey cubes, floor, walls, furniture silhouettes.
        // Goal: proportions, scale, navigation.
        // ============================================================

        [MenuItem("Afterhumans/v2/Sprint 1 — Greybox")]
        public static void Sprint1_Greybox()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // HIGH-5 fix: WIPE ENTIRE SCENE with explicit warning
            var roots = scene.GetRootGameObjects();
            if (roots.Length > 0)
                Debug.Log($"[BotanikaBuilder] WARNING: Clearing {roots.Length} root objects from scene (full rebuild)");
            foreach (var go in roots)
                Object.DestroyImmediate(go);

            var root = new GameObject("Botanika_Greybox");
            var grey = MakeGreyMaterial();
            var darkGrey = MakeMaterial("DarkGrey", new Color(0.35f, 0.35f, 0.35f));
            var green = MakeMaterial("Plant", new Color(0.25f, 0.45f, 0.2f));

            // === STRUCTURE: floor, HIGH walls, ceiling, panoramic windows ===
            float wallH = WallHeight;
            float halfH = wallH / 2f;

            MakeBox(root, "Floor", new Vector3(0, -0.05f, 0), new Vector3(12, 0.1f, 10), grey);

            // Ceiling (solid, visible, not transparent)
            MakeBox(root, "Ceiling", new Vector3(0, wallH, 0), new Vector3(12, 0.15f, 10), grey);

            // North wall: 2 pillars + panoramic window gap in between
            MakeBox(root, "Wall_North_L", new Vector3(-4.5f, halfH, 5), new Vector3(3, wallH, 0.2f), grey);
            MakeBox(root, "Wall_North_R", new Vector3(4.5f, halfH, 5), new Vector3(3, wallH, 0.2f), grey);
            MakeBox(root, "Wall_North_Top", new Vector3(0, wallH - 0.5f, 5), new Vector3(6, 1, 0.2f), grey); // lintel above window
            MakeBox(root, "Wall_North_Bot", new Vector3(0, 0.4f, 5), new Vector3(6, 0.8f, 0.2f), grey); // sill below window
            // Window = gap between sill and lintel (no geometry = see through to skybox)

            // South wall: doorway in center + windows on sides
            MakeBox(root, "Wall_South_L", new Vector3(-4.5f, halfH, -5), new Vector3(3, wallH, 0.2f), grey);
            MakeBox(root, "Wall_South_R", new Vector3(4.5f, halfH, -5), new Vector3(3, wallH, 0.2f), grey);
            MakeBox(root, "Wall_South_Top", new Vector3(0, wallH - 0.5f, -5), new Vector3(6, 1, 0.2f), grey);
            // Invisible wall blocking doorway exit (player can't leave yet)
            var doorBlock = MakeBox(root, "DoorBlock", new Vector3(0, halfH, -5), new Vector3(6, wallH, 0.2f), grey);
            doorBlock.GetComponent<Renderer>().enabled = false; // invisible but collider stays

            // East wall: 2 pillars + panoramic window
            MakeBox(root, "Wall_East_F", new Vector3(6, halfH, -3.5f), new Vector3(0.2f, wallH, 3), grey);
            MakeBox(root, "Wall_East_B", new Vector3(6, halfH, 3.5f), new Vector3(0.2f, wallH, 3), grey);
            MakeBox(root, "Wall_East_Top", new Vector3(6, wallH - 0.5f, 0), new Vector3(0.2f, 1, 4), grey);
            MakeBox(root, "Wall_East_Bot", new Vector3(6, 0.4f, 0), new Vector3(0.2f, 0.8f, 4), grey);

            // West wall: same pattern
            MakeBox(root, "Wall_West_F", new Vector3(-6, halfH, -3.5f), new Vector3(0.2f, wallH, 3), grey);
            MakeBox(root, "Wall_West_B", new Vector3(-6, halfH, 3.5f), new Vector3(0.2f, wallH, 3), grey);
            MakeBox(root, "Wall_West_Top", new Vector3(-6, wallH - 0.5f, 0), new Vector3(0.2f, 1, 4), grey);
            MakeBox(root, "Wall_West_Bot", new Vector3(-6, 0.4f, 0), new Vector3(0.2f, 0.8f, 4), grey);

            // Chandelier (simple box in center of ceiling)
            var chandelierMat = MakeMaterial("Chandelier", new Color(0.9f, 0.8f, 0.5f), 0.5f);
            MakeBox(root, "Chandelier", new Vector3(0, wallH - 1.5f, 0), new Vector3(1.5f, 0.3f, 1.5f), chandelierMat);
            // Light source at chandelier
            var chandLight = new GameObject("ChandelierLight");
            chandLight.transform.SetParent(root.transform);
            chandLight.transform.position = new Vector3(0, wallH - 1.8f, 0);
            var cl = chandLight.AddComponent<Light>();
            cl.type = LightType.Point;
            cl.color = new Color(1f, 0.9f, 0.7f);
            cl.intensity = 3f;
            cl.range = 12f;

            // === FURNITURE: Kenney FBX (mat=null → preserve original FBX materials) ===
            PlaceFbx(root, $"{FurnitureFbx}/loungeDesignSofa.fbx", "Sofa_Sasha",
                PosSofa, Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/loungeSofa.fbx", "Sofa_East",
                PosSofaEast, Quaternion.Euler(0, -90, 0), Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/tableCoffeeGlassSquare.fbx", "CoffeeTable",
                PosCoffeeTable, Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/lampRoundFloor.fbx", "FloorLamp",
                PosFloorLamp, Quaternion.identity, Vector3.one);

            // === NPC ZONES — Kenney FBX ===
            PlaceFbx(root, $"{FurnitureFbx}/desk.fbx", "Desk_Mila",
                PosDesk, Quaternion.Euler(0, 90, 0), Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/chairDesk.fbx", "Chair_Mila",
                PosChairMila, Quaternion.Euler(0, -90, 0), Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/kitchenCabinetCornerRound.fbx", "Kitchen_Counter",
                PosKitchen, Quaternion.Euler(0, -90, 0), Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/tableRound.fbx", "Table_Nikolai",
                PosTableNikolai, Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/chairCushion.fbx", "Chair_Nikolai",
                PosChairNikolai, Quaternion.Euler(0, 135, 0), Vector3.one);

            // === BOOKCASES — Kenney FBX ===
            PlaceFbx(root, $"{FurnitureFbx}/bookcaseOpen.fbx", "Bookcase_NW",
                PosBookcaseNW, Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/bookcaseOpen.fbx", "Bookcase_NE",
                PosBookcaseNE, Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/bookcaseOpenLow.fbx", "Bookcase_W",
                PosBookcaseW, Quaternion.Euler(0, 90, 0), Vector3.one);

            // === SERVER RACK ===
            MakeBox(root, "ServerRack", PosServerRack + Vector3.up * 0.8f, new Vector3(0.5f, 1.6f, 0.4f), darkGrey);

            // === PLANTS — Kenney nature-kit FBX (preserve original materials) ===
            PlaceFbx(root, $"{NatureFbx}/plant_bushLarge.fbx", "Plant_NW",
                new Vector3(-5.0f, 0, 3.0f), Quaternion.identity, Vector3.one * 1.3f);
            PlaceFbx(root, $"{NatureFbx}/plant_bushLarge.fbx", "Plant_NE",
                new Vector3(5.0f, 0, 3.0f), Quaternion.Euler(0, 90, 0), Vector3.one * 1.1f);
            PlaceFbx(root, $"{NatureFbx}/plant_bushLarge.fbx", "Plant_SW",
                new Vector3(-5.0f, 0, -3.5f), Quaternion.Euler(0, 45, 0), Vector3.one);
            PlaceFbx(root, $"{NatureFbx}/plant_bushDetailed.fbx", "Plant_SE",
                new Vector3(5.0f, 0, -1.0f), Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{NatureFbx}/plant_bushSmall.fbx", "Plant_W1",
                new Vector3(-5.3f, 0, -1.0f), Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{NatureFbx}/plant_bushSmall.fbx", "Plant_W2",
                new Vector3(-5.3f, 0, 1.0f), Quaternion.Euler(0, 60, 0), Vector3.one);
            PlaceFbx(root, $"{NatureFbx}/plant_bushSmall.fbx", "Plant_E1",
                new Vector3(5.3f, 0, 1.5f), Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{NatureFbx}/tree_palmShort.fbx", "Plant_N1",
                new Vector3(-2.0f, 0, 4.5f), Quaternion.identity, Vector3.one * 1.2f);
            PlaceFbx(root, $"{NatureFbx}/tree_palmShort.fbx", "Plant_N2",
                new Vector3(2.5f, 0, 4.5f), Quaternion.Euler(0, -45, 0), Vector3.one);
            PlaceFbx(root, $"{NatureFbx}/flower_redA.fbx", "Flower_1",
                new Vector3(-1.5f, 0, 0.5f), Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{NatureFbx}/flower_yellowA.fbx", "Flower_2",
                new Vector3(1.5f, 0, -0.5f), Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{NatureFbx}/flower_purpleA.fbx", "Flower_3",
                new Vector3(3.0f, 0, 4.0f), Quaternion.identity, Vector3.one);

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
            // LOW-4 fix: validate Player exists
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[BotanikaBuilder] Sprint 2: Player NOT FOUND — run Sprint 1 first!");
                return;
            }
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

            // NPC positions from shared constants
            SpawnNpc(root, "Sasha",   PosSasha,   180, "sasha",   3.0f, npcYellow);
            SpawnNpc(root, "Mila",    PosMila,     90, "mila",    2.5f, npcBlue);
            SpawnNpc(root, "Kirill",  PosKirill,  -90, "kirill",  2.5f, npcRed);
            SpawnNpc(root, "Nikolai", PosNikolai, 135, "nikolai", 2.5f, npcPurple);
            SpawnNpc(root, "Stas",    PosStas,      0, "stas",    2.5f, npcGreen);

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
        // SPRINT 3: LIGHTING
        // Warm sun, shadows, accent lights, skybox, post-FX
        // Goal: grey room transforms into warm golden hour greenhouse
        // ============================================================

        [MenuItem("Afterhumans/v2/Sprint 3 — Lighting")]
        public static void Sprint3_Lighting()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearRoot("Botanika_Lighting");

            var root = new GameObject("Botanika_Lighting");

            // Remove temp light from Sprint 1 (HIGH-2 fix: validate dependency)
            var tempLight = GameObject.Find("Sun_Temp");
            if (tempLight != null)
                Object.DestroyImmediate(tempLight);
            else if (GameObject.Find("Botanika_Greybox") == null)
                Debug.LogWarning("[BotanikaBuilder] Sprint 3: Botanika_Greybox not found — run Sprint 1 first");

            // === DIRECTIONAL LIGHT (Sun) — Art Bible §4.1 ===
            var sunGo = new GameObject("Sun_Directional");
            sunGo.transform.SetParent(root.transform);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            // CRITICAL-3 fix: exact Art Bible §4.1 values
            sun.color = ArtBibleSunColor;
            sun.intensity = ArtBibleSunIntensity;
            sun.transform.rotation = Quaternion.Euler(ArtBibleSunRotation);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            RenderSettings.sun = sun;

            // === RENDER SETTINGS — exact Art Bible §4.1 ===
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ArtBibleAmbientColor; // #F5D8A3
            RenderSettings.ambientIntensity = ArtBibleAmbientIntensity; // 0.4

            // Fog — Art Bible §4.1
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = ArtBibleFogDensity; // 0.015
            RenderSettings.fogColor = ArtBibleFogColor;

            // === SKYBOX ===
            var hdriPath = "Assets/_Project/Vendor/PolyHaven/kloppenheim_06_puresky_2k.hdr";
            var hdri = AssetDatabase.LoadAssetAtPath<Texture2D>(hdriPath);
            if (hdri != null)
            {
                var skyShader = Shader.Find("Skybox/Panoramic");
                if (skyShader != null)
                {
                    var skyMat = new Material(skyShader);
                    skyMat.SetTexture("_MainTex", hdri);
                    skyMat.SetFloat("_Exposure", 0.8f); // slightly dim so interior isn't washed out
                    RenderSettings.skybox = skyMat;
                }
                // Camera uses skybox
                var cam = FindPlayerCamera();
                if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
                Debug.Log("[BotanikaBuilder] HDRI Skybox applied");
            }
            else
            {
                Debug.LogWarning($"[BotanikaBuilder] HDRI not found at {hdriPath}");
            }

            // === ACCENT POINT LIGHTS ===
            // Near Sasha's sofa — warm reading light
            CreatePointLight(root, "Light_Sofa", new Vector3(1.8f, 2.2f, 3.5f),
                new Color(1f, 0.85f, 0.55f), 2.0f, 4f);
            // Nikolai's corner — dim warm
            CreatePointLight(root, "Light_Nikolai", new Vector3(-4.5f, 2.2f, 4f),
                new Color(1f, 0.78f, 0.45f), 1.5f, 3f);
            // Server rack — cool accent (contrast)
            CreatePointLight(root, "Light_Server", new Vector3(5.2f, 1.5f, -3.5f),
                new Color(0.6f, 0.75f, 1f), 1.0f, 2.5f);
            // Kitchen — warm
            CreatePointLight(root, "Light_Kitchen", new Vector3(4.5f, 2.0f, -2.5f),
                new Color(1f, 0.82f, 0.5f), 1.5f, 3f);

            // === POST-PROCESSING VOLUME ===
            SetupPostProcessing(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BotanikaBuilder] Sprint 3 LIGHTING done — sun, shadows, skybox, accents, post-FX");
        }

        private static Camera FindPlayerCamera()
        {
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in cams)
                if (c.CompareTag("MainCamera")) return c;
            return cams.Length > 0 ? cams[0] : null;
        }

        private static void CreatePointLight(GameObject parent, string name, Vector3 pos,
            Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None; // perf: only sun casts shadows
        }

        private static void SetupPostProcessing(GameObject parent)
        {
            var cam = FindPlayerCamera();
            if (cam == null) return;

            // Load or create Volume Profile
            var profilePath = "Assets/_Project/Settings/URP/VolumeProfiles/VP_Botanika_v2.asset";
            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                System.IO.Directory.CreateDirectory("Assets/_Project/Settings/URP/VolumeProfiles");
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            // Clear old overrides
            profile.components.Clear();

            // Add URP post-FX
            AddPostFxToProfile(profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            // Attach Volume to camera
            var volume = cam.GetComponent<UnityEngine.Rendering.Volume>();
            if (volume == null) volume = cam.gameObject.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.profile = profile;
            volume.priority = 1;

            Debug.Log("[BotanikaBuilder] Post-processing Volume applied to camera");
        }

        private static void AddPostFxToProfile(UnityEngine.Rendering.VolumeProfile profile)
        {
            // Bloom
            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.intensity.Override(0.5f);
            bloom.threshold.Override(1.0f);
            bloom.scatter.Override(0.7f);

            // Tonemapping ACES
            var tone = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
            tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);

            // Color Adjustments
            var color = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
            // LOW-2 fix: exact Art Bible §5 values
            color.saturation.Override(10f); // Art Bible: +10 ✓
            color.contrast.Override(5f);    // Art Bible: +5 (was 8)
            color.postExposure.Override(0.2f);

            // White Balance
            var wb = profile.Add<UnityEngine.Rendering.Universal.WhiteBalance>(true);
            wb.temperature.Override(15f);
            wb.tint.Override(-5f);

            // Vignette
            var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            vig.intensity.Override(0.22f);
            vig.smoothness.Override(0.5f);

            // Film Grain
            var grain = profile.Add<UnityEngine.Rendering.Universal.FilmGrain>(true);
            grain.intensity.Override(0.15f);
        }

        // ============================================================
        // SPRINT 4: ART PASS
        // Replace grey cubes with Kenney FBX, apply textures to NPC,
        // procedural textures on surfaces
        // ============================================================

        // Asset paths defined in shared constants above

        [MenuItem("Afterhumans/v2/Sprint 4 — Art Pass")]
        public static void Sprint4_Art()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // HIGH-3 fix: validate asset paths before proceeding
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Vendor/Kenney/furniture-kit"))
            {
                Debug.LogError("[BotanikaBuilder] Sprint 4: Kenney furniture-kit NOT FOUND. Run scripts/download-assets.sh first.");
                return;
            }

            ClearRoot("Botanika_Art");
            var root = new GameObject("Botanika_Art");

            // Generate procedural textures
            ProceduralTextures.ClearCache();
            var texTile = ProceduralTextures.TileFloor();
            var texPlaster = ProceduralTextures.PlasterWall();
            var texWood = ProceduralTextures.WoodFurniture();
            var texFabric = ProceduralTextures.Fabric();

            // === RETEXTURE GREYBOX SURFACES ===
            var greybox = GameObject.Find("Botanika_Greybox");
            if (greybox != null)
            {
                RetextureByName(greybox, "Floor", texTile, new Color(0.75f, 0.58f, 0.42f), 6f);
                RetextureByName(greybox, "Wall_", texPlaster, new Color(0.85f, 0.75f, 0.60f), 3f);
                RetextureByName(greybox, "GlassCeiling", null, new Color(0.75f, 0.88f, 0.82f, 0.3f), 1f, true); // more visible glass
                RetextureByName(greybox, "Sofa_", texFabric, new Color(0.55f, 0.32f, 0.22f), 2f);
                RetextureByName(greybox, "Desk_", texWood, new Color(0.65f, 0.45f, 0.28f), 2f);
                RetextureByName(greybox, "Kitchen_", texWood, new Color(0.50f, 0.38f, 0.25f), 2f);
                RetextureByName(greybox, "Table_", texWood, new Color(0.60f, 0.42f, 0.26f), 2f);
                RetextureByName(greybox, "Chair_", texFabric, new Color(0.45f, 0.30f, 0.20f), 2f);
                RetextureByName(greybox, "Bookcase_", texWood, new Color(0.42f, 0.28f, 0.16f), 2f);
                RetextureByName(greybox, "FloorLamp", texWood, new Color(0.35f, 0.25f, 0.15f), 1f);
                RetextureByName(greybox, "CoffeeTable", texWood, new Color(0.50f, 0.35f, 0.22f), 2f);
                RetextureByName(greybox, "ServerRack", null, new Color(0.25f, 0.25f, 0.28f), 1f);
                RetextureByName(greybox, "Plant_", null, new Color(0.22f, 0.48f, 0.18f), 1f);
                Debug.Log("[BotanikaBuilder] Greybox surfaces retextured");
            }

            // === RETEXTURE NPC with Kenney character textures ===
            var gameplay = GameObject.Find("Botanika_Gameplay");
            if (gameplay != null)
            {
                ApplyCharacterTexture(gameplay, "NPC_Sasha", "texture-a.png");
                ApplyCharacterTexture(gameplay, "NPC_Mila", "texture-c.png");
                ApplyCharacterTexture(gameplay, "NPC_Kirill", "texture-e.png");
                ApplyCharacterTexture(gameplay, "NPC_Nikolai", "texture-g.png");
                ApplyCharacterTexture(gameplay, "NPC_Stas", "texture-i.png");
                Debug.Log("[BotanikaBuilder] NPC textures applied");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BotanikaBuilder] Sprint 4 ART done — textures on surfaces + NPC skins");
        }

        private static void RetextureByName(GameObject parent, string nameContains,
            Texture2D texture, Color tint, float tileScale, bool transparent = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            foreach (var rend in parent.GetComponentsInChildren<Renderer>(true))
            {
                if (!rend.gameObject.name.Contains(nameContains)) continue;

                var mat = new Material(shader);
                if (transparent)
                {
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetFloat("_Blend", 0);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                }
                mat.SetColor("_BaseColor", tint);
                if (texture != null)
                {
                    mat.SetTexture("_BaseMap", texture);
                    mat.SetTextureScale("_BaseMap", new Vector2(tileScale, tileScale));
                }
                mat.SetFloat("_Smoothness", 0.15f);
                rend.sharedMaterial = mat;
            }
        }

        private static void ApplyCharacterTexture(GameObject parent, string npcName, string texFileName)
        {
            var npc = parent.transform.Find(npcName);
            if (npc == null) return;

            var texPath = $"{CharacterTex}/{texFileName}";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                Debug.LogWarning($"[BotanikaBuilder] Texture not found: {texPath}");
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.2f);

            foreach (var rend in npc.GetComponentsInChildren<Renderer>(true))
                rend.sharedMaterial = mat;
        }

        // ============================================================
        // SPRINT 5: POLISH — extra props only (main furniture now in Sprint 1)
        // ============================================================

        [MenuItem("Afterhumans/v2/Sprint 5 — Polish")]
        public static void Sprint5_Polish()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearRoot("Botanika_Polish");

            var root = new GameObject("Botanika_Polish");

            // Extra props — books, rug, additional details
            PlaceFbx(root, $"{FurnitureFbx}/books.fbx", "Books_Table",
                PosCoffeeTable + new Vector3(0.2f, 0.5f, 0), Quaternion.Euler(0, 25, 0), Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/books.fbx", "Books_Bookcase",
                PosBookcaseNW + new Vector3(0, 0.8f, 0), Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/rugRounded.fbx", "Rug",
                PosCoffeeTable + Vector3.up * 0.01f, Quaternion.identity, new Vector3(2, 1, 1.5f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BotanikaBuilder] Sprint 5 POLISH done — extra props (books, rug)");
        }

        /// <summary>
        /// Place Kenney FBX model. If mat is null, PRESERVES original FBX materials.
        /// CRITICAL-2 fix: don't destroy embedded FBX textures unless explicitly overriding.
        /// </summary>
        private static void PlaceFbx(GameObject parent, string fbxPath, string name,
            Vector3 pos, Quaternion rot, Vector3 scale, Material mat = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null)
            {
                Debug.LogError($"[BotanikaBuilder] FBX not found: {fbxPath}");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (go == null) return;
            go.name = name;
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            go.isStatic = true;
            // Only override materials if explicitly provided
            if (mat != null)
            {
                foreach (var rend in go.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = new Material[rend.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                    rend.sharedMaterials = mats;
                }
            }
            ColliderHelper.AddSimpleCollider(go);
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
