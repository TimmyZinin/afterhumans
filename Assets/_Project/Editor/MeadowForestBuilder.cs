using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using URandom = UnityEngine.Random;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Replaces the Meadow_Greybox_Root (primitives) with Stylized Nature MegaKit
    /// FBX prefabs. Same seed as MeadowGreyboxBuilder (42) so layout stays consistent.
    ///
    /// Prerequisites:
    ///   1. Assets/ThirdParty/StylizedNature/FBX (Unity)/*.fbx must exist
    ///      (unzip Stylized Nature MegaKit[Standard].zip into that folder).
    ///   2. Stylized Nature materials should exist under
    ///      Assets/_Project/Materials/Nature/Stylized/Mat_*.mat
    ///      (run 'Afterhumans/Meadow/Build Stylized Nature Materials' first).
    ///
    /// Menu: Afterhumans → Meadow → Build Stylized Forest
    /// </summary>
    public static class MeadowForestBuilder
    {
        private const string GreyboxTag = "MeadowGreybox";
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";
        private const string FbxDir = "Assets/ThirdParty/StylizedNature/FBX (Unity)";
        private const string MatDir = "Assets/_Project/Materials/Nature/Stylized";

        private const int TreeCount = 60;
        private const int RingTreeCount = 40;
        private const int RockCount = 15;
        private const int BushCount = 20;
        private const int GrassCount = 120;
        private const int FlowerCount = 40;
        private const int FernCount = 15;
        private const int MushroomCount = 8;

        private const float InnerRadius = 6f;
        private const float ScatterOuter = 30f;
        private const float RingInner = 25f;
        private const float RingOuter = 38f;
        private const float MeadowHalf = 38f;
        private const int Seed = 42;

        // Tree species pool (ringed forest ~60 trees).
        // 70% Common+Pine for bulk, 15% Dead for character, 15% Twisted.
        private static readonly string[] CommonTreeSpecies = new[]
        {
            "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5",
            "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5",
        };
        private static readonly string[] DeadTreeSpecies = new[]
        {
            "DeadTree_1", "DeadTree_2", "DeadTree_3", "DeadTree_4", "DeadTree_5",
        };
        private static readonly string[] TwistedTreeSpecies = new[]
        {
            "TwistedTree_1", "TwistedTree_2", "TwistedTree_3", "TwistedTree_4", "TwistedTree_5",
        };
        private static readonly string[] RockSpecies = new[] { "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3" };
        private static readonly string[] GrassSpecies = new[]
        {
            "Grass_Common_Short", "Grass_Common_Tall", "Grass_Wispy_Short", "Grass_Wispy_Tall",
        };
        private static readonly string[] FlowerSpecies = new[]
        {
            "Flower_3_Single", "Flower_3_Group", "Flower_4_Single", "Flower_4_Group",
        };
        private static readonly string[] FernSpecies = new[] { "Fern_1", "Clover_1", "Clover_2" };
        private static readonly string[] MushroomSpecies = new[] { "Mushroom_Common", "Mushroom_Laetiporus" };
        private static readonly string[] BushSpecies = new[] { "Bush_Common", "Bush_Common_Flowers" };

        // Asset-name prefix → material slots (ordered). Unity will preserve extra slots untouched.
        private static readonly Dictionary<string, string[]> MaterialMap = new Dictionary<string, string[]>
        {
            { "CommonTree",   new[] { "Mat_Bark_NormalTree",  "Mat_Leaves_NormalTree" } },
            { "Pine",         new[] { "Mat_Bark_NormalTree",  "Mat_Leaves_Pine" } },
            { "DeadTree",     new[] { "Mat_Bark_DeadTree" } },
            { "TwistedTree",  new[] { "Mat_Bark_TwistedTree", "Mat_Leaves_TwistedTree" } },
            { "Rock_Medium",  new[] { "Mat_Rocks_Medium" } },
            { "Grass",        new[] { "Mat_Grass" } },
            { "Flower",       new[] { "Mat_Flowers" } },
            { "Fern",         new[] { "Mat_Grass" } },
            { "Clover",       new[] { "Mat_Grass" } },
            { "Mushroom",     new[] { "Mat_Mushroom" } },
            { "Bush",         new[] { "Mat_Leaves_Generic" } },
        };

        [MenuItem("Afterhumans/Meadow/Build Stylized Forest")]
        public static void Build()
        {
            Debug.Log("[MeadowForest] Starting...");

            if (!Directory.Exists(FbxDir))
            {
                Debug.LogError($"[MeadowForest] FBX folder missing: {FbxDir}. Unpack the MegaKit zip first.");
                return;
            }

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!File.Exists(ScenePath))
                {
                    Debug.LogError($"[MeadowForest] Scene missing: {ScenePath}. Bootstrap it first.");
                    return;
                }
                Debug.Log($"[MeadowForest] Opening {ScenePath}");
                active = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            int wiped = WipeExisting();
            Debug.Log($"[MeadowForest] Wiped {wiped} previous greybox/forest objects.");

            var root = new GameObject("Meadow_Forest_Root");
            root.tag = GreyboxTag;
            root.isStatic = true;

            URandom.InitState(Seed);

            int trees = PlaceTrees(root.transform);
            int rocks = PlaceCategory(root.transform, "Rocks", RockSpecies, RockCount,
                InnerRadius + 1f, ScatterOuter, scaleMin: 0.9f, scaleMax: 1.4f, addCollider: true);
            int grass = PlaceCategory(root.transform, "Grass", GrassSpecies, GrassCount,
                InnerRadius, MeadowHalf, scaleMin: 0.6f, scaleMax: 1.4f, addCollider: false);
            int flowers = PlaceCategory(root.transform, "Flowers", FlowerSpecies, FlowerCount,
                InnerRadius, MeadowHalf - 2f, scaleMin: 0.7f, scaleMax: 1.1f, addCollider: false);
            int ferns = PlaceCategory(root.transform, "Ferns", FernSpecies, FernCount,
                InnerRadius, ScatterOuter, scaleMin: 0.7f, scaleMax: 1.2f, addCollider: false);
            int mushrooms = PlaceCategory(root.transform, "Mushrooms", MushroomSpecies, MushroomCount,
                RingInner, RingOuter, scaleMin: 0.9f, scaleMax: 1.3f, addCollider: false);
            int bushes = PlaceCategory(root.transform, "Bushes", BushSpecies, BushCount,
                InnerRadius, ScatterOuter, scaleMin: 0.8f, scaleMax: 1.2f, addCollider: false);

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MeadowForest] DONE. Trees {trees}, rocks {rocks}, grass {grass}, " +
                      $"flowers {flowers}, ferns/clovers {ferns}, mushrooms {mushrooms}, bushes {bushes}.");
        }

        private static int PlaceTrees(Transform parentRoot)
        {
            var parent = MakeChild(parentRoot, "Trees");
            int placed = 0;

            for (int i = 0; i < RingTreeCount; i++)
            {
                Vector2 p = RandomRing(RingInner, RingOuter);
                string species = PickTreeSpecies();
                if (Spawn(parent, species, $"Tree_Ring_{i:00}_{species}",
                        new Vector3(p.x, 0f, p.y),
                        URandom.Range(0.85f, 1.25f),
                        addCollider: true))
                    placed++;
            }
            int scatter = TreeCount - RingTreeCount;
            for (int i = 0; i < scatter; i++)
            {
                Vector2 p = RandomRing(InnerRadius, ScatterOuter);
                string species = PickTreeSpecies();
                if (Spawn(parent, species, $"Tree_Scatter_{i:00}_{species}",
                        new Vector3(p.x, 0f, p.y),
                        URandom.Range(0.85f, 1.25f),
                        addCollider: true))
                    placed++;
            }
            return placed;
        }

        private static int PlaceCategory(Transform parentRoot, string groupName, string[] species,
            int count, float inner, float outer, float scaleMin, float scaleMax, bool addCollider)
        {
            var parent = MakeChild(parentRoot, groupName);
            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = RandomRing(inner, outer);
                string s = species[URandom.Range(0, species.Length)];
                if (Spawn(parent, s, $"{groupName}_{i:000}_{s}", new Vector3(p.x, 0f, p.y),
                        URandom.Range(scaleMin, scaleMax), addCollider))
                    placed++;
            }
            return placed;
        }

        private static string PickTreeSpecies()
        {
            float roll = URandom.value;
            if (roll < 0.70f) return CommonTreeSpecies[URandom.Range(0, CommonTreeSpecies.Length)];
            if (roll < 0.85f) return DeadTreeSpecies[URandom.Range(0, DeadTreeSpecies.Length)];
            return TwistedTreeSpecies[URandom.Range(0, TwistedTreeSpecies.Length)];
        }

        private static bool Spawn(Transform parent, string fbxName, string instanceName,
            Vector3 pos, float scale, bool addCollider)
        {
            string path = $"{FbxDir}/{fbxName}.fbx";
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (fbx == null)
            {
                Debug.LogWarning($"[MeadowForest] FBX missing: {path}");
                return false;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
            inst.name = instanceName;
            inst.transform.localPosition = pos;
            inst.transform.localRotation = Quaternion.Euler(0f, URandom.Range(0f, 360f), 0f);
            inst.transform.localScale = Vector3.one * scale;
            inst.tag = GreyboxTag;

            foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                t.gameObject.isStatic = true;

            ApplyStylizedMaterials(inst, fbxName);

            if (addCollider)
            {
                foreach (var mf in inst.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    if (mf.GetComponent<Collider>() != null) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                }
            }
            return true;
        }

        private static void ApplyStylizedMaterials(GameObject inst, string fbxName)
        {
            string key = MapPrefixKey(fbxName);
            if (key == null || !MaterialMap.TryGetValue(key, out var matNames)) return;

            var materials = new List<Material>();
            foreach (var name in matNames)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");
                if (mat != null) materials.Add(mat);
            }
            if (materials.Count == 0) return;

            foreach (var r in inst.GetComponentsInChildren<Renderer>())
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length && i < materials.Count; i++)
                    slots[i] = materials[i];
                if (slots.Length > materials.Count)
                    for (int i = materials.Count; i < slots.Length; i++)
                        slots[i] = materials[materials.Count - 1];
                r.sharedMaterials = slots;
            }
        }

        private static string MapPrefixKey(string fbxName)
        {
            foreach (var kv in MaterialMap)
                if (fbxName.StartsWith(kv.Key)) return kv.Key;
            return null;
        }

        private static int WipeExisting()
        {
            int count = 0;
            var active = EditorSceneManager.GetActiveScene();
            var toKill = new List<GameObject>();
            foreach (var root in active.GetRootGameObjects())
            {
                if (root.name == "Meadow_Greybox_Root" || root.name == "Meadow_Forest_Root")
                {
                    toKill.Add(root);
                    count += 1 + CountChildren(root.transform);
                }
            }
            foreach (var go in toKill) Object.DestroyImmediate(go);
            return count;
        }

        private static int CountChildren(Transform t)
        {
            int n = 0;
            for (int i = 0; i < t.childCount; i++)
                n += 1 + CountChildren(t.GetChild(i));
            return n;
        }

        private static Vector2 RandomRing(float inner, float outer)
        {
            float angle = URandom.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(URandom.Range(inner * inner, outer * outer));
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        private static Transform MakeChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.tag = GreyboxTag;
            go.transform.SetParent(parent, false);
            return go.transform;
        }
    }
}
