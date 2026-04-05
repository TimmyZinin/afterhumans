using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Populates Scene_City with Kenney city-kit-commercial FBX — a grid of buildings
    /// and skyscrapers forming an empty street corridor. Clean, sterile, cool-lit
    /// to contrast Botanika's warm oasis.
    ///
    /// Shares the Tints materials authored by BotanikaDresser.
    /// </summary>
    public static class CityDresser
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_City.unity";
        private const string KenneyBase = "Assets/_Project/Vendor/Kenney";
        private const string CityFbx = KenneyBase + "/city-kit-commercial/Models/FBX format";
        private const string PropsRootName = "City_Props";
        private const string MaterialsDir = "Assets/_Project/Materials/Tints";

        private static Material _matConcrete;
        private static Material _matGlass;
        private static Material _matAsphalt;

        private static Material LoadOrCreateLit(string name, Color color, float smoothness = 0.2f)
        {
            if (!Directory.Exists(MaterialsDir)) Directory.CreateDirectory(MaterialsDir);
            string path = $"{MaterialsDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
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

        [MenuItem("Afterhumans/Setup/Dress City")]
        public static void Dress()
        {
            Debug.Log("[CityDresser] Opening Scene_City...");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            _matConcrete = LoadOrCreateLit("Tint_Concrete", new Color(0.82f, 0.84f, 0.87f), 0.3f);
            _matGlass = LoadOrCreateLit("Tint_Glass", new Color(0.65f, 0.78f, 0.90f), 0.8f);
            _matAsphalt = LoadOrCreateLit("Tint_Asphalt", new Color(0.35f, 0.35f, 0.37f), 0.2f);

            var existing = GameObject.Find(PropsRootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var propsRoot = new GameObject(PropsRootName);

            // Player spawn at one end of the street
            var player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(0f, 1.1f, -14f);
                player.transform.rotation = Quaternion.identity;
                Debug.Log("[CityDresser] Player spawn at (0, 1.1, -14)");
            }

            // Move NPC Dmitriy near the far end as the one who opens the city door
            var dmitriy = GameObject.Find("Placeholder_NPC_Dmitriy");
            if (dmitriy != null)
            {
                dmitriy.transform.position = new Vector3(0f, 1f, 8f);
                dmitriy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // Build two rows of buildings flanking a narrow street
            // Left row (−X)
            string[] leftBuildings = {
                "building-a", "building-skyscraper-a", "building-c",
                "building-skyscraper-b", "building-e", "building-skyscraper-c",
            };
            for (int i = 0; i < leftBuildings.Length; i++)
            {
                float z = -14f + i * 6f;
                Place(propsRoot, $"{CityFbx}/{leftBuildings[i]}.fbx",
                    new Vector3(-6f, 0f, z),
                    Quaternion.Euler(0f, 90f, 0f),
                    Vector3.one * 1.2f);
            }

            // Right row (+X)
            string[] rightBuildings = {
                "building-b", "building-d", "building-skyscraper-d",
                "building-f", "building-skyscraper-e", "building-g",
            };
            for (int i = 0; i < rightBuildings.Length; i++)
            {
                float z = -14f + i * 6f;
                Place(propsRoot, $"{CityFbx}/{rightBuildings[i]}.fbx",
                    new Vector3(6f, 0f, z),
                    Quaternion.Euler(0f, -90f, 0f),
                    Vector3.one * 1.2f);
            }

            // A couple of low-detail buildings at the far back as skyline
            Place(propsRoot, $"{CityFbx}/low-detail-building-a.fbx",
                new Vector3(-10f, 0f, 18f), Quaternion.identity, Vector3.one * 1.5f);
            Place(propsRoot, $"{CityFbx}/low-detail-building-b.fbx",
                new Vector3(0f, 0f, 20f), Quaternion.identity, Vector3.one * 1.5f);
            Place(propsRoot, $"{CityFbx}/low-detail-building-c.fbx",
                new Vector3(10f, 0f, 18f), Quaternion.identity, Vector3.one * 1.5f);

            // Street parasols for some street-level detail
            Place(propsRoot, $"{CityFbx}/detail-parasol-a.fbx",
                new Vector3(-2f, 0f, -6f), Quaternion.identity, Vector3.one);
            Place(propsRoot, $"{CityFbx}/detail-parasol-b.fbx",
                new Vector3(2f, 0f, -2f), Quaternion.identity, Vector3.one);
            Place(propsRoot, $"{CityFbx}/detail-parasol-a.fbx",
                new Vector3(-2f, 0f, 4f), Quaternion.identity, Vector3.one);

            // Hide placeholder walls so the city feels like a real street
            var walls = GameObject.Find("Boundary_Walls");
            if (walls != null)
            {
                foreach (var r in walls.GetComponentsInChildren<Renderer>()) r.enabled = false;
                Debug.Log("[CityDresser] Hid boundary wall renderers");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (saved) Debug.Log("[CityDresser] Scene_City saved");
            else Debug.LogError("[CityDresser] Failed to save Scene_City");

            AssetDatabase.SaveAssets();
        }

        private static Material PickMaterial(string assetName)
        {
            string n = assetName.ToLowerInvariant();
            if (n.Contains("skyscraper")) return _matGlass;
            if (n.Contains("parasol") || n.Contains("awning") || n.Contains("overhang")) return _matAsphalt;
            return _matConcrete;
        }

        private static void Place(GameObject parent, string assetPath, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError($"[CityDresser] Asset not found: {assetPath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) return;

            instance.transform.SetParent(parent.transform, worldPositionStays: false);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.localScale = scale;

            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            var tint = PickMaterial(assetName);
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
