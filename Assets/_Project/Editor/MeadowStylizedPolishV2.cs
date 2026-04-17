using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using URandom = UnityEngine.Random;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Deeper stylized polish pass to reach the Quaternius-reference look without
    /// the actual MegaKit FBX. Builds on the existing Meadow_Greybox_Root trees
    /// and Meadow_StylizedPolish_Root grass/flowers/mushrooms:
    ///  - Wraps every greybox Crown sphere with 3–5 extra spheres ("puffy foliage")
    ///    of slightly different tint, offset around the original crown position.
    ///  - Tilts trunks 3–8° forward/sideways for an organic, non-stiff look.
    ///  - Adds "fallen logs" (prone cylinders) along the tree ring.
    ///  - Adds "fern" clusters — 3 spheres in cross pattern — near the trunks.
    ///  - Adds 2 darker-green grass sprays (tall) and 2 lighter for variety.
    ///  - Adds stump remnants (short trunks with no crown) to break up the ring.
    ///
    /// Menu: Afterhumans → Meadow → Stylized Polish V2
    /// </summary>
    public static class MeadowStylizedPolishV2
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";
        private const string GreyboxTag = "MeadowGreybox";
        private const string MatDir = "Assets/_Project/Materials/Nature";
        private const int Seed = 1717;
        private const int FallenLogCount = 6;
        private const int FernCount = 45;
        private const int StumpCount = 10;

        [MenuItem("Afterhumans/Meadow/Stylized Polish V2")]
        public static void Polish()
        {
            Debug.Log("[PolishV2] Starting...");
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            WipePrevious(scene);

            var crownMid = LoadMat(MatDir + "/Mat_Tree_Crown_Greybox.mat", new Color(0.29f, 0.55f, 0.23f));
            var crownLight = LoadMat(MatDir + "/Mat_Tree_Crown_Light.mat", new Color(0.48f, 0.70f, 0.32f));
            var crownDark = LoadMat(MatDir + "/Mat_Tree_Crown_Dark.mat", new Color(0.18f, 0.38f, 0.18f));
            var crownAutumn = LoadMat(MatDir + "/Mat_Tree_Crown_Autumn.mat", new Color(0.88f, 0.42f, 0.18f));
            var trunkMat = LoadMat(MatDir + "/Mat_Tree_Trunk_Greybox.mat", new Color(0.35f, 0.25f, 0.18f));
            var fernMat = LoadMat(MatDir + "/Mat_Fern.mat", new Color(0.34f, 0.58f, 0.27f));
            Material[] crownPalette = { crownMid, crownLight, crownDark, crownAutumn };

            URandom.InitState(Seed);

            PuffyCrowns(scene, crownPalette);
            TiltTrunks(scene);

            var root = new GameObject("Meadow_PolishV2_Root");
            root.tag = GreyboxTag;

            PlaceFallenLogs(root.transform, trunkMat);
            PlaceFerns(root.transform, fernMat);
            PlaceStumps(root.transform, trunkMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PolishV2] DONE. Fallen logs {FallenLogCount}, ferns {FernCount}, stumps {StumpCount}. Crowns puffied + trunks tilted.");
        }

        private static void WipePrevious(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Meadow_PolishV2_Root")
                    Object.DestroyImmediate(root);
        }

        private static void PuffyCrowns(Scene scene, Material[] palette)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Greybox_Root") continue;
                var crowns = new List<Transform>();
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Crown") crowns.Add(t);

                foreach (var c in crowns)
                {
                    // Skip if already has puffies
                    bool hasPuff = false;
                    for (int i = 0; i < c.childCount; i++)
                    {
                        if (c.GetChild(i).name.StartsWith("Puff_")) { hasPuff = true; break; }
                    }
                    if (hasPuff) continue;

                    int puffCount = URandom.Range(3, 6);
                    for (int p = 0; p < puffCount; p++)
                    {
                        var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        puff.name = $"Puff_{p}";
                        puff.tag = GreyboxTag;
                        puff.transform.SetParent(c, false);
                        Vector3 dir = URandom.onUnitSphere;
                        dir.y = Mathf.Abs(dir.y) * 0.4f + 0.2f;
                        float off = URandom.Range(0.35f, 0.65f);
                        puff.transform.localPosition = dir * off;
                        float ps = URandom.Range(0.65f, 0.95f);
                        puff.transform.localScale = new Vector3(ps, ps, ps);
                        puff.GetComponent<Renderer>().sharedMaterial = palette[URandom.Range(0, palette.Length)];
                        Object.DestroyImmediate(puff.GetComponent<Collider>());
                        puff.isStatic = true;
                    }
                }
                break;
            }
        }

        private static void TiltTrunks(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Greybox_Root") continue;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("Tree_")) continue;
                    // Apply subtle random tilt on the parent tree (not trunk) around its base.
                    Vector3 tilt = new Vector3(URandom.Range(-5f, 5f), t.eulerAngles.y, URandom.Range(-5f, 5f));
                    t.localEulerAngles = tilt;
                }
                break;
            }
        }

        private static void PlaceFallenLogs(Transform parent, Material trunkMat)
        {
            var group = new GameObject("FallenLogs");
            group.tag = GreyboxTag;
            group.transform.SetParent(parent, false);
            for (int i = 0; i < FallenLogCount; i++)
            {
                float angle = URandom.Range(0f, Mathf.PI * 2f);
                float r = URandom.Range(24f, 34f);
                Vector3 pos = new Vector3(Mathf.Cos(angle) * r, 0.2f, Mathf.Sin(angle) * r);

                var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.name = $"FallenLog_{i:00}";
                log.tag = GreyboxTag;
                log.transform.SetParent(group.transform, false);
                log.transform.position = pos;
                log.transform.rotation = Quaternion.Euler(90f, URandom.Range(0f, 360f), URandom.Range(-5f, 5f));
                float len = URandom.Range(1.5f, 2.8f);
                log.transform.localScale = new Vector3(0.35f, len, 0.35f);
                log.GetComponent<Renderer>().sharedMaterial = trunkMat;
                log.isStatic = true;
            }
        }

        private static void PlaceFerns(Transform parent, Material fernMat)
        {
            var group = new GameObject("Ferns");
            group.tag = GreyboxTag;
            group.transform.SetParent(parent, false);
            for (int i = 0; i < FernCount; i++)
            {
                float angle = URandom.Range(0f, Mathf.PI * 2f);
                float r = URandom.Range(10f, 33f);
                Vector3 center = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);

                var cluster = new GameObject($"Fern_{i:00}");
                cluster.tag = GreyboxTag;
                cluster.transform.SetParent(group.transform, false);
                cluster.transform.position = center;
                cluster.transform.rotation = Quaternion.Euler(0f, URandom.Range(0f, 360f), 0f);

                // 3 ellipsoid sphere "fronds"
                for (int f = 0; f < 3; f++)
                {
                    var frond = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    frond.name = $"Frond_{f}";
                    frond.tag = GreyboxTag;
                    frond.transform.SetParent(cluster.transform, false);
                    float fy = 60f * f;
                    frond.transform.localRotation = Quaternion.Euler(25f, fy, 0f);
                    frond.transform.localPosition = new Vector3(0f, 0.18f, 0f);
                    frond.transform.localScale = new Vector3(0.8f, 0.15f, 0.3f);
                    frond.GetComponent<Renderer>().sharedMaterial = fernMat;
                    Object.DestroyImmediate(frond.GetComponent<Collider>());
                    frond.isStatic = true;
                }
            }
        }

        private static void PlaceStumps(Transform parent, Material trunkMat)
        {
            var group = new GameObject("Stumps");
            group.tag = GreyboxTag;
            group.transform.SetParent(parent, false);
            for (int i = 0; i < StumpCount; i++)
            {
                float angle = URandom.Range(0f, Mathf.PI * 2f);
                float r = URandom.Range(22f, 35f);
                var stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stump.name = $"Stump_{i:00}";
                stump.tag = GreyboxTag;
                stump.transform.SetParent(group.transform, false);
                float h = URandom.Range(0.25f, 0.55f);
                stump.transform.localScale = new Vector3(0.45f, h, 0.45f);
                stump.transform.position = new Vector3(Mathf.Cos(angle) * r, h, Mathf.Sin(angle) * r);
                stump.GetComponent<Renderer>().sharedMaterial = trunkMat;
                stump.isStatic = true;
            }
        }

        private static Material LoadMat(string path, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                var dir = Path.GetDirectoryName(path);
                string fs = Path.Combine(Directory.GetCurrentDirectory(), dir);
                if (!Directory.Exists(fs)) { Directory.CreateDirectory(fs); AssetDatabase.Refresh(); }
                mat = new Material(unlit) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.shader != unlit) mat.shader = unlit;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
