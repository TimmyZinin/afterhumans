using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Afterhumans.Art;

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
        private const string BotanikaThemePath = "Assets/_Project/Art/Themes/Botanika.asset";

        private static Material LoadOrCreateLit(string name, Color color, float smoothness = 0.2f)
        {
            if (!Directory.Exists(MaterialsDir))
            {
                Directory.CreateDirectory(MaterialsDir);
                AssetDatabase.Refresh();
            }
            string path = $"{MaterialsDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            // BOT-F01: URP активен → используем URP/Lit. Fallback на Standard для edge cases.
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("[BotanikaDresser] URP/Lit not found — URP not activated? Falling back to Standard");
                shader = Shader.Find("Standard");
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

        // BOT-A08: palette now data-driven from Botanika.asset SceneTheme.
        // Missing theme fields fall back to hardcoded defaults below so that
        // adding new tint slots doesn't require theme schema changes yet.
        private static void InitMaterials()
        {
            var theme = AssetDatabase.LoadAssetAtPath<SceneTheme>(BotanikaThemePath);
            if (theme == null)
            {
                Debug.LogWarning("[BotanikaDresser] Botanika.asset SceneTheme missing — falling back to hardcoded palette.");
            }

            // Helper: derive tint from theme.tertiary (warm wood) adjusted per role.
            // Where theme has a direct match, use theme color. Otherwise mix.
            Color wood       = theme != null ? theme.tertiary  : new Color(0.42f, 0.28f, 0.18f);
            Color upholstery = theme != null ? Multiply(theme.accent2, 0.85f) : new Color(0.55f, 0.30f, 0.22f);
            Color metal      = new Color(0.55f, 0.52f, 0.48f);  // metal stays neutral, not themed
            Color leaf       = theme != null ? theme.secondary : new Color(0.28f, 0.52f, 0.18f);
            Color bark       = theme != null ? theme.shadow    : new Color(0.32f, 0.22f, 0.15f);
            Color stem       = theme != null ? Lighten(theme.secondary, 0.25f) : new Color(0.38f, 0.55f, 0.25f);
            Color book       = theme != null ? theme.accent2   : new Color(0.65f, 0.22f, 0.18f);
            Color flowerRed  = theme != null ? Lighten(theme.accent2, 0.15f) : new Color(0.85f, 0.2f, 0.2f);
            Color flowerYell = theme != null ? theme.accent1   : new Color(0.95f, 0.82f, 0.18f);
            Color flowerPurp = new Color(0.58f, 0.32f, 0.78f);  // no theme slot; keep fixed

            _matWood         = LoadOrCreateLit("Tint_Wood",         wood,        0.15f);
            _matUpholstery   = LoadOrCreateLit("Tint_Upholstery",   upholstery,  0.1f);
            _matMetal        = LoadOrCreateLit("Tint_Metal",        metal,       0.6f);
            _matLeaf         = LoadOrCreateLit("Tint_Leaf",         leaf,        0.15f);
            _matBark         = LoadOrCreateLit("Tint_Bark",         bark,        0.1f);
            _matStem         = LoadOrCreateLit("Tint_Stem",         stem,        0.15f);
            _matBook         = LoadOrCreateLit("Tint_Book",         book,        0.15f);
            _matFlowerRed    = LoadOrCreateLit("Tint_FlowerRed",    flowerRed,   0.3f);
            _matFlowerYellow = LoadOrCreateLit("Tint_FlowerYellow", flowerYell,  0.3f);
            _matFlowerPurple = LoadOrCreateLit("Tint_FlowerPurple", flowerPurp,  0.3f);
            AssetDatabase.SaveAssets();
        }

        // Small color math helpers for palette derivation — keep inline to avoid
        // a utility file for one-off use. If more sites need these, promote to
        // Assets/_Project/Scripts/Art/ColorUtils.cs.
        private static Color Multiply(Color c, float f)
        {
            return new Color(c.r * f, c.g * f, c.b * f, c.a);
        }

        private static Color Lighten(Color c, float amount)
        {
            return new Color(
                Mathf.Lerp(c.r, 1f, amount),
                Mathf.Lerp(c.g, 1f, amount),
                Mathf.Lerp(c.b, 1f, amount),
                c.a);
        }

        private static void BuildGreenhouseShell(GameObject parent)
        {
            // Floor: 11x11 tiles, each 1m square, centered on origin so room spans (-5.5..5.5)
            var matTile = LoadOrCreateLit("Tint_TileFloor", new Color(0.58f, 0.45f, 0.30f), 0.1f);
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    Place(parent, $"{FurnitureFbx}/floorFull.fbx",
                        new Vector3(x, 0f, z), Quaternion.identity, Vector3.one, matTile);
                }
            }

            // Walls along the perimeter, with wallWindow tiles every 3rd slot for light
            var matPlaster = LoadOrCreateLit("Tint_Plaster", new Color(0.75f, 0.63f, 0.48f), 0.1f);
            for (int x = -5; x <= 5; x++)
            {
                // North wall (+Z side), facing -Z (into room)
                bool windowN = (x + 5) % 3 == 1;
                string wallN = windowN ? "wallWindow" : "wall";
                Place(parent, $"{FurnitureFbx}/{wallN}.fbx",
                    new Vector3(x, 0f, 5.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, matPlaster);

                // South wall (-Z side), facing +Z
                // Leave a 3-wide gap for the doorway in the middle
                if (x >= -1 && x <= 1)
                {
                    if (x == 0)
                    {
                        Place(parent, $"{FurnitureFbx}/doorway.fbx",
                            new Vector3(x, 0f, -5.5f), Quaternion.identity, Vector3.one, matPlaster);
                    }
                    continue;
                }
                bool windowS = (x + 5) % 3 == 2;
                string wallS = windowS ? "wallWindow" : "wall";
                Place(parent, $"{FurnitureFbx}/{wallS}.fbx",
                    new Vector3(x, 0f, -5.5f), Quaternion.identity, Vector3.one, matPlaster);
            }
            for (int z = -5; z <= 5; z++)
            {
                // West wall (-X)
                bool windowW = (z + 5) % 3 == 1;
                string wallW = windowW ? "wallWindow" : "wall";
                Place(parent, $"{FurnitureFbx}/{wallW}.fbx",
                    new Vector3(-5.5f, 0f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one, matPlaster);

                // East wall (+X)
                bool windowE = (z + 5) % 3 == 2;
                string wallE = windowE ? "wallWindow" : "wall";
                Place(parent, $"{FurnitureFbx}/{wallE}.fbx",
                    new Vector3(5.5f, 0f, z), Quaternion.Euler(0f, -90f, 0f), Vector3.one, matPlaster);
            }

            // Ceiling lamps in a 2x2 grid, mounted at y=2.8 (Kenney wall height ~3m)
            var matLamp = LoadOrCreateLit("Tint_CeilingLamp", new Color(0.95f, 0.82f, 0.55f), 0.4f);
            Place(parent, $"{FurnitureFbx}/lampSquareCeiling.fbx",
                new Vector3(-2.5f, 2.8f, -2.5f), Quaternion.identity, Vector3.one, matLamp);
            Place(parent, $"{FurnitureFbx}/lampSquareCeiling.fbx",
                new Vector3(2.5f, 2.8f, -2.5f), Quaternion.identity, Vector3.one, matLamp);
            Place(parent, $"{FurnitureFbx}/lampSquareCeiling.fbx",
                new Vector3(-2.5f, 2.8f, 2.5f), Quaternion.identity, Vector3.one, matLamp);
            Place(parent, $"{FurnitureFbx}/lampSquareCeiling.fbx",
                new Vector3(2.5f, 2.8f, 2.5f), Quaternion.identity, Vector3.one, matLamp);

            // Rug in front of the sofa
            Place(parent, $"{FurnitureFbx}/rugDoormat.fbx",
                new Vector3(0f, 0.01f, 1.8f), Quaternion.identity, new Vector3(2f, 1f, 2f), _matUpholstery);

            Debug.Log("[BotanikaDresser] Greenhouse shell built (11x11 floor, walls with windows, doorway, ceiling lamps)");
        }

        // Override of Place that accepts an explicit material — used by BuildGreenhouseShell.
        private static void Place(GameObject parent, string assetPath, Vector3 pos, Quaternion rot, Vector3 scale, Material forcedMat)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError($"[BotanikaDresser] Asset not found: {assetPath}");
                return;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) return;
            instance.transform.SetParent(parent.transform, worldPositionStays: false);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.localScale = scale;
            foreach (var r in instance.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                var count = r.sharedMaterials.Length;
                var mats = new Material[count];
                for (int i = 0; i < count; i++) mats[i] = forcedMat;
                r.sharedMaterials = mats;
            }
            // BOT-F05: use BoxCollider helper вместо MeshCollider (skill 3d-games anti-pattern)
            ColliderHelper.AddSimpleCollider(instance);
            // BOT-F10: mark static для batching / baked GI / occlusion culling
            ColliderHelper.MarkStaticProp(instance);
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
            LightingSetup.Apply(LightingSetup.Preset.Botanika);

            // Clear any previous dressing so re-running is idempotent
            var existing = GameObject.Find(PropsRootName);
            if (existing != null)
            {
                Debug.Log("[BotanikaDresser] Removing previous props");
                Object.DestroyImmediate(existing);
            }

            var propsRoot = new GameObject(PropsRootName);

            // --- GREENHOUSE SHELL (floor + walls + doorway + ceiling lamps) ---
            // Kenney furniture kit floor/wall tiles are ~1m x 1m. Build a 10x10 room.
            BuildGreenhouseShell(propsRoot);

            // Player spawns just inside the greenhouse doorway (south wall),
            // facing +Z toward Sasha on the sofa. Room spans (-5.5..5.5) on both axes.
            var player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(0f, 1.1f, -4.5f);
                player.transform.rotation = Quaternion.identity;
                Debug.Log("[BotanikaDresser] Player spawn at doorway (0, 1.1, -4.5)");
            }

            // Hide the 50m placeholder ground plane — our 11x11 tile floor replaces it.
            var oldGround = GameObject.Find("Placeholder_Ground");
            if (oldGround != null)
            {
                var rend = oldGround.GetComponent<Renderer>();
                if (rend != null) rend.enabled = false;
                Debug.Log("[BotanikaDresser] Hid placeholder 50m ground plane");
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

            // Bookcase along the back wall (pulled inside room)
            Place(propsRoot, $"{FurnitureFbx}/bookcaseOpen.fbx",
                new Vector3(-4.5f, 0f, 4.8f),
                Quaternion.identity,
                Vector3.one);

            Place(propsRoot, $"{FurnitureFbx}/bookcaseOpenLow.fbx",
                new Vector3(4.5f, 0f, 4.8f),
                Quaternion.identity,
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

            // BOT-A03: environmental storytelling props layer
            // (server rack, graffiti, NPC stations, foil hat, extra books)
            BotanikaEnvProps.Apply(propsRoot);

            // BOT-A04: volumetric atmosphere (glass ceiling + dust motes + window accents)
            BotanikaAtmosphere.Apply(propsRoot);

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

            // BOT-F05 + F10
            ColliderHelper.AddSimpleCollider(instance);
            ColliderHelper.MarkStaticProp(instance);
        }
    }
}
