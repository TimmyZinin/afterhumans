using TMPro;
using UnityEditor;
using UnityEngine;
using Afterhumans.Art;
using Afterhumans.Dialogue;

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

            Debug.Log($"[BotanikaNpcPopulator] DONE — {spawned}/{Npcs.Length} NPCs spawned.");
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

            // Collider for interaction — blocky characters ship without colliders
            var col = instance.GetComponent<CapsuleCollider>();
            if (col == null) col = instance.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.radius = 0.4f;
            col.height = 1.8f;
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
