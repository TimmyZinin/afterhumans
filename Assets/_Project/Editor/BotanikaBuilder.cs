using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
