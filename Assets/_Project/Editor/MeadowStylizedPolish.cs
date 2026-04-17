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
    /// Stylized polish pass on top of the greybox meadow, since a full MegaKit
    /// FBX import is prohibitive on M1 8GB (Unity stalls for 40+ min on 454
    /// models). This brings the greybox closer to the reference Quaternius
    /// Stylized Nature look using only Unity primitives + multi-tint materials
    /// + URP post-processing.
    ///
    /// Adds in an existing Scene_MeadowForest_Greybox:
    ///  - Three-tone tree crowns (light / mid / autumn orange) via material swaps
    ///  - Grass tufts: ~250 short vertical cylinders
    ///  - Mushroom clusters: white caps under trees
    ///  - Flower patches: small red/yellow/pink spheres
    ///  - Warm golden-hour directional light
    ///  - URP Volume: Bloom + Vignette + Color Adjustments (warm, +saturation)
    ///
    /// Menu: Afterhumans → Meadow → Stylized Polish
    /// </summary>
    public static class MeadowStylizedPolish
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";
        private const string GreyboxTag = "MeadowGreybox";
        private const string MatDir = "Assets/_Project/Materials/Nature";
        private const string VolumeProfilePath = "Assets/_Project/Settings/URP/VolumeProfiles/VP_MeadowForest.asset";

        private const int GrassCount = 250;
        private const int FlowerCount = 60;
        private const int MushroomCount = 18;
        private const int Seed = 4242;
        private const float OuterRadius = 36f;
        private const float InnerRadius = 2f;

        [MenuItem("Afterhumans/Meadow/Stylized Polish")]
        public static void Polish()
        {
            Debug.Log("[StylizedPolish] Starting...");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!File.Exists(ScenePath))
                {
                    Debug.LogError($"[StylizedPolish] Scene missing: {ScenePath}");
                    return;
                }
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            WipePreviousPolish(scene);

            var crownLight = LoadOrUpdate(MatDir + "/Mat_Tree_Crown_Light.mat", new Color(0.48f, 0.70f, 0.32f));
            var crownMid = LoadOrUpdate(MatDir + "/Mat_Tree_Crown_Greybox.mat", new Color(0.29f, 0.55f, 0.23f));
            var crownAutumn = LoadOrUpdate(MatDir + "/Mat_Tree_Crown_Autumn.mat", new Color(0.88f, 0.42f, 0.18f));
            var grassMat = LoadOrUpdate(MatDir + "/Mat_Grass_Tuft.mat", new Color(0.55f, 0.78f, 0.32f));
            var mushroomMat = LoadOrUpdate(MatDir + "/Mat_Mushroom.mat", new Color(0.96f, 0.92f, 0.82f));
            var flowerRed = LoadOrUpdate(MatDir + "/Mat_Flower_Red.mat", new Color(0.92f, 0.28f, 0.24f));
            var flowerYellow = LoadOrUpdate(MatDir + "/Mat_Flower_Yellow.mat", new Color(0.97f, 0.85f, 0.26f));
            var flowerPink = LoadOrUpdate(MatDir + "/Mat_Flower_Pink.mat", new Color(0.95f, 0.56f, 0.72f));

            RandomizeTreeCrowns(crownLight, crownMid, crownAutumn);

            var root = new GameObject("Meadow_StylizedPolish_Root");
            root.tag = GreyboxTag;

            URandom.InitState(Seed);
            PlaceGrass(root.transform, grassMat);
            PlaceMushrooms(root.transform, mushroomMat);
            PlaceFlowers(root.transform, flowerRed, flowerYellow, flowerPink);

            TuneLighting();
            TuneVolume();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[StylizedPolish] DONE. Grass {GrassCount}, flowers {FlowerCount}, mushrooms {MushroomCount}. Crowns re-tinted.");
        }

        private static void WipePreviousPolish(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Meadow_StylizedPolish_Root")
                    Object.DestroyImmediate(root);
            }
        }

        private static void RandomizeTreeCrowns(Material light, Material mid, Material autumn)
        {
            URandom.InitState(Seed + 1);
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Meadow_Greybox_Root") continue;
                var crowns = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in crowns)
                {
                    if (t.name != "Crown") continue;
                    var r = t.GetComponent<Renderer>();
                    if (r == null) continue;
                    float roll = URandom.value;
                    Material chosen = roll < 0.55f ? mid : (roll < 0.85f ? light : autumn);
                    r.sharedMaterial = chosen;
                }
                break;
            }
        }

        private static void PlaceGrass(Transform parent, Material mat)
        {
            var group = new GameObject("Grass_Tufts");
            group.tag = GreyboxTag;
            group.transform.SetParent(parent, false);
            group.isStatic = true;
            for (int i = 0; i < GrassCount; i++)
            {
                Vector2 p = RandomDisk(InnerRadius, OuterRadius);
                var tuft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tuft.name = $"Grass_{i:000}";
                tuft.tag = GreyboxTag;
                tuft.transform.SetParent(group.transform, false);
                float h = URandom.Range(0.15f, 0.35f);
                tuft.transform.localScale = new Vector3(0.08f, h, 0.08f);
                tuft.transform.localPosition = new Vector3(p.x, h, p.y);
                tuft.transform.localRotation = Quaternion.Euler(URandom.Range(-8f, 8f), URandom.Range(0f, 360f), URandom.Range(-8f, 8f));
                tuft.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(tuft.GetComponent<Collider>());
                tuft.isStatic = true;
            }
        }

        private static void PlaceMushrooms(Transform parent, Material mat)
        {
            var group = new GameObject("Mushrooms");
            group.tag = GreyboxTag;
            group.transform.SetParent(parent, false);
            group.isStatic = true;
            for (int i = 0; i < MushroomCount; i++)
            {
                float angle = URandom.Range(0f, Mathf.PI * 2f);
                float r = URandom.Range(18f, 32f); // near tree ring
                Vector3 center = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                int capsInCluster = URandom.Range(2, 5);
                for (int c = 0; c < capsInCluster; c++)
                {
                    var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cap.name = $"Mushroom_{i:00}_{c}";
                    cap.tag = GreyboxTag;
                    cap.transform.SetParent(group.transform, false);
                    float capR = URandom.Range(0.12f, 0.2f);
                    float capH = URandom.Range(0.05f, 0.12f);
                    cap.transform.localScale = new Vector3(capR, capH, capR);
                    Vector2 offset = URandom.insideUnitCircle * 0.6f;
                    cap.transform.localPosition = center + new Vector3(offset.x, capH, offset.y);
                    cap.GetComponent<Renderer>().sharedMaterial = mat;
                    Object.DestroyImmediate(cap.GetComponent<Collider>());
                    cap.isStatic = true;
                }
            }
        }

        private static void PlaceFlowers(Transform parent, Material red, Material yellow, Material pink)
        {
            var group = new GameObject("Flowers");
            group.tag = GreyboxTag;
            group.transform.SetParent(parent, false);
            group.isStatic = true;
            Material[] mats = { red, yellow, pink };
            for (int i = 0; i < FlowerCount; i++)
            {
                Vector2 p = RandomDisk(InnerRadius, OuterRadius * 0.9f);
                var bud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bud.name = $"Flower_{i:000}";
                bud.tag = GreyboxTag;
                bud.transform.SetParent(group.transform, false);
                float s = URandom.Range(0.08f, 0.14f);
                bud.transform.localScale = new Vector3(s, s, s);
                bud.transform.localPosition = new Vector3(p.x, 0.2f, p.y);
                bud.GetComponent<Renderer>().sharedMaterial = mats[URandom.Range(0, mats.Length)];
                Object.DestroyImmediate(bud.GetComponent<Collider>());
                bud.isStatic = true;
            }
        }

        private static void TuneLighting()
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "Directional Light") continue;
                var light = root.GetComponent<Light>();
                if (light == null) continue;
                light.color = new Color(1.0f, 0.86f, 0.62f); // golden hour
                light.intensity = 1.35f;
                light.shadows = LightShadows.Soft;
                root.transform.rotation = Quaternion.Euler(28f, -35f, 0f);
                return;
            }
        }

        private static void TuneVolume()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                Debug.LogWarning("[StylizedPolish] VP_MeadowForest not found.");
                return;
            }

            EnsureBloom(profile);
            EnsureVignette(profile);
            EnsureColorAdjustments(profile);
            EditorUtility.SetDirty(profile);
        }

        private static void EnsureBloom(VolumeProfile p)
        {
            if (!p.TryGet(out UnityEngine.Rendering.Universal.Bloom bloom))
                bloom = p.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.active = true;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.6f;
            bloom.threshold.overrideState = true; bloom.threshold.value = 0.95f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.7f;
            bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.9f, 0.78f);
        }

        private static void EnsureVignette(VolumeProfile p)
        {
            if (!p.TryGet(out UnityEngine.Rendering.Universal.Vignette v))
                v = p.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            v.active = true;
            v.intensity.overrideState = true; v.intensity.value = 0.28f;
            v.smoothness.overrideState = true; v.smoothness.value = 0.5f;
            v.color.overrideState = true; v.color.value = new Color(0.1f, 0.08f, 0.05f);
        }

        private static void EnsureColorAdjustments(VolumeProfile p)
        {
            if (!p.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments c))
                c = p.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
            c.active = true;
            c.postExposure.overrideState = true; c.postExposure.value = 0.15f;
            c.contrast.overrideState = true; c.contrast.value = 12f;
            c.saturation.overrideState = true; c.saturation.value = 15f;
            c.colorFilter.overrideState = true; c.colorFilter.value = new Color(1.0f, 0.95f, 0.86f);
        }

        private static Vector2 RandomDisk(float inner, float outer)
        {
            float angle = URandom.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(URandom.Range(inner * inner, outer * outer));
            return new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        private static Material LoadOrUpdate(string path, Color color)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            if (mat == null)
            {
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

        private static void EnsureFolder(string assetDir)
        {
            string fs = Path.Combine(Directory.GetCurrentDirectory(), assetDir);
            if (!Directory.Exists(fs))
            {
                Directory.CreateDirectory(fs);
                AssetDatabase.Refresh();
            }
        }
    }
}
