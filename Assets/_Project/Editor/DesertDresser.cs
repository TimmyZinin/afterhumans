using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Populates Scene_Desert — the meditative final walk ending at the broken server
    /// with the blinking cursor. Uses Kenney cliff rocks, cacti, and bent palms to
    /// evoke a Dune-adjacent wasteland.
    /// </summary>
    public static class DesertDresser
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_Desert.unity";
        private const string NatureFbx = "Assets/_Project/Vendor/Kenney/nature-kit/Models/FBX format";
        private const string CityFbx = "Assets/_Project/Vendor/Kenney/city-kit-commercial/Models/FBX format";
        private const string PropsRootName = "Desert_Props";
        private const string MaterialsDir = "Assets/_Project/Materials/Tints";

        private static Material _matSand;
        private static Material _matRock;
        private static Material _matCactus;
        private static Material _matDeadPalm;
        private static Material _matServer;

        private static Material LoadOrCreateLit(string name, Color color, float smoothness = 0.2f)
        {
            if (!Directory.Exists(MaterialsDir)) Directory.CreateDirectory(MaterialsDir);
            string path = $"{MaterialsDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (existing == null)
            {
                var mat = new Material(shader);
                mat.name = name;
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
                AssetDatabase.SaveAssets();
                return mat;
            }
            existing.shader = shader;
            if (existing.HasProperty("_Color")) existing.SetColor("_Color", color);
            if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        [MenuItem("Afterhumans/Setup/Dress Desert")]
        public static void Dress()
        {
            Debug.Log("[DesertDresser] Opening Scene_Desert...");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            LightingSetup.Apply(LightingSetup.Preset.Desert);

            _matSand = LoadOrCreateLit("Tint_Sand", new Color(0.82f, 0.66f, 0.42f), 0.1f);
            _matRock = LoadOrCreateLit("Tint_Rock", new Color(0.56f, 0.40f, 0.28f), 0.15f);
            _matCactus = LoadOrCreateLit("Tint_Cactus", new Color(0.34f, 0.45f, 0.22f), 0.2f);
            _matDeadPalm = LoadOrCreateLit("Tint_DeadPalm", new Color(0.45f, 0.33f, 0.20f), 0.1f);
            _matServer = LoadOrCreateLit("Tint_Server", new Color(0.12f, 0.14f, 0.18f), 0.55f);

            var existing = GameObject.Find(PropsRootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var propsRoot = new GameObject(PropsRootName);

            var player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(0f, 1.1f, -18f);
                player.transform.rotation = Quaternion.identity;
            }

            // The Cursor placeholder is the final moment — put it at the far end near the broken server.
            var cursor = GameObject.Find("Placeholder_Cursor");
            if (cursor != null)
            {
                cursor.transform.position = new Vector3(0f, 1f, 14f);
            }

            // Scattered cliffs as dunes/rock formations to the sides
            float[] zs = { -14f, -8f, -2f, 4f, 10f };
            for (int i = 0; i < zs.Length; i++)
            {
                float z = zs[i];
                Place(propsRoot, $"{NatureFbx}/cliff_block_stone.fbx",
                    new Vector3(-9f + (i % 2) * 1.5f, 0f, z), Quaternion.Euler(0f, i * 37f, 0f), Vector3.one * 1.8f);
                Place(propsRoot, $"{NatureFbx}/cliff_blockSlope_rock.fbx",
                    new Vector3(9f - (i % 2) * 1.5f, 0f, z), Quaternion.Euler(0f, -i * 37f, 0f), Vector3.one * 1.6f);
            }

            // Larger cliff formations at the back as horizon silhouettes
            Place(propsRoot, $"{NatureFbx}/cliff_cornerInnerLarge_rock.fbx",
                new Vector3(-15f, 0f, 18f), Quaternion.identity, Vector3.one * 2.5f);
            Place(propsRoot, $"{NatureFbx}/cliff_cornerInnerLarge_rock.fbx",
                new Vector3(15f, 0f, 18f), Quaternion.Euler(0f, 90f, 0f), Vector3.one * 2.5f);

            // Cacti scattered along the walk
            Place(propsRoot, $"{NatureFbx}/cactus_tall.fbx",
                new Vector3(-3f, 0f, -10f), Quaternion.identity, Vector3.one * 1.2f);
            Place(propsRoot, $"{NatureFbx}/cactus_short.fbx",
                new Vector3(3f, 0f, -6f), Quaternion.identity, Vector3.one);
            Place(propsRoot, $"{NatureFbx}/cactus_tall.fbx",
                new Vector3(4f, 0f, 0f), Quaternion.Euler(0f, 45f, 0f), Vector3.one * 1.1f);
            Place(propsRoot, $"{NatureFbx}/cactus_short.fbx",
                new Vector3(-4f, 0f, 4f), Quaternion.Euler(0f, 90f, 0f), Vector3.one);

            // Bent/dead palms — remnants of a vanished oasis
            Place(propsRoot, $"{NatureFbx}/tree_palmBend.fbx",
                new Vector3(-6f, 0f, -12f), Quaternion.Euler(0f, 30f, 0f), Vector3.one * 1.4f);
            Place(propsRoot, $"{NatureFbx}/tree_palmBend.fbx",
                new Vector3(6f, 0f, 8f), Quaternion.Euler(0f, -60f, 0f), Vector3.one * 1.3f);

            // The broken server — a few low-detail buildings stacked & rotated to feel ruined
            // Use building-a rotated down as a crashed rack monument at (0, 0, 12)
            var monument = new GameObject("Broken_Server_Monument");
            monument.transform.SetParent(propsRoot.transform);
            monument.transform.position = new Vector3(0f, 0f, 12f);

            PlaceInto(monument, $"{CityFbx}/low-detail-building-a.fbx",
                new Vector3(0f, 0f, 0f), Quaternion.Euler(15f, 0f, 5f), Vector3.one * 0.8f, _matServer);
            PlaceInto(monument, $"{CityFbx}/low-detail-building-b.fbx",
                new Vector3(1.5f, 0f, -0.5f), Quaternion.Euler(0f, 45f, -10f), Vector3.one * 0.6f, _matServer);
            PlaceInto(monument, $"{NatureFbx}/cliff_block_rock.fbx",
                new Vector3(-1.5f, 0f, 0.5f), Quaternion.Euler(0f, 10f, 0f), Vector3.one * 1.2f, _matRock);

            // Hide boundary walls
            var walls = GameObject.Find("Boundary_Walls");
            if (walls != null)
            {
                foreach (var r in walls.GetComponentsInChildren<Renderer>()) r.enabled = false;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (saved) Debug.Log("[DesertDresser] Scene_Desert saved");

            AssetDatabase.SaveAssets();
        }

        private static Material PickMaterial(string assetName)
        {
            string n = assetName.ToLowerInvariant();
            if (n.Contains("cactus")) return _matCactus;
            if (n.Contains("palm")) return _matDeadPalm;
            if (n.Contains("cliff") || n.Contains("rock") || n.Contains("stone")) return _matRock;
            return _matSand;
        }

        private static void Place(GameObject parent, string assetPath, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            PlaceInto(parent, assetPath, pos, rot, scale, PickMaterial(assetName));
        }

        private static void PlaceInto(GameObject parent, string assetPath, Vector3 pos, Quaternion rot, Vector3 scale, Material tint)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError($"[DesertDresser] Asset not found: {assetPath}");
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
                for (int i = 0; i < count; i++) mats[i] = tint;
                r.sharedMaterials = mats;
            }

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
