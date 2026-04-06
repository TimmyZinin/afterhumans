using TMPro;
using UnityEditor;
using UnityEngine;
using Afterhumans.Art;
using Afterhumans.Dialogue;
using Afterhumans.UI;
using Afterhumans.Scenes;
using Afterhumans.Audio;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-N01/N02/N03/N08: Populate Scene_Botanika with 5 humanoid NPCs.
    ///
    /// Since Mixamo OAuth is out of autonomous reach and Quaternius scraper
    /// not scriptable, we fall back to Kenney blocky-characters pack (20 CC0
    /// low-poly humanoid FBX, 2MB, downloadable via curl).
    ///
    /// Plan coordinates (from Plan Mode):
    /// - Саша (philosopher): (0, 0, 4) on sofa, facing -Z toward player, knot=sasha
    /// - Мила (manifest writer): (-3.5, 0, 2) at desk, rot 45°, knot=mila
    /// - Кирилл (mushroom/coffee): (3.5, 0, 2) at kitchen, rot -45°, knot=kirill
    /// - Николай (data priest): (-4.5, 0, 4.5) corner, rot 135°, knot=nikolai,
    ///                           turnOnInteract enabled
    /// - Стас (paranoid): (0, 0, -3.5) near door, knot=stas
    ///
    /// Each NPC gets:
    /// - Kenney blocky character model (character-a..e.fbx)
    /// - CapsuleCollider (r 0.4, h 1.8) for interaction range
    /// - Interactable component with knot name
    /// - NpcIdleBob procedural animation (phase staggered)
    /// - NpcFacing (Nikolai only, turnOnInteract=true)
    ///
    /// Skills: game-art §silhouette, game-design §NPC variety, game-development
    /// §runtime-editor separation, theme-factory (materials via Interactable).
    ///
    /// Idempotent: finds+destroys previous Botanika_Npcs group before rebuild.
    /// Called from BotanikaDresser.Dress() after env props + atmosphere.
    /// </summary>
    public static class BotanikaNpcPopulator
    {
        private const string BlockyBase = "Assets/_Project/Vendor/Kenney/blocky-characters/Models/FBX format";
        private const string GroupName = "Botanika_Npcs";

        private struct NpcSpec
        {
            public string name;
            public string knot;
            public string modelFile;
            public Vector3 position;
            public float yRotation;
            public bool turnOnInteract;
            public float interactRadius;
            public string promptText;
        }

        private static readonly NpcSpec[] Npcs = new[]
        {
            new NpcSpec {
                name = "Npc_Sasha", knot = "sasha",
                modelFile = "character-a.fbx",
                position = new Vector3(0f, 0f, 4f),
                yRotation = 180f,
                interactRadius = 3.0f,
                promptText = "поговорить"
            },
            new NpcSpec {
                name = "Npc_Mila", knot = "mila",
                modelFile = "character-b.fbx",
                position = new Vector3(-3.5f, 0f, 2f),
                yRotation = 45f,
                interactRadius = 2.5f,
                promptText = "поговорить"
            },
            new NpcSpec {
                name = "Npc_Kirill", knot = "kirill",
                modelFile = "character-c.fbx",
                position = new Vector3(3.5f, 0f, 2f),
                yRotation = -45f,
                interactRadius = 2.5f,
                promptText = "поговорить"
            },
            new NpcSpec {
                name = "Npc_Nikolai", knot = "nikolai",
                modelFile = "character-d.fbx",
                position = new Vector3(-4.5f, 0f, 4.5f),
                yRotation = 135f,
                turnOnInteract = true,
                interactRadius = 2.5f,
                promptText = "поговорить"
            },
            new NpcSpec {
                name = "Npc_Stas", knot = "stas",
                modelFile = "character-e.fbx",
                position = new Vector3(0f, 0f, -3.5f),
                yRotation = 0f,
                interactRadius = 2.5f,
                promptText = "поговорить"
            },
        };

        public static void Apply(GameObject propsRoot)
        {
            if (propsRoot == null)
            {
                Debug.LogError("[BotanikaNpcPopulator] propsRoot is null — aborting.");
                return;
            }

            // Remove existing NPC group + legacy placeholder Sasha cube
            var existing = propsRoot.transform.Find(GroupName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var placeholder = GameObject.Find("Placeholder_NPC_Sasha");
            if (placeholder != null)
            {
                var rend = placeholder.GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;  // hide, keep for ref
            }

            var root = new GameObject(GroupName);
            root.transform.SetParent(propsRoot.transform, worldPositionStays: false);

            int spawned = 0;
            for (int i = 0; i < Npcs.Length; i++)
            {
                if (SpawnNpc(root, Npcs[i], i)) spawned++;
            }

            // BOT-S03: Note interactable on coffee table
            SpawnNote(root);

            // BOT-S05: Door cue UI (monitors door_to_city_open Ink var)
            SpawnDoorCue();

            // BOT-S08: Chapter indicator "I. Ботаника"
            SpawnChapterIndicator();

            // BOT-S01/S02: Wake-up cinematic director
            SpawnIntroDirector();

            // BOT-S06: Ambient audio
            SpawnAmbientAudio();

            // BOT-S07: Footstep controller on player
            SpawnFootstepController();

            Debug.Log($"[BotanikaNpcPopulator] DONE — {spawned}/{Npcs.Length} NPCs + note + cue + chapter + intro + audio.");
        }

        private static void SpawnNote(GameObject parent)
        {
            var existing = GameObject.Find("Note");
            if (existing != null) Object.DestroyImmediate(existing);

            // Simple white quad representing a folded paper note
            var note = GameObject.CreatePrimitive(PrimitiveType.Quad);
            note.name = "Note";
            note.transform.SetParent(parent.transform, false);
            note.transform.position = new Vector3(0.25f, 0.48f, 1.8f);  // on coffee table
            note.transform.rotation = Quaternion.Euler(90f, 15f, 0f);   // face up, slight angle
            note.transform.localScale = new Vector3(0.25f, 0.18f, 1f);  // paper-sized

            var rend = note.GetComponent<Renderer>();
            if (rend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.95f, 0.92f, 0.85f));
                rend.sharedMaterial = mat;
            }

            // Replace default MeshCollider with box
            Object.DestroyImmediate(note.GetComponent<Collider>());
            var box = note.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.1f);

            var interact = note.AddComponent<Interactable>();
            interact.knotName = "note";
            interact.promptText = "прочитать";
            interact.interactRadius = 1.5f;
            interact.oneTime = true;

            SpawnPrompt(note, "прочитать", 1.5f);
        }

        private static void SpawnDoorCue()
        {
            var existing = GameObject.Find("DoorCueUI");
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject("DoorCueUI");
            go.AddComponent<DoorCueUI>();
        }

        private static void SpawnChapterIndicator()
        {
            var existing = GameObject.Find("ChapterIndicator");
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject("ChapterIndicator");
            go.AddComponent<ChapterIndicatorUI>();
        }

        private static void SpawnAmbientAudio()
        {
            var existing = GameObject.Find("BotanikaAmbient");
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject("BotanikaAmbient");
            go.AddComponent<BotanikaAmbientAudio>();
        }

        private static void SpawnFootstepController()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player == null) return;

            if (player.GetComponent<FootstepController>() != null) return;

            // Add AudioSource + FootstepController
            if (player.GetComponent<AudioSource>() == null)
                player.AddComponent<AudioSource>();
            player.AddComponent<FootstepController>();
        }

        private static void SpawnIntroDirector()
        {
            var existing = GameObject.Find("BotanikaIntroDirector");
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject("BotanikaIntroDirector");
            go.AddComponent<BotanikaIntroDirector>();
        }

        private static bool SpawnNpc(GameObject parent, NpcSpec spec, int index)
        {
            string fbxPath = $"{BlockyBase}/{spec.modelFile}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null)
            {
                Debug.LogError($"[BotanikaNpcPopulator] Character FBX not found: {fbxPath}. " +
                    "Run scripts/download-assets.sh to fetch Kenney blocky-characters pack.");
                return false;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                Debug.LogError($"[BotanikaNpcPopulator] Failed to instantiate {fbxPath}");
                return false;
            }

            instance.name = spec.name;
            instance.transform.SetParent(parent.transform, worldPositionStays: false);
            instance.transform.position = spec.position;
            instance.transform.rotation = Quaternion.Euler(0f, spec.yRotation, 0f);

            // Collider for interaction — compute from actual renderer bounds
            // mm-review LOW fix: Kenney models may be scaled 0.2x via
            // KenneyAssetPostprocessor so hardcoded 1.8m height doesn't match.
            // Read combined renderer bounds and derive capsule from that.
            var col = instance.GetComponent<CapsuleCollider>();
            if (col == null) col = instance.AddComponent<CapsuleCollider>();
            Bounds? combinedBounds = null;
            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                if (combinedBounds == null) combinedBounds = r.bounds;
                else { var cb = combinedBounds.Value; cb.Encapsulate(r.bounds); combinedBounds = cb; }
            }
            if (combinedBounds.HasValue)
            {
                var b = combinedBounds.Value;
                col.center = instance.transform.InverseTransformPoint(b.center);
                col.height = b.size.y;
                col.radius = Mathf.Max(b.size.x, b.size.z) * 0.5f;
            }
            else
            {
                col.center = new Vector3(0f, 0.9f, 0f);
                col.radius = 0.4f;
                col.height = 1.8f;
            }
            col.direction = 1;  // Y
            col.isTrigger = false;

            // Interactable + knot wiring
            var interact = instance.GetComponent<Interactable>();
            if (interact == null) interact = instance.AddComponent<Interactable>();
            interact.knotName = spec.knot;
            interact.promptText = spec.promptText;
            interact.interactRadius = spec.interactRadius;
            interact.oneTime = false;

            // Procedural idle animation (BOT-N02 fallback for Mixamo)
            var bob = instance.GetComponent<NpcIdleBob>();
            if (bob == null) bob = instance.AddComponent<NpcIdleBob>();
            bob.SetPhase(index * 0.21f);  // stagger so NPCs breathe out of sync

            // Facing behavior only for Nikolai
            if (spec.turnOnInteract)
            {
                var facing = instance.GetComponent<NpcFacing>();
                if (facing == null) facing = instance.AddComponent<NpcFacing>();
                facing.turnOnInteract = true;
            }

            // BOT-N05: worldspace hover prompt «[E] говорить»
            SpawnPrompt(instance, spec.promptText, spec.interactRadius);

            return true;
        }

        private static void SpawnPrompt(GameObject npc, string text, float radius)
        {
            var promptGo = new GameObject("InteractionPrompt");
            promptGo.transform.SetParent(npc.transform, worldPositionStays: false);
            promptGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);  // above head

            var canvas = promptGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            var rect = promptGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2f, 0.5f);
            rect.localScale = Vector3.one * 0.01f;  // world-space 1px = 0.01 units

            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(promptGo.transform, worldPositionStays: false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"[E] {text}";
            tmp.fontSize = 28;
            tmp.color = new Color(1f, 0.92f, 0.7f);  // warm cream
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            var prompt = promptGo.AddComponent<InteractionPromptUI>();
            prompt.showRadius = radius + 1f;  // show slightly before interact range
            prompt.fadeSpeed = 5f;
        }

        /// <summary>
        /// BOT-T01 integration: verifies 5 expected NPCs exist with correct knots.
        /// </summary>
        public static bool Verify(out string reason)
        {
            foreach (var spec in Npcs)
            {
                var go = GameObject.Find(spec.name);
                if (go == null) { reason = $"Missing NPC: {spec.name}"; return false; }

                var interact = go.GetComponent<Interactable>();
                if (interact == null)
                {
                    reason = $"{spec.name} has no Interactable";
                    return false;
                }
                if (interact.knotName != spec.knot)
                {
                    reason = $"{spec.name} knot='{interact.knotName}' expected '{spec.knot}'";
                    return false;
                }
                var bob = go.GetComponent<NpcIdleBob>();
                if (bob == null)
                {
                    reason = $"{spec.name} missing NpcIdleBob";
                    return false;
                }
            }

            reason = "OK (5 NPCs wired with Interactable + NpcIdleBob)";
            return true;
        }

        [MenuItem("Afterhumans/Art/Verify Botanika NPCs")]
        public static void VerifyMenu()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/Scene_Botanika.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);
            if (!scene.IsValid()) { Debug.LogError("[BotanikaNpcPopulator] Failed to open scene"); return; }
            string reason;
            bool ok = Verify(out reason);
            if (ok) Debug.Log($"[BotanikaNpcPopulator] PASS — {reason}");
            else Debug.LogError($"[BotanikaNpcPopulator] FAIL — {reason}");
        }
    }
}
