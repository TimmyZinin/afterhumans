using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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
        // BOTANIKA NAVE GRID (G-01) — SINGLE SOURCE OF TRUTH
        // ============================================================
        // Coords: Unity left-handed, meters. +Z = NORTH = forward.
        // The space is a LARGE greenhouse nave (oranzhereya-nef), NOT a room.
        //
        //   Floor:   X  -7 .. +7  (width 14)    Z  -14 .. +14  (length 28)
        //   Center corridor ~7 m clear down the middle.
        //   Vault:   gable glass roof. Apex Y = 8 at X = 0.
        //            Eaves  Y = 4 at X = +/-7. ONE mesh per slope (not 96 panels).
        //   Columns: 4 steel columns at X = +/-3.5, Z = +/-5 (visual r~0.3).
        //   Walls:   side glass walls at X = +/-7.
        //            solid NORTH wall at Z = +14 (server zone backdrop).
        //            SOUTH entrance wall at Z = -14 (with central doorway).
        //   Spawn:   Player + Kafka at SOUTH, Z = -12, facing NORTH (+Z).
        //   Door:    locked gate to City at NORTH, Z = +13.
        //
        //   NPC zones on this grid:
        //     Sasha  sofa, center   Z = -2
        //     Mila   west           X = -4,  Z = +1
        //     Kirill kitchen, east  X = +4,  Z = -6
        //     Nikolai far center    Z = +8   (gate keeper)
        //     Stas   near door      Z = +10..11
        //     Server rack east      X = +5,  Z = +2
        // ============================================================

        // Nave shell dimensions
        private const float NaveWidth   = 14f;   // X span: -7 .. +7
        private const float NaveHalfW   = 7f;    // |X| extent
        private const float NaveLength  = 28f;   // Z span: -14 .. +14
        private const float NaveHalfL   = 14f;   // |Z| extent
        private const float VaultApex   = 8f;    // roof height at X = 0
        private const float EaveHeight  = 4f;    // roof height at X = +/-7 (side wall top)

        // Legacy alias — later art sprints (Sprint 8 CreateWindowGlass) read this.
        // Maps to eave height of the new nave so those sprints still compile.
        private const float WallHeight  = EaveHeight;

        // Column grid
        private const float ColumnX     = 3.5f;  // |X| of columns
        private const float ColumnZ     = 5f;    // |Z| of columns
        private const float ColumnVisR  = 0.3f;  // visual radius
        private const float ColumnColR  = 0.25f; // collider radius

        // Z anchors
        private const float SpawnZ      = -12f;  // player + Kafka spawn (south)
        private const float DoorZ       = 13f;   // locked gate to City (north)

        // Asset paths
        private const string FurnitureFbx = "Assets/_Project/Vendor/Kenney/furniture-kit/Models/FBX format";
        private const string NatureFbx = "Assets/_Project/Vendor/Kenney/nature-kit/Models/FBX format";
        private const string CharacterFbx = "Assets/_Project/Vendor/Kenney/blocky-characters/Models/FBX format";
        private const string CharacterTex = CharacterFbx + "/Textures";

        // Furniture positions — recomputed onto the NAVE grid (used by later art sprints)
        private static readonly Vector3 PosSofa        = new Vector3(0, 0, -2f);     // Sasha sofa, center
        private static readonly Vector3 PosSofaEast     = new Vector3(4.5f, 0, -3f);
        private static readonly Vector3 PosCoffeeTable  = new Vector3(0, 0, -3.2f);
        private static readonly Vector3 PosFloorLamp    = new Vector3(2.0f, 0, -2f);
        private static readonly Vector3 PosDesk         = new Vector3(-4.2f, 0, 1f);  // Mila west
        private static readonly Vector3 PosChairMila    = new Vector3(-3.4f, 0, 1f);
        private static readonly Vector3 PosKitchen      = new Vector3(4.2f, 0, -6f);  // Kirill kitchen east
        private static readonly Vector3 PosTableNikolai = new Vector3(-1.0f, 0, 8f);  // Nikolai far center
        private static readonly Vector3 PosChairNikolai = new Vector3(-0.2f, 0, 8f);
        private static readonly Vector3 PosBookcaseNW   = new Vector3(-5.5f, 0, 8f);
        private static readonly Vector3 PosBookcaseNE   = new Vector3(5.5f, 0, 8f);
        private static readonly Vector3 PosBookcaseW    = new Vector3(-5.8f, 0, 0);
        private static readonly Vector3 PosServerRack   = new Vector3(5f, 0, 2f);     // server east passage

        // NPC positions — recomputed onto the NAVE grid
        private static readonly Vector3 PosSasha   = new Vector3(0, 0, -2f);     // sofa, center
        private static readonly Vector3 PosMila    = new Vector3(-3.6f, 0, 1f);  // west
        private static readonly Vector3 PosKirill  = new Vector3(3.4f, 0, -6f);  // kitchen, east
        private static readonly Vector3 PosNikolai = new Vector3(-0.8f, 0, 8f);  // far center, gate keeper
        private static readonly Vector3 PosStas    = new Vector3(1.5f, 0, 10.5f);// near door
        private static readonly Vector3 PosKafka   = new Vector3(1.2f, 0, SpawnZ + 0.5f); // beside spawn
        private static readonly Vector3 PosPlayer  = new Vector3(0, 0, SpawnZ);  // south spawn

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
            BuildGreybox();
        }

        /// <summary>
        /// Builds the LARGE Botanika nave greybox (M1, G-02). Pure grey geometry,
        /// correct scale, NO art. Headless-callable via -executeMethod
        /// (Afterhumans.EditorTools.BotanikaBuilder.BuildGreybox) — no MenuItem-only
        /// state, no editor-window dependencies.
        /// </summary>
        public static void BuildGreybox()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // WIPE ENTIRE SCENE — full rebuild from the GRID.
            var roots = scene.GetRootGameObjects();
            if (roots.Length > 0)
                Debug.Log($"[BotanikaBuilder] WARNING: Clearing {roots.Length} root objects from scene (full rebuild)");
            foreach (var go in roots)
                Object.DestroyImmediate(go);

            var root = new GameObject("Botanika_Greybox");
            var grey      = MakeGreyMaterial();                                    // floor / walls
            var steelGrey = MakeMaterial("Steel", new Color(0.38f, 0.40f, 0.42f), 0.3f); // columns
            var glassGrey = MakeMaterial("GlassPlaceholder", new Color(0.62f, 0.66f, 0.64f), 0.2f, doubleSided: true); // vault + glass walls (double-sided so the roof is visible from inside)
            var darkGrey  = MakeMaterial("DarkGrey", new Color(0.30f, 0.30f, 0.32f)); // server / door

            // ===== FLOOR — 28 (Z) x 14 (X), center at origin, top at Y=0 =====
            var floor = MakeBox(root, "Floor", new Vector3(0, -0.05f, 0),
                new Vector3(NaveWidth, 0.1f, NaveLength), grey);
            // BoxCollider from the primitive is fine, but the plan asks for a MeshCollider
            // on the walkable floor — swap to a convex-off MeshCollider for accuracy.
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            var floorMc = floor.AddComponent<MeshCollider>();
            floorMc.convex = false;
            ColliderHelper.MarkStaticProp(floor);

            // ===== SIDE GLASS WALLS at X = +/-7 (eave height 4 m) =====
            float sideH = EaveHeight;
            var wallE = MakeBox(root, "Wall_GlassEast", new Vector3(NaveHalfW, sideH * 0.5f, 0),
                new Vector3(0.15f, sideH, NaveLength), glassGrey);
            var wallW = MakeBox(root, "Wall_GlassWest", new Vector3(-NaveHalfW, sideH * 0.5f, 0),
                new Vector3(0.15f, sideH, NaveLength), glassGrey);
            // Glass side walls let sunlight through too.
            wallE.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wallW.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // ===== SOLID NORTH WALL at Z = +14 (server-zone backdrop) =====
            // Full gable-height wall so the apex is closed off behind the gate.
            MakeBox(root, "Wall_North", new Vector3(0, VaultApex * 0.5f, NaveHalfL),
                new Vector3(NaveWidth, VaultApex, 0.2f), grey);

            // ===== SOUTH ENTRANCE WALL at Z = -14 (doorway gap in center) =====
            // Two side panels + lintel; central 4 m gap is the entrance.
            float doorGapHalf = 2f; // 4 m wide doorway
            float sidePanelW = (NaveHalfW - doorGapHalf); // each side panel width
            float sidePanelCenterX = doorGapHalf + sidePanelW * 0.5f;
            MakeBox(root, "Wall_South_L", new Vector3(-sidePanelCenterX, sideH * 0.5f, -NaveHalfL),
                new Vector3(sidePanelW, sideH, 0.2f), grey);
            MakeBox(root, "Wall_South_R", new Vector3(sidePanelCenterX, sideH * 0.5f, -NaveHalfL),
                new Vector3(sidePanelW, sideH, 0.2f), grey);
            MakeBox(root, "Wall_South_Lintel", new Vector3(0, sideH - 0.4f, -NaveHalfL),
                new Vector3(doorGapHalf * 2f, 0.8f, 0.2f), grey);
            // Invisible blocker across the south doorway — player cannot leave south.
            var southBlock = MakeBox(root, "SouthDoorBlock", new Vector3(0, sideH * 0.5f, -NaveHalfL),
                new Vector3(doorGapHalf * 2f, sideH, 0.2f), grey);
            southBlock.GetComponent<Renderer>().enabled = false; // collider stays, invisible

            // ===== GABLE GLASS VAULT — ONE mesh per slope (apex Y=8 @ X=0) =====
            BuildVaultSlope(root, "Vault_East", +1, glassGrey);  // +X slope
            BuildVaultSlope(root, "Vault_West", -1, glassGrey);  // -X slope
            // Gable end caps (triangular) — close the vault at north/south ends.
            BuildGableEnd(root, "Gable_North", NaveHalfL, glassGrey);
            BuildGableEnd(root, "Gable_South", -NaveHalfL, glassGrey);

            // ===== 4 STEEL COLUMNS at X=+/-3.5, Z=+/-5 (full height to apex region) =====
            // Column rises from floor toward the vault; height tied to local roof Y.
            MakeColumn(root, "Column_NE", new Vector3( ColumnX, 0,  ColumnZ), steelGrey);
            MakeColumn(root, "Column_NW", new Vector3(-ColumnX, 0,  ColumnZ), steelGrey);
            MakeColumn(root, "Column_SE", new Vector3( ColumnX, 0, -ColumnZ), steelGrey);
            MakeColumn(root, "Column_SW", new Vector3(-ColumnX, 0, -ColumnZ), steelGrey);

            // ===== SERVER RACK placeholder (east passage, far north) =====
            MakeBox(root, "ServerRack", PosServerRack + Vector3.up * 0.9f,
                new Vector3(0.6f, 1.8f, 0.5f), darkGrey);

            // ===== LOCKED DOOR placeholder — gate to City at NORTH Z=+13 =====
            var door = MakeBox(root, "DoorToCity_Placeholder", new Vector3(0, 1.4f, DoorZ),
                new Vector3(2.4f, 2.8f, 0.15f), darkGrey);
            // Solid collider = locked (player can't pass until Nikolai opens it in Sprint 2/7).
            ColliderHelper.MarkStaticProp(door);

            // ===== PLAYER + camera (spawn south, facing north +Z) =====
            SetupPlayer();

            // ===== KAFKA spawn marker (beside player, south) =====
            // Greybox: a small grey capsule placeholder; Sprint 2 replaces with model.
            var kafkaMarker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            kafkaMarker.name = "Kafka_SpawnMarker";
            kafkaMarker.transform.SetParent(root.transform, worldPositionStays: false);
            kafkaMarker.transform.position = PosKafka + new Vector3(0, 0.45f, 0);
            kafkaMarker.transform.localScale = new Vector3(0.35f, 0.45f, 0.6f);
            kafkaMarker.GetComponent<Renderer>().sharedMaterial = darkGrey;
            Object.DestroyImmediate(kafkaMarker.GetComponent<Collider>());

            // ===== SCALE REFERENCE — human silhouette 1.8 m (greybox only) =====
            // A standing 1.8 m capsule near the gate zone gives the eye an
            // unambiguous human-scale anchor inside the 28 m nave.
            var scaleRef = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            scaleRef.name = "ScaleRef_Human_1m8";
            scaleRef.transform.SetParent(root.transform, worldPositionStays: false);
            scaleRef.transform.position = new Vector3(-1.5f, 0.9f, 6f); // near Nikolai zone
            scaleRef.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f); // 1.8 m tall
            scaleRef.GetComponent<Renderer>().sharedMaterial = steelGrey;
            Object.DestroyImmediate(scaleRef.GetComponent<Collider>());

            // ===== MINIMAL LIGHT (just to see the greybox) =====
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

            int objCount = root.GetComponentsInChildren<Transform>(true).Length;
            Debug.Log($"[BotanikaBuilder] Sprint 1 GREYBOX done — LARGE nave " +
                      $"{NaveWidth}x{NaveLength}m, vault apex {VaultApex}m, 4 columns, " +
                      $"glass walls, north/south walls, player+Kafka spawn @ Z={SpawnZ}, " +
                      $"locked door @ Z={DoorZ}. Object count under root: {objCount}");
        }

        /// <summary>
        /// Builds ONE roof slope as a single quad mesh (NOT 96 panels).
        /// side = +1 → +X slope (eave at X=+7), side = -1 → -X slope (eave at X=-7).
        /// Apex line is at X=0, Y=VaultApex; eave line at X=+/-7, Y=EaveHeight.
        /// </summary>
        private static void BuildVaultSlope(GameObject parent, string name, int side, Material mat)
        {
            float ax = 0f;            // apex X
            float ay = VaultApex;     // apex Y
            // Eave overlaps slightly INSIDE and BELOW the 4 m side-wall top so the
            // wall↔roof seam is sealed (kills the sky sliver / corner gap QA flagged).
            float ex = side * (NaveHalfW - 0.1f); // eave X — behind wall inner face
            float ey = EaveHeight - 0.3f;         // eave Y — below wall top (overlap)
            float z0 = -NaveHalfL;    // south edge
            float z1 = NaveHalfL;     // north edge

            var verts = new Vector3[]
            {
                new Vector3(ax, ay, z0), // 0 apex-south
                new Vector3(ax, ay, z1), // 1 apex-north
                new Vector3(ex, ey, z1), // 2 eave-north
                new Vector3(ex, ey, z0), // 3 eave-south
            };

            // Wind so the normal faces DOWN/INWARD (visible from inside the nave).
            // For +X slope inside-normal points toward -X/+down; flip order by side.
            int[] tris = side > 0
                ? new[] { 0, 1, 2, 0, 2, 3 }
                : new[] { 0, 2, 1, 0, 3, 2 };

            var uvs = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(1, 1), new Vector2(1, 0),
            };

            var mesh = new Mesh { name = name + "_Mesh" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var vr = go.AddComponent<MeshRenderer>();
            vr.sharedMaterial = mat;
            // Glass roof must NOT cast shadows — sunlight pours THROUGH the vault
            // into the nave (otherwise the closed shell blacks out the interior).
            vr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Roof collider so the player/camera cannot poke through the vault.
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = false;
            ColliderHelper.MarkStaticProp(go);
        }

        /// <summary>
        /// Triangular gable end cap (one mesh) at a given Z, closing the vault
        /// between the side-wall eaves and the apex.
        /// </summary>
        private static void BuildGableEnd(GameObject parent, string name, float z, Material mat)
        {
            // Eave verts overlap the side walls (match BuildVaultSlope) so the
            // triangular gable cap fully seals the end with no gap to the sky.
            var verts = new Vector3[]
            {
                new Vector3(-(NaveHalfW - 0.1f), EaveHeight - 0.3f, z), // 0 west eave
                new Vector3( 0f,                 VaultApex,         z), // 1 apex
                new Vector3( (NaveHalfW - 0.1f), EaveHeight - 0.3f, z), // 2 east eave
            };
            // Face inward: north cap (z>0) normal points -Z, south cap (z<0) points +Z.
            int[] tris = z > 0 ? new[] { 0, 1, 2 } : new[] { 0, 2, 1 };
            var uvs = new Vector2[] { new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(1, 0) };

            var mesh = new Mesh { name = name + "_Mesh" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var gr = go.AddComponent<MeshRenderer>();
            gr.sharedMaterial = mat;
            gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // glass gable lets light through
            ColliderHelper.MarkStaticProp(go);
        }

        /// <summary>
        /// Steel column: visual cylinder (r=ColumnVisR) with a CapsuleCollider
        /// (r=ColumnColR). Height reaches the local roof Y at the column's X
        /// (linear interp apex→eave), so it visually meets the vault.
        /// </summary>
        private static void MakeColumn(GameObject parent, string name, Vector3 basePos, Material mat)
        {
            // Local roof height at |X|=ColumnX (linear apex@X0 → eave@X7).
            float t = Mathf.Abs(ColumnX) / NaveHalfW;
            float roofY = Mathf.Lerp(VaultApex, EaveHeight, t);
            float h = roofY; // full height floor → roof

            var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col.name = name;
            col.transform.SetParent(parent.transform, worldPositionStays: false);
            col.transform.position = basePos + new Vector3(0, h * 0.5f, 0);
            // Unity cylinder is 2 m tall at scale 1 → scale Y by h/2.
            col.transform.localScale = new Vector3(ColumnVisR * 2f, h * 0.5f, ColumnVisR * 2f);
            col.GetComponent<Renderer>().sharedMaterial = mat;

            // Replace default capsule/mesh collider with a tighter CapsuleCollider.
            // CapsuleCollider.radius is LOCAL; world radius = local * max(scaleX,scaleZ).
            // scaleX = scaleZ = ColumnVisR*2, so local = desiredWorld / (ColumnVisR*2).
            Object.DestroyImmediate(col.GetComponent<Collider>());
            var cc = col.AddComponent<CapsuleCollider>();
            cc.direction = 1;        // Y axis
            cc.radius = ColumnColR / (ColumnVisR * 2f); // → world radius ≈ ColumnColR (0.25)
            cc.height = 2f;          // local height (cylinder is 2 units tall pre-scale)
            cc.center = Vector3.zero;
            ColliderHelper.MarkStaticProp(col);
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
                // Sprint 10: debug HUD OFF for production look
                var piSo = new SerializedObject(pi);
                var debugProp = piSo.FindProperty("showDebugHud");
                if (debugProp != null) { debugProp.boolValue = false; piSo.ApplyModifiedPropertiesWithoutUndo(); }
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
            door.transform.position = new Vector3(0, 1.4f, DoorZ); // NORTH gate to City, Z = +13
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
            kafka.transform.position = PosKafka;

            var kafkaFbx = "Assets/_Project/Models/kafka_corgi.fbx";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(kafkaFbx);
            if (prefab != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                model.name = "KafkaModel";
                model.transform.SetParent(kafka.transform, worldPositionStays: false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0, 90, 0); // Tripo model faces -X, flip to +Z
                model.transform.localScale = Vector3.one * 3f; // 0.30m real → 0.90m game scale (knee-height to NPC)

                // Build AnimatorController: Idle (rest pose) ↔ Walk (clip)
                var animator = model.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    var ctrlPath = "Assets/_Project/Models/KafkaAnimator.controller";
                    // Delete old controller if exists
                    if (AssetDatabase.LoadAssetAtPath<Object>(ctrlPath) != null)
                        AssetDatabase.DeleteAsset(ctrlPath);

                    var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

                    // Bool parameter for walk/idle switch
                    controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);

                    var sm = controller.layers[0].stateMachine;

                    // Idle state — no motion = bind/rest pose (all paws on ground)
                    var idleState = sm.AddState("Idle");
                    sm.defaultState = idleState;

                    // Walk state — load clip from FBX
                    var walkState = sm.AddState("Walk");
                    var clips = AssetDatabase.LoadAllAssetsAtPath(kafkaFbx)
                        .OfType<AnimationClip>()
                        .Where(c => !c.name.StartsWith("__preview__"))
                        .ToArray();

                    if (clips.Length > 0)
                    {
                        walkState.motion = clips[0];
                        Debug.Log($"[BotanikaBuilder] Kafka: Walk clip '{clips[0].name}' assigned ({clips.Length} clips total)");
                    }
                    else
                    {
                        Debug.LogWarning("[BotanikaBuilder] Kafka: no animation clips found in FBX!");
                    }

                    // Transitions: Idle → Walk (IsWalking=true), Walk → Idle (IsWalking=false)
                    var toWalk = idleState.AddTransition(walkState);
                    toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
                    toWalk.hasExitTime = false;
                    toWalk.duration = 0.15f;

                    var toIdle = walkState.AddTransition(idleState);
                    toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
                    toIdle.hasExitTime = false;
                    toIdle.duration = 0.15f;

                    animator.runtimeAnimatorController = controller;
                    AssetDatabase.SaveAssets();
                    Debug.Log("[BotanikaBuilder] Kafka: AnimatorController created (Idle ↔ Walk)");
                }
                else
                {
                    Debug.LogWarning("[BotanikaBuilder] Kafka: no Animator found on model!");
                }

                Debug.Log("[BotanikaBuilder] Kafka: model loaded");
            }
            else
            {
                Debug.LogWarning($"[BotanikaBuilder] Kafka FBX not found at {kafkaFbx}, using capsule fallback");
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "KafkaBody";
                body.transform.SetParent(kafka.transform, worldPositionStays: false);
                body.transform.localPosition = Vector3.zero;
                body.transform.localScale = new Vector3(0.25f, 0.2f, 0.4f);
                body.transform.localRotation = Quaternion.Euler(0, 0, 90);
                var kafkaMat = MakeMaterial("Kafka", new Color(0.15f, 0.13f, 0.12f));
                body.GetComponent<Renderer>().sharedMaterial = kafkaMat;
                Object.DestroyImmediate(body.GetComponent<Collider>());
            }

            kafka.AddComponent<Afterhumans.Kafka.KafkaFollowSimple>();
            Debug.Log($"[BotanikaBuilder] Kafka spawned at {PosKafka}");
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

            // === DIRECTIONAL LIGHT (Sun) — low sunset key, the HERO of the scene ===
            var sunGo = new GameObject("Sun_Directional");
            sunGo.transform.SetParent(root.transform);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.78f, 0.50f);  // warm 3200K sunset
            sun.intensity = 1.5f;                        // dominant, no blown walls
            // ~30° — pours from ABOVE-along the nave (through the vault), still
            // angled enough for long shadows. (Tim-proxy: light read as a side
            // lamp; the greenhouse needs it coming from overhead through glass.)
            sun.transform.rotation = Quaternion.Euler(30f, -22f, 0f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;

            // RIM / back light — cool, low, from behind to catch column edges
            // (Tim-proxy: columns read as flat cardboard tubes, no edge light).
            var rimGo = new GameObject("Light_Rim");
            rimGo.transform.SetParent(root.transform);
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.6f, 0.72f, 0.95f); // cool counter
            rim.intensity = 0.5f;
            rim.transform.rotation = Quaternion.Euler(12f, 150f, 0f); // from far end, low
            rim.shadows = LightShadows.None;

            // === RENDER SETTINGS — gradient ambient = warm sky / COOL shadows ===
            // (Flat ambient flattened everything; gradient gives the warm/cool
            //  contrast the AAA QA flagged as missing.)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.42f, 0.28f);     // warm bounce from above
            RenderSettings.ambientEquatorColor = new Color(0.30f, 0.28f, 0.26f); // neutral mid
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.18f, 0.24f);  // COOL shadow fill
            RenderSettings.ambientIntensity = 1.0f;

            // Fog — denser warm haze for depth/atmosphere down the long nave
            // (AAA QA: interior air too dry, depth only geometric).
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.032f; // denser dusty air in ALL shots (Tim-proxy)
            RenderSettings.fogColor = new Color(0.80f, 0.63f, 0.42f); // warm sunset haze

            // === SKYBOX ===
            // Sprint 4: warm sunset interior HDRI from 3d-artist sourcing.
            var hdriPath = "Assets/_Project/Vendor/PolyHaven/HDRI/sunset_botanika_4k.exr";
            var hdri = AssetDatabase.LoadAssetAtPath<Texture2D>(hdriPath);
            if (hdri == null)
            {
                hdriPath = "Assets/_Project/Vendor/PolyHaven/rogland_sunset_2k.hdr";
                hdri = AssetDatabase.LoadAssetAtPath<Texture2D>(hdriPath);
            }
            if (hdri != null)
            {
                var skyShader = Shader.Find("Skybox/Panoramic");
                if (skyShader != null)
                {
                    var skyMat = new Material(skyShader);
                    skyMat.SetTexture("_MainTex", hdri);
                    skyMat.SetFloat("_Exposure", 0.32f); // dim sky so edges don't clip to white
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

            // === ACCENT POINT LIGHTS — the SUN is the key; these are low accents
            // only (previously they blew the whole nave to cream). ===
            var warm = new Color(1f, 0.82f, 0.52f);
            var warmDeep = new Color(1f, 0.74f, 0.42f);
            var cool = new Color(0.62f, 0.74f, 0.92f); // less saturated blue (Tim-proxy: random blue pools)
            // Sasha sofa (center, south) — small warm pool
            CreatePointLight(root, "Light_Sofa", new Vector3(0f, 2.4f, -2f), warm, 1.6f, 5f);
            // Kirill kitchen (east)
            CreatePointLight(root, "Light_Kitchen", new Vector3(3.4f, 2.4f, -6f), warm, 1.3f, 4.5f);
            // Nikolai far center — warm GLOW at the far end that beckons down the
            // dark entry POV (forward shot).
            CreatePointLight(root, "Light_Nikolai", new Vector3(-0.8f, 2.6f, 8f), warmDeep, 2.8f, 9f);
            // Server rack (east passage) — FOCUSED cool accent = the one cold note
            // against the warm hall (AAA QA: cold was too diffuse).
            CreatePointLight(root, "Light_Server", new Vector3(5f, 3.2f, 2f), cool, 3.8f, 5.5f); // raised off the floor
            // Warm fill at the player spawn — lift the entry POV out of near-black
            // so the front columns read (keep it moodier than the rest).
            CreatePointLight(root, "Light_Spawn", new Vector3(0f, 2.5f, -10.5f), warm, 3f, 13f);

            // === GOD RAYS — deferred to art pass (AP-01) ===
            // Additive-quad fake shafts read as flat blown-white slabs in URP;
            // believable god-rays need a volumetric raymarch shader, which ships
            // with the glass-vault material work. Atmosphere here comes from the
            // dense warm fog + sun-from-above instead.
            // CreateLightShafts(root);

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

        /// <summary>
        /// Fakes volumetric god-rays (URP has no built-in light shafts without
        /// HDRP): additive, faintly warm, soft-edged quad "beams" descending from
        /// the vault to the floor at the SUN's angle, scattered down the nave.
        /// Each beam is a crossed pair of quads so it reads as a volume from any
        /// camera angle. Combined with the warm fog this gives dusty sunbeams.
        /// </summary>
        private static void CreateLightShafts(GameObject parent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.name = "GodRay_Additive";
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(1f, 0.82f, 0.5f, 1f));
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
            // Transparent + ADDITIVE blend (glow, never darkens).
            mat.SetFloat("_Surface", 1f);     // Transparent
            mat.SetFloat("_Blend", 2f);       // Additive
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3100;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            var shaftRoot = new GameObject("GodRays");
            shaftRoot.transform.SetParent(parent.transform);

            // Beams angled to match the sun (down-and-along the nave), placed
            // between column pairs where light would break through the vault.
            var beamSpots = new[]
            {
                new Vector3(-2.0f, 4.0f, -4f),
                new Vector3( 2.2f, 4.0f,  1f),
                new Vector3(-1.4f, 4.0f,  6f),
                new Vector3( 1.0f, 4.0f, -8f),
            };
            var beamRot = Quaternion.Euler(62f, -18f, 0f); // steep shaft, sun-aligned
            int i = 0;
            foreach (var spot in beamSpots)
            {
                // Crossed quads = pseudo-volume beam.
                for (int k = 0; k < 2; k++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    q.name = $"Shaft_{i}_{k}";
                    Object.DestroyImmediate(q.GetComponent<Collider>());
                    q.transform.SetParent(shaftRoot.transform);
                    q.transform.position = spot;
                    q.transform.rotation = beamRot * Quaternion.Euler(0f, k * 90f, 0f);
                    q.transform.localScale = new Vector3(1.6f, 9f, 1f); // narrow, tall shaft
                    var r = q.GetComponent<Renderer>();
                    r.sharedMaterial = mat;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                i++;
            }
            Debug.Log($"[BotanikaBuilder] God rays: {beamSpots.Length} dusty sunbeams placed");
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
            // Bloom — stronger for stylized glow on lights/emissive
            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.intensity.Override(1.15f);  // glow on far-end light + warm highlights
            bloom.threshold.Override(0.7f);   // lower → highlights bloom (AAA QA: bloom not reading)
            bloom.scatter.Override(0.62f); // tighter core (was 0.8 — far-end glow too cottony)
            bloom.tint.Override(new Color(1f, 0.9f, 0.72f)); // warm bloom

            // Tonemapping ACES
            var tone = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
            tone.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);

            // Color Adjustments
            var color = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
            // LOW-2 fix: exact Art Bible §5 values
            color.saturation.Override(20f);  // richer sunset amber
            color.contrast.Override(12f);    // tonal punch
            color.postExposure.Override(-0.32f); // kill remaining edge clipping
            color.colorFilter.Override(new Color(1f, 0.96f, 0.9f)); // warm filter, de-green the cream

            // White Balance
            var wb = profile.Add<UnityEngine.Rendering.Universal.WhiteBalance>(true);
            wb.temperature.Override(15f);
            wb.tint.Override(-5f);

            // Shadows/Midtones/Highlights — Art Bible exact
            var smh = profile.Add<UnityEngine.Rendering.Universal.ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.40f, 0.46f, 0.55f, 0f));   // cooler blue shadows
            smh.midtones.Override(new Vector4(1.0f, 0.93f, 0.86f, 0f));   // warm midtones (de-green cream → honey)
            smh.highlights.Override(new Vector4(1.0f, 0.82f, 0.54f, 0f)); // amber-orange highlight peak

            // Lift the deep shadows slightly (warm) so the forward POV floor isn't
            // crushed to pure black, without touching midtones/highlights.
            var lgg = profile.Add<UnityEngine.Rendering.Universal.LiftGammaGain>(true);
            lgg.lift.Override(new Vector4(1.04f, 1.0f, 0.95f, 0.05f)); // warm +lift, forward floor readable

            // Vignette — stronger for cinematic feel
            var vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            vig.intensity.Override(0.45f); // stronger — collects the frame, tames the bright side wall
            vig.smoothness.Override(0.45f);

            // Film Grain — cinematic
            var grain = profile.Add<UnityEngine.Rendering.Universal.FilmGrain>(true);
            grain.intensity.Override(0.2f); // Sprint 10: was 0.15

            // Depth of Field — subtle background blur
            var dof = profile.Add<UnityEngine.Rendering.Universal.DepthOfField>(true);
            dof.mode.Override(UnityEngine.Rendering.Universal.DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(3f);
            dof.gaussianEnd.Override(15f);
            dof.gaussianMaxRadius.Override(0.6f);
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

        // ============================================================
        // SPRINT 8: MATERIALS — normal maps, roughness, emissive, glass
        // ============================================================

        [MenuItem("Afterhumans/v2/Sprint 8 — Materials")]
        public static void Sprint8_Materials()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Generate normal maps
            var tileNormal = ProceduralTextures.TileFloorNormal();
            var plasterNormal = ProceduralTextures.PlasterWallNormal();
            var woodNormal = ProceduralTextures.WoodNormal();

            var greybox = GameObject.Find("Botanika_Greybox");
            if (greybox == null)
            {
                Debug.LogError("[BotanikaBuilder] Sprint 8: Botanika_Greybox not found");
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // === FLOOR: tile texture + normal map + roughness ===
            var texTile = ProceduralTextures.TileFloor();
            ApplyPbrMaterial(greybox, "Floor", shader, texTile, tileNormal,
                new Color(0.72f, 0.55f, 0.38f), 0.75f, 0f, 4f);

            // === WALLS: plaster texture + normal map ===
            var texPlaster = ProceduralTextures.PlasterWall();
            ApplyPbrMaterial(greybox, "Wall_", shader, texPlaster, plasterNormal,
                new Color(0.82f, 0.72f, 0.58f), 0.85f, 0f, 3f);

            // === CEILING: slightly different plaster ===
            ApplyPbrMaterial(greybox, "Ceiling", shader, texPlaster, plasterNormal,
                new Color(0.88f, 0.82f, 0.72f), 0.8f, 0f, 2f);

            // === GLASS on window gaps (between sills and lintels) ===
            var glassRoot = GameObject.Find("Botanika_Lighting");
            if (glassRoot == null) glassRoot = greybox;
            CreateWindowGlass(glassRoot, shader);

            // === EMISSIVE: chandelier glow ===
            var chandelier = greybox.transform.Find("Chandelier");
            if (chandelier != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(0.95f, 0.85f, 0.55f));
                mat.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.6f) * 2f);
                mat.EnableKeyword("_EMISSION");
                mat.SetFloat("_Smoothness", 0.6f);
                mat.SetFloat("_Metallic", 0.3f);
                foreach (var r in chandelier.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = mat;
            }

            // === SERVER RACK: metallic + emissive LED spots ===
            var serverRack = greybox.transform.Find("ServerRack");
            if (serverRack != null)
            {
                var metalMat = new Material(shader);
                metalMat.SetColor("_BaseColor", new Color(0.22f, 0.22f, 0.25f));
                metalMat.SetFloat("_Smoothness", 0.6f);
                metalMat.SetFloat("_Metallic", 0.8f);
                foreach (var r in serverRack.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = metalMat;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BotanikaBuilder] Sprint 8 MATERIALS done — normal maps, roughness, emissive, glass");
        }

        // ============================================================
        // SPRINT 9: ATMOSPHERE + DETAILS
        // Particles, storytelling props, graffiti, server LED
        // ============================================================

        [MenuItem("Afterhumans/v2/Sprint 9 — Atmosphere")]
        public static void Sprint9_Atmosphere()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ClearRoot("Botanika_Atmosphere");

            var root = new GameObject("Botanika_Atmosphere");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // === DUST PARTICLES in light beams ===
            CreateDustParticles(root);

            // === COFFEE STEAM near Kirill ===
            CreateSteamParticles(root, PosKitchen + new Vector3(0, 1.0f, 0));

            // === STORYTELLING PROPS ===
            // Mugs (cylinders — no Kenney mug model)
            var mugMat = MakeMaterial("Mug", new Color(0.85f, 0.82f, 0.75f), 0.3f);
            MakeCylinder(root, "Mug_Sasha", PosCoffeeTable + new Vector3(-0.3f, 0.5f, 0.1f),
                new Vector3(0.06f, 0.05f, 0.06f), mugMat);
            MakeCylinder(root, "Mug_Mila", PosDesk + new Vector3(0.4f, 0.85f, 0.2f),
                new Vector3(0.06f, 0.05f, 0.06f), mugMat);
            MakeCylinder(root, "Mug_Kirill", PosKitchen + new Vector3(-0.3f, 1.0f, 0.1f),
                new Vector3(0.06f, 0.05f, 0.06f), mugMat);

            // Laptop on Mila's desk (emissive screen)
            PlaceFbx(root, $"{FurnitureFbx}/laptop.fbx", "Laptop_Mila",
                PosDesk + new Vector3(0, 0.78f, 0), Quaternion.Euler(0, -90, 0), Vector3.one * 0.8f);
            // Make laptop screen emissive
            var laptopObj = root.transform.Find("Laptop_Mila");
            if (laptopObj != null)
            {
                var emMat = new Material(shader);
                emMat.SetColor("_BaseColor", new Color(0.2f, 0.3f, 0.5f));
                emMat.SetColor("_EmissionColor", new Color(0.4f, 0.6f, 0.9f) * 1.5f);
                emMat.EnableKeyword("_EMISSION");
                emMat.SetFloat("_Smoothness", 0.9f);
                foreach (var r in laptopObj.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = emMat;
            }

            // Bottle near Nikolai (glass cylinder)
            var glassMat = new Material(shader);
            glassMat.SetColor("_BaseColor", new Color(0.15f, 0.25f, 0.12f, 0.6f));
            glassMat.SetFloat("_Surface", 1);
            glassMat.SetOverrideTag("RenderType", "Transparent");
            glassMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glassMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glassMat.SetInt("_ZWrite", 0);
            glassMat.renderQueue = 3000;
            glassMat.SetFloat("_Smoothness", 0.95f);
            MakeCylinder(root, "Bottle_Nikolai", PosTableNikolai + new Vector3(0.15f, 0.65f, 0),
                new Vector3(0.035f, 0.12f, 0.035f), glassMat);

            // Turka near Kirill (copper cylinder)
            var copperMat = MakeMaterial("Copper", new Color(0.72f, 0.42f, 0.22f), 0.4f);
            MakeCylinder(root, "Turka_Kirill", PosKitchen + new Vector3(0.3f, 1.0f, -0.1f),
                new Vector3(0.03f, 0.06f, 0.03f), copperMat);

            // Foil hat on Stas (flattened silver sphere)
            var foilMat = MakeMaterial("Foil", new Color(0.85f, 0.87f, 0.90f), 0.7f);
            var hat = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hat.name = "FoilHat_Stas";
            hat.transform.SetParent(root.transform, false);
            hat.transform.position = PosStas + new Vector3(0, 1.6f, 0);
            hat.transform.localScale = new Vector3(0.25f, 0.08f, 0.25f);
            hat.GetComponent<Renderer>().sharedMaterial = foilMat;
            Object.DestroyImmediate(hat.GetComponent<Collider>());

            // Note on coffee table (white thin cube)
            var noteMat = MakeMaterial("Note", new Color(0.95f, 0.93f, 0.88f), 0.9f);
            MakeBox(root, "Note_Table", PosCoffeeTable + new Vector3(-0.1f, 0.48f, -0.15f),
                new Vector3(0.15f, 0.005f, 0.1f), noteMat);

            // Pillows on sofa
            PlaceFbx(root, $"{FurnitureFbx}/pillow.fbx", "Pillow_1",
                PosSofa + new Vector3(-0.5f, 0.45f, 0), Quaternion.Euler(0, 15, 0), Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/pillow.fbx", "Pillow_2",
                PosSofa + new Vector3(0.5f, 0.45f, 0), Quaternion.Euler(0, -20, 0), Vector3.one);

            // Small potted plants on surfaces
            PlaceFbx(root, $"{FurnitureFbx}/plantSmall1.fbx", "PlantPot_Desk",
                PosDesk + new Vector3(-0.4f, 0.85f, 0), Quaternion.identity, Vector3.one);
            PlaceFbx(root, $"{FurnitureFbx}/plantSmall2.fbx", "PlantPot_Bookcase",
                PosBookcaseNW + new Vector3(0, 1.6f, 0), Quaternion.identity, Vector3.one);

            // === GRAFFITI: "segfault == freedom" ===
            CreateGraffiti(root);

            // === SERVER RACK LED ===
            CreateServerLED(root);

            // Coat rack near door
            PlaceFbx(root, $"{FurnitureFbx}/coatRackStanding.fbx", "CoatRack",
                new Vector3(-2f, 0, -4.2f), Quaternion.identity, Vector3.one);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BotanikaBuilder] Sprint 9 ATMOSPHERE done — particles, props, graffiti, LED");
        }

        // ============================================================
        // SPRINT AA: PBR TEXTURE UPGRADE
        // Replace procedural textures with downloaded PBR (Poly Haven, ambientCG)
        // ============================================================

        [MenuItem("Afterhumans/v2/Sprint AA — PBR Textures")]
        public static void SprintAA_PbrTextures()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // First create AA materials
            AAMaterialSetup.SetupAllMaterials();

            var greybox = GameObject.Find("Botanika_Greybox");
            if (greybox == null)
            {
                Debug.LogError("[BotanikaBuilder] Sprint AA: Botanika_Greybox not found");
                return;
            }

            // Load AA materials
            var matFloor   = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AA/Mat_Floor_WoodWorn.mat");
            var matWalls   = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AA/Mat_Walls_Plaster.mat");
            var matCeiling = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AA/Mat_Ceiling_White.mat");
            var matPillars = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AA/Mat_Pillars_Concrete.mat");
            var matGlass   = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/AA/Mat_Glass_Window.mat");

            // Apply to scene objects by name
            foreach (var rend in greybox.GetComponentsInChildren<Renderer>(true))
            {
                var name = rend.gameObject.name;

                if (name == "Floor" && matFloor != null)
                    rend.sharedMaterial = matFloor;
                else if (name == "Ceiling" && matCeiling != null)
                    rend.sharedMaterial = matCeiling;
                else if (name.StartsWith("Wall_") && name.Contains("_L") || name.Contains("_R") || name.Contains("_F") || name.Contains("_B"))
                {
                    if (matPillars != null) rend.sharedMaterial = matPillars;
                }
                else if (name.StartsWith("Wall_") && (name.Contains("Top") || name.Contains("Bot")))
                {
                    if (matPillars != null) rend.sharedMaterial = matPillars;
                }
                else if (name.StartsWith("Glass_") && matGlass != null)
                    rend.sharedMaterial = matGlass;
            }

            // Also apply wall material to remaining wall objects
            foreach (var rend in greybox.GetComponentsInChildren<Renderer>(true))
            {
                var name = rend.gameObject.name;
                if (name.StartsWith("Wall_") && rend.sharedMaterial != matPillars && rend.sharedMaterial != matGlass)
                {
                    if (matWalls != null) rend.sharedMaterial = matWalls;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[BotanikaBuilder] Sprint AA PBR TEXTURES done — floor, walls, ceiling, pillars, glass upgraded");
        }

        private static void CreateDustParticles(GameObject parent)
        {
            var go = new GameObject("DustParticles");
            go.transform.SetParent(parent.transform);
            go.transform.position = new Vector3(2, 5, 1); // near east window, high up

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 10f;
            main.startSpeed = 0.02f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.maxParticles = 80;
            main.startColor = new Color(1f, 0.92f, 0.72f, 0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.005f; // slight upward drift

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(4, 6, 4);

            var colorLife = ps.colorOverLifetime;
            colorLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.4f, 0.3f),
                        new GradientAlphaKey(0.4f, 0.7f), new GradientAlphaKey(0, 1) }
            );
            colorLife.color = gradient;

            // Use default particle material
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                            Shader.Find("Particles/Standard Unlit"));
            particleMat.SetColor("_BaseColor", new Color(1, 0.92f, 0.72f, 0.3f));
            renderer.sharedMaterial = particleMat;

            Debug.Log("[BotanikaBuilder] Dust particles created (80 max, warm amber)");
        }

        private static void CreateSteamParticles(GameObject parent, Vector3 pos)
        {
            var go = new GameObject("CoffeeSteam");
            go.transform.SetParent(parent.transform);
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 3f;
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.maxParticles = 15;
            main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.15f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15;
            shape.radius = 0.05f;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = 0.1f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var steamMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                                         Shader.Find("Particles/Standard Unlit"));
            steamMat.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.9f, 0.15f));
            renderer.sharedMaterial = steamMat;
        }

        private static void CreateGraffiti(GameObject parent)
        {
            var go = new GameObject("Graffiti");
            go.transform.SetParent(parent.transform);
            // ON north wall surface, facing south (into room)
            go.transform.position = new Vector3(2, 3.5f, 4.89f);
            go.transform.rotation = Quaternion.Euler(0, 180, 0);
            go.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f); // flush against wall

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 60);

            var textGo = new GameObject("GraffitiText");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "segfault == freedom";
            tmp.fontSize = 48;
            tmp.color = new Color(0.85f, 0.15f, 0.1f); // red graffiti
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;

            Debug.Log("[BotanikaBuilder] Graffiti created: 'segfault == freedom'");
        }

        private static void CreateServerLED(GameObject parent)
        {
            var pos = PosServerRack;
            Color[] ledColors = { new Color(0.1f, 1f, 0.2f), new Color(1f, 0.2f, 0.1f), new Color(0.1f, 1f, 0.2f) };
            float[] heights = { 0.3f, 0.8f, 1.3f };

            for (int i = 0; i < 3; i++)
            {
                var ledGo = new GameObject($"ServerLED_{i}");
                ledGo.transform.SetParent(parent.transform);
                ledGo.transform.position = pos + new Vector3(-0.15f, heights[i], 0);
                var led = ledGo.AddComponent<Light>();
                led.type = LightType.Point;
                led.color = ledColors[i];
                led.intensity = 0.5f;
                led.range = 0.5f;
                // Add blinking
                ledGo.AddComponent<Afterhumans.Art.BlinkingLight>();
            }
            Debug.Log("[BotanikaBuilder] Server rack LED created (3 lights)");
        }

        private static GameObject MakeCylinder(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.isStatic = true;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // ============================================================
        // PBR HELPERS
        // ============================================================

        private static void ApplyPbrMaterial(GameObject parent, string nameContains, Shader shader,
            Texture2D albedo, Texture2D normal, Color tint, float roughness, float metallic, float tileScale)
        {
            foreach (var rend in parent.GetComponentsInChildren<Renderer>(true))
            {
                if (!rend.gameObject.name.Contains(nameContains)) continue;
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", tint);
                if (albedo != null)
                {
                    mat.SetTexture("_BaseMap", albedo);
                    mat.SetTextureScale("_BaseMap", new Vector2(tileScale, tileScale));
                }
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.SetTextureScale("_BumpMap", new Vector2(tileScale, tileScale));
                    mat.SetFloat("_BumpScale", 1.0f);
                    mat.EnableKeyword("_NORMALMAP");
                }
                mat.SetFloat("_Smoothness", 1f - roughness); // URP: smoothness = 1 - roughness
                mat.SetFloat("_Metallic", metallic);
                rend.sharedMaterial = mat;
            }
        }

        private static void CreateWindowGlass(GameObject parent, Shader shader)
        {
            // Glass panes in window openings (between sill and lintel)
            var glassMat = new Material(shader);
            glassMat.SetColor("_BaseColor", new Color(0.75f, 0.88f, 0.82f, 0.12f));
            glassMat.SetFloat("_Surface", 1); // Transparent
            glassMat.SetFloat("_Blend", 0);   // Alpha
            glassMat.SetOverrideTag("RenderType", "Transparent");
            glassMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glassMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glassMat.SetInt("_ZWrite", 0);
            glassMat.renderQueue = 3000;
            glassMat.SetFloat("_Smoothness", 0.95f); // very glossy
            glassMat.SetFloat("_Metallic", 0.1f);

            float wallH = WallHeight;
            // North window glass
            var nGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nGlass.name = "Glass_North";
            nGlass.transform.SetParent(parent.transform, false);
            nGlass.transform.position = new Vector3(0, wallH * 0.5f, 5);
            nGlass.transform.localScale = new Vector3(6, wallH - 1.8f, 0.05f);
            nGlass.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.DestroyImmediate(nGlass.GetComponent<Collider>());
            nGlass.isStatic = true;

            // East window glass
            var eGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eGlass.name = "Glass_East";
            eGlass.transform.SetParent(parent.transform, false);
            eGlass.transform.position = new Vector3(6, wallH * 0.5f, 0);
            eGlass.transform.localScale = new Vector3(0.05f, wallH - 1.8f, 4);
            eGlass.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.DestroyImmediate(eGlass.GetComponent<Collider>());
            eGlass.isStatic = true;

            // West window glass
            var wGlass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wGlass.name = "Glass_West";
            wGlass.transform.SetParent(parent.transform, false);
            wGlass.transform.position = new Vector3(-6, wallH * 0.5f, 0);
            wGlass.transform.localScale = new Vector3(0.05f, wallH - 1.8f, 4);
            wGlass.GetComponent<Renderer>().sharedMaterial = glassMat;
            Object.DestroyImmediate(wGlass.GetComponent<Collider>());
            wGlass.isStatic = true;

            Debug.Log("[BotanikaBuilder] Window glass panes created (N/E/W)");
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

        private static Material MakeMaterial(string name, Color color, float smoothness = 0.1f,
            bool doubleSided = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.name = name;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (doubleSided)
            {
                // Render BOTH faces — single-quad roof slopes are seen from inside
                // the nave regardless of triangle winding (URP/Lit _Cull 0 = Off).
                if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
                if (mat.HasProperty("_RenderFace")) mat.SetFloat("_RenderFace", 0f);
                mat.doubleSidedGI = true;
            }
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
            player.transform.position = PosPlayer;                 // south spawn, Z = -12
            player.transform.rotation = Quaternion.Euler(0, 0, 0); // facing NORTH (+Z)

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

            Debug.Log($"[BotanikaBuilder] Player setup at {PosPlayer} facing +Z (north), camera at eye height");
        }
    }
}
