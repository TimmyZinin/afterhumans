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
    /// Procedural greybox forest placer — 60 trees (cylinder + sphere),
    /// 15 rocks (cubes), 20 bushes (spheres) scattered around a 80×80m meadow.
    /// Seeded random (42) for reproducibility. All objects tagged MeadowGreybox
    /// so Sprint 7 MeadowForestBuilder can wipe them before MegaKit replacement.
    ///
    /// Menu: Afterhumans → Greybox → Build Meadow Forest
    /// </summary>
    public static class MeadowGreyboxBuilder
    {
        private const string GreyboxTag = "MeadowGreybox";
        private const string TrunkMatPath = "Assets/_Project/Materials/Nature/Mat_Tree_Trunk_Greybox.mat";
        private const string CrownMatPath = "Assets/_Project/Materials/Nature/Mat_Tree_Crown_Greybox.mat";
        private const string RockMatPath = "Assets/_Project/Materials/Nature/Mat_Rock_Greybox.mat";
        private const string BushMatPath = "Assets/_Project/Materials/Nature/Mat_Bush_Greybox.mat";

        private const int TreeCount = 60;
        private const int RingTreeCount = 40;
        private const int RockCount = 15;
        private const int BushCount = 20;

        private const float InnerRadius = 6f;
        private const float ScatterOuter = 30f;
        private const float RingInner = 25f;
        private const float RingOuter = 38f;
        private const int Seed = 42;
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";

        [MenuItem("Afterhumans/Greybox/Build Meadow Forest")]
        public static void Build()
        {
            Debug.Log("[MeadowGreybox] Starting...");

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!File.Exists(ScenePath))
                {
                    Debug.LogError($"[MeadowGreybox] Scene not found at {ScenePath}. Run 'Bootstrap Sandbox Scene' first.");
                    return;
                }
                Debug.Log($"[MeadowGreybox] Opening {ScenePath}");
                active = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            EnsureTagExists();
            var trunkMat = LoadOrCreate(TrunkMatPath, new Color(0.35f, 0.25f, 0.18f));
            var crownMat = LoadOrCreate(CrownMatPath, new Color(0.29f, 0.48f, 0.23f));
            var rockMat = LoadOrCreate(RockMatPath, new Color(0.48f, 0.46f, 0.45f));
            var bushMat = LoadOrCreate(BushMatPath, new Color(0.41f, 0.60f, 0.38f));

            int wiped = WipeExisting();
            Debug.Log($"[MeadowGreybox] Wiped {wiped} previous greybox objects.");

            var root = new GameObject("Meadow_Greybox_Root");
            root.tag = GreyboxTag;
            root.isStatic = true;

            URandom.InitState(Seed);

            var treeParent = MakeChild(root.transform, "Trees").transform;
            for (int i = 0; i < RingTreeCount; i++)
            {
                Vector2 p = RandomRing(RingInner, RingOuter);
                SpawnTree(treeParent, $"Tree_Ring_{i:00}", new Vector3(p.x, 0f, p.y), trunkMat, crownMat);
            }
            int scatter = TreeCount - RingTreeCount;
            for (int i = 0; i < scatter; i++)
            {
                Vector2 p = RandomRing(InnerRadius, ScatterOuter);
                SpawnTree(treeParent, $"Tree_Scatter_{i:00}", new Vector3(p.x, 0f, p.y), trunkMat, crownMat);
            }

            var rockParent = MakeChild(root.transform, "Rocks").transform;
            for (int i = 0; i < RockCount; i++)
            {
                Vector2 p = RandomRing(InnerRadius + 1f, ScatterOuter);
                SpawnRock(rockParent, $"Rock_{i:00}", new Vector3(p.x, 0f, p.y), rockMat);
            }

            var bushParent = MakeChild(root.transform, "Bushes").transform;
            for (int i = 0; i < BushCount; i++)
            {
                Vector2 p = RandomRing(InnerRadius, ScatterOuter);
                SpawnBush(bushParent, $"Bush_{i:00}", new Vector3(p.x, 0f, p.y), bushMat);
            }

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MeadowGreybox] DONE. Spawned {TreeCount} trees, {RockCount} rocks, {BushCount} bushes under {root.name}.");
        }

        [MenuItem("Afterhumans/Greybox/Wipe Meadow Greybox")]
        public static void Wipe()
        {
            int n = WipeExisting();
            Debug.Log($"[MeadowGreybox] Wiped {n} objects.");
        }

        private static int WipeExisting()
        {
            int count = 0;
            var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            foreach (var root in active.GetRootGameObjects())
            {
                if (root.name == "Meadow_Greybox_Root" || root.CompareTag(GreyboxTag) && root.name != "Meadow_Ground")
                {
                    count += 1 + CountChildren(root.transform);
                    Object.DestroyImmediate(root);
                }
            }
            return count;
        }

        private static int CountChildren(Transform t)
        {
            int n = 0;
            for (int i = 0; i < t.childCount; i++)
                n += 1 + CountChildren(t.GetChild(i));
            return n;
        }

        private static void SpawnTree(Transform parent, string name, Vector3 pos, Material trunkMat, Material crownMat)
        {
            var go = new GameObject(name);
            go.tag = GreyboxTag;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, URandom.Range(0f, 360f), 0f);
            go.transform.localScale = Vector3.one * URandom.Range(0.85f, 1.2f);
            go.isStatic = true;

            int variant = URandom.Range(0, 3);
            float trunkR, trunkH, crownR, crownYOffset;
            switch (variant)
            {
                case 0: trunkR = 0.22f; trunkH = 3.6f; crownR = 1.5f; crownYOffset = 2.9f; break;
                case 1: trunkR = 0.35f; trunkH = 2.6f; crownR = 2.0f; crownYOffset = 2.3f; break;
                default: trunkR = 0.28f; trunkH = 2.9f; crownR = 2.3f; crownYOffset = 2.4f; break;
            }

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(go.transform, false);
            trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
            trunk.transform.localScale = new Vector3(trunkR * 2f, trunkH * 0.5f, trunkR * 2f);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;
            trunk.tag = GreyboxTag;
            trunk.isStatic = true;

            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Crown";
            crown.transform.SetParent(go.transform, false);
            crown.transform.localPosition = new Vector3(0f, crownYOffset, 0f);
            crown.transform.localScale = new Vector3(crownR * 2f, crownR * 2f, crownR * 2f);
            crown.GetComponent<Renderer>().sharedMaterial = crownMat;
            Object.DestroyImmediate(crown.GetComponent<Collider>());
            crown.tag = GreyboxTag;
            crown.isStatic = true;
        }

        private static void SpawnRock(Transform parent, string name, Vector3 pos, Material mat)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = name;
            rock.tag = GreyboxTag;
            rock.transform.SetParent(parent, false);
            rock.transform.position = pos + new Vector3(0f, 0.2f, 0f);
            rock.transform.rotation = Quaternion.Euler(URandom.Range(-10f, 10f), URandom.Range(0f, 360f), URandom.Range(-10f, 10f));
            float s = URandom.Range(0.5f, 1.5f);
            rock.transform.localScale = new Vector3(s, s * URandom.Range(0.6f, 1.0f), s);
            rock.GetComponent<Renderer>().sharedMaterial = mat;
            rock.isStatic = true;
        }

        private static void SpawnBush(Transform parent, string name, Vector3 pos, Material mat)
        {
            var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = name;
            bush.tag = GreyboxTag;
            bush.transform.SetParent(parent, false);
            bush.transform.position = pos + new Vector3(0f, 0.4f, 0f);
            float s = URandom.Range(0.7f, 1.1f);
            bush.transform.localScale = new Vector3(s, s * 0.75f, s);
            bush.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(bush.GetComponent<Collider>());
            bush.isStatic = true;
        }

        private static Vector2 RandomRing(float inner, float outer)
        {
            float angle = URandom.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(URandom.Range(inner * inner, outer * outer));
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        private static GameObject MakeChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.tag = GreyboxTag;
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Material LoadOrCreate(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            EnsureAssetFolder(Path.GetDirectoryName(path));
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void EnsureAssetFolder(string assetDir)
        {
            string fs = Path.Combine(Directory.GetCurrentDirectory(), assetDir);
            if (!Directory.Exists(fs))
            {
                Directory.CreateDirectory(fs);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureTagExists()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == GreyboxTag) return;
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = GreyboxTag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
