using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Replaces placeholder cubes in Scene_Botanika with Kenney FBX assets:
    /// sofa for Sasha, coffee table with book, floor lamp, and scattered plants
    /// to give the botanical garden a tangible "warm oasis" vibe.
    ///
    /// Run via:
    ///   Unity -batchmode -nographics -quit -projectPath ~/afterhumans \
    ///     -executeMethod Afterhumans.EditorTools.BotanikaDresser.Dress
    /// </summary>
    public static class BotanikaDresser
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_Botanika.unity";
        private const string KenneyBase = "Assets/_Project/Vendor/Kenney";
        private const string FurnitureFbx = KenneyBase + "/furniture-kit/Models/FBX format";
        private const string NatureFbx = KenneyBase + "/nature-kit/Models/FBX format";
        private const string PropsRootName = "Botanika_Props";

        // Runtime URP/Lit materials shared across all instances to keep draw calls low.
        // Kenney FBX don't bundle textures — they rely on per-material Kd colors in
        // .mtl files which Unity's FBX importer doesn't read. We replace all submesh
        // materials with a single tinted URP/Lit material per category.
        private static Material _matWood;
        private static Material _matUpholstery;
        private static Material _matMetal;
        private static Material _matLeaf;
        private static Material _matBark;
        private static Material _matStem;
        private static Material _matBook;
        private static Material _matFlowerRed;
        private static Material _matFlowerYellow;
        private static Material _matFlowerPurple;

        private const string MaterialsDir = "Assets/_Project/Materials/Tints";

        private static Material LoadOrCreateLit(string name, Color color, float smoothness = 0.2f)
        {
            if (!Directory.Exists(MaterialsDir))
            {
                Directory.CreateDirectory(MaterialsDir);
                AssetDatabase.Refresh();
            }
            string path = $"{MaterialsDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            // Project uses Built-in Render Pipeline (GraphicsSettings.m_CustomRenderPipeline is null),
            // so Standard shader is the correct choice — URP/Lit renders as magenta missing-shader.
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("[BotanikaDresser] Standard shader not found, trying URP/Lit");
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (existing == null)
            {
                var mat = new Material(shader);
                mat.name = name;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
                AssetDatabase.SaveAssets();
                return mat;
            }
            // Update existing asset's color in case we tweak the palette
            existing.shader = shader;
            if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
            if (existing.HasProperty("_Color")) existing.SetColor("_Color", color);
            if (existing.HasProperty("_Smoothness")) existing.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void InitMaterials()
        {
            _matWood = LoadOrCreateLit("Tint_Wood", new Color(0.42f, 0.28f, 0.18f), 0.15f);
            _matUpholstery = LoadOrCreateLit("Tint_Upholstery", new Color(0.55f, 0.30f, 0.22f), 0.1f);
            _matMetal = LoadOrCreateLit("Tint_Metal", new Color(0.55f, 0.52f, 0.48f), 0.6f);
            _matLeaf = LoadOrCreateLit("Tint_Leaf", new Color(0.28f, 0.52f, 0.18f), 0.15f);
            _matBark = LoadOrCreateLit("Tint_Bark", new Color(0.32f, 0.22f, 0.15f), 0.1f);
            _matStem = LoadOrCreateLit("Tint_Stem", new Color(0.38f, 0.55f, 0.25f), 0.15f);
            _matBook = LoadOrCreateLit("Tint_Book", new Color(0.65f, 0.22f, 0.18f), 0.15f);
            _matFlowerRed = LoadOrCreateLit("Tint_FlowerRed", new Color(0.85f, 0.2f, 0.2f), 0.3f);
            _matFlowerYellow = LoadOrCreateLit("Tint_FlowerYellow", new Color(0.95f, 0.82f, 0.18f), 0.3f);
            _matFlowerPurple = LoadOrCreateLit("Tint_FlowerPurple", new Color(0.58f, 0.32f, 0.78f), 0.3f);
            AssetDatabase.SaveAssets();
        }

        private static Material PickMaterial(string assetName)
        {
            string n = assetName.ToLowerInvariant();
            if (n.Contains("book") && !n.Contains("bookcase")) return _matBook;
            if (n.Contains("sofa") || n.Contains("lounge") || n.Contains("pillow") || n.Contains("cushion")) return _matUpholstery;
            if (n.Contains("lamp")) return _matMetal;
            if (n.Contains("flower_red")) return _matFlowerRed;
            if (n.Contains("flower_yellow")) return _matFlowerYellow;
            if (n.Contains("flower_purple")) return _matFlowerPurple;
            if (n.Contains("tree") || n.Contains("stump") || n.Contains("log")) return _matBark;
            if (n.Contains("grass") || n.Contains("plant") || n.Contains("bush") || n.Contains("leaf")) return _matLeaf;
            return _matWood; // default for furniture
        }

        [MenuItem("Afterhumans/Setup/Dress Botanika")]
        public static void Dress()
        {
            Debug.Log("[BotanikaDresser] Opening Scene_Botanika...");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            InitMaterials();

            // Clear any previous dressing so re-running is idempotent
            var existing = GameObject.Find(PropsRootName);
            if (existing != null)
            {
                Debug.Log("[BotanikaDresser] Removing previous props");
                Object.DestroyImmediate(existing);
            }

            var propsRoot = new GameObject(PropsRootName);

            // Pull Player well back and slightly off-axis so the whole hero arrangement
            // (sofa + Sasha + coffee table + bookcases) is framed nicely at spawn,
            // and the first few steps forward bring them to the NPC naturally.
            var player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(0f, 1.1f, -12f);
                player.transform.rotation = Quaternion.identity;
                Debug.Log("[BotanikaDresser] Player spawn moved to (0, 1.1, -12)");
            }

            // Relocate Sasha NPC to (0, 1, 3) so the hero arrangement is clean.
            var sasha = GameObject.Find("Placeholder_NPC_Sasha");
            if (sasha != null)
            {
                sasha.transform.position = new Vector3(0f, 1f, 3f);
                sasha.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // --- Sasha's sofa behind his position (he sits on it)
            // Kenney sofa pivot is at base-front, scale ~1m so OK as-is.
            Place(propsRoot, $"{FurnitureFbx}/loungeDesignSofa.fbx",
                new Vector3(0f, 0f, 4f),
                Quaternion.Euler(0f, 180f, 0f),
                Vector3.one);

            // Coffee table in front of Sasha with books on top
            Place(propsRoot, $"{FurnitureFbx}/tableCoffeeGlassSquare.fbx",
                new Vector3(0f, 0f, 1.8f),
                Quaternion.identity,
                Vector3.one);

            Place(propsRoot, $"{FurnitureFbx}/books.fbx",
                new Vector3(0.2f, 0.45f, 1.8f),
                Quaternion.Euler(0f, 25f, 0f),
                Vector3.one);

            // Floor lamp to the right of the sofa
            Place(propsRoot, $"{FurnitureFbx}/lampRoundFloor.fbx",
                new Vector3(1.8f, 0f, 4.2f),
                Quaternion.identity,
                Vector3.one);

            // Bookcase along the back wall
            Place(propsRoot, $"{FurnitureFbx}/bookcaseOpen.fbx",
                new Vector3(-4f, 0f, 5.5f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one);

            Place(propsRoot, $"{FurnitureFbx}/bookcaseOpenLow.fbx",
                new Vector3(4f, 0f, 5.5f),
                Quaternion.Euler(0f, -90f, 0f),
                Vector3.one);

            // Lounge chair to the side where other NPCs could sit
            Place(propsRoot, $"{FurnitureFbx}/loungeChairRelax.fbx",
                new Vector3(-3f, 0f, 2f),
                Quaternion.Euler(0f, 45f, 0f),
                Vector3.one);

            Place(propsRoot, $"{FurnitureFbx}/chairCushion.fbx",
                new Vector3(3f, 0f, 2f),
                Quaternion.Euler(0f, -45f, 0f),
                Vector3.one);

            // --- Plants around the edges for oasis feel
            // Large bushes at corners
            Place(propsRoot, $"{NatureFbx}/plant_bushLarge.fbx",
                new Vector3(-6f, 0f, -4f), Quaternion.identity, Vector3.one * 1.2f);
            Place(propsRoot, $"{NatureFbx}/plant_bushLarge.fbx",
                new Vector3(6f, 0f, -4f), Quaternion.identity, Vector3.one * 1.2f);
            Place(propsRoot, $"{NatureFbx}/plant_bushLarge.fbx",
                new Vector3(-6f, 0f, 6f), Quaternion.Euler(0f, 120f, 0f), Vector3.one * 1.2f);
            Place(propsRoot, $"{NatureFbx}/plant_bushLarge.fbx",
                new Vector3(6f, 0f, 6f), Quaternion.Euler(0f, -120f, 0f), Vector3.one * 1.2f);

            // Mid bushes
            Place(propsRoot, $"{NatureFbx}/plant_bushDetailed.fbx",
                new Vector3(-5f, 0f, 1f), Quaternion.Euler(0f, 30f, 0f), Vector3.one);
            Place(propsRoot, $"{NatureFbx}/plant_bushDetailed.fbx",
                new Vector3(5f, 0f, 1f), Quaternion.Euler(0f, -30f, 0f), Vector3.one);

            // Small bushes scattered
            Place(propsRoot, $"{NatureFbx}/plant_bushSmall.fbx",
                new Vector3(-2.5f, 0f, -1f), Quaternion.identity, Vector3.one);
            Place(propsRoot, $"{NatureFbx}/plant_bushSmall.fbx",
                new Vector3(2.5f, 0f, -1f), Quaternion.identity, Vector3.one);
            Place(propsRoot, $"{NatureFbx}/plant_bushSmall.fbx",
                new Vector3(-3f, 0f, 4.5f), Quaternion.Euler(0f, 60f, 0f), Vector3.one);
            Place(propsRoot, $"{NatureFbx}/plant_bushSmall.fbx",
                new Vector3(3f, 0f, 4.5f), Quaternion.Euler(0f, -60f, 0f), Vector3.one);

            // Trees along back
            Place(propsRoot, $"{NatureFbx}/tree_palmShort.fbx",
                new Vector3(-7f, 0f, 3f), Quaternion.identity, Vector3.one * 1.3f);
            Place(propsRoot, $"{NatureFbx}/tree_palmShort.fbx",
                new Vector3(7f, 0f, 3f), Quaternion.Euler(0f, 45f, 0f), Vector3.one * 1.3f);

            // Grass patches
            for (int i = 0; i < 12; i++)
            {
                float angle = i * (360f / 12f) * Mathf.Deg2Rad;
                float radius = 8f + (i % 3) * 1.5f;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius + 1f);
                Place(propsRoot, $"{NatureFbx}/grass_large.fbx",
                    pos, Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f), Vector3.one);
            }

            // Flowers as decoration near the sitting area
            Place(propsRoot, $"{NatureFbx}/flower_redA.fbx",
                new Vector3(-1.5f, 0f, 0.5f), Quaternion.identity, Vector3.one);
            Place(propsRoot, $"{NatureFbx}/flower_yellowA.fbx",
                new Vector3(1.5f, 0f, 0.5f), Quaternion.identity, Vector3.one);
            Place(propsRoot, $"{NatureFbx}/flower_purpleA.fbx",
                new Vector3(-0.7f, 0f, -1.2f), Quaternion.identity, Vector3.one);
            Place(propsRoot, $"{NatureFbx}/flower_purpleB.fbx",
                new Vector3(0.7f, 0f, -1.2f), Quaternion.identity, Vector3.one);

            // Hide the placeholder gray walls — we want to feel open, not boxed in.
            var walls = GameObject.Find("Boundary_Walls");
            if (walls != null)
            {
                foreach (var r in walls.GetComponentsInChildren<Renderer>())
                {
                    r.enabled = false;
                }
                Debug.Log("[BotanikaDresser] Hid boundary wall renderers (colliders kept)");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (saved)
            {
                Debug.Log("[BotanikaDresser] Scene_Botanika saved with Botanika_Props");
            }
            else
            {
                Debug.LogError("[BotanikaDresser] Failed to save Scene_Botanika");
            }

            AssetDatabase.SaveAssets();
        }

        private static void Place(GameObject parent, string assetPath, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError($"[BotanikaDresser] Asset not found: {assetPath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                Debug.LogError($"[BotanikaDresser] Failed to instantiate: {assetPath}");
                return;
            }

            instance.transform.SetParent(parent.transform, worldPositionStays: false);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.localScale = scale;

            // Replace magenta missing-material on all submeshes with a tinted URP/Lit material.
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            var tint = PickMaterial(assetName);
            foreach (var r in instance.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                var count = r.sharedMaterials.Length;
                var mats = new Material[count];
                for (int i = 0; i < count; i++) mats[i] = tint;
                r.sharedMaterials = mats;
            }

            // Give it a collider so the player can't walk through furniture
            if (instance.GetComponent<Collider>() == null)
            {
                var meshFilter = instance.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    var mc = instance.AddComponent<MeshCollider>();
                    mc.sharedMesh = meshFilter.sharedMesh;
                    mc.convex = false;
                }
            }
        }
    }
}
