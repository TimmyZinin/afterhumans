using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Creates URP/Lit materials for Quaternius's Stylized Nature MegaKit (Standard edition)
    /// from its 20 PNG textures. The pack ships no materials/shaders, so we author our own.
    ///
    /// Leaf materials use Alpha Clipping (threshold 0.2) matching glTF alphaMode MASK / cutoff 0.2.
    /// Stylized look: Metallic 0, Smoothness 0.1 (matte). Not PBR.
    ///
    /// Expects MegaKit at Assets/ThirdParty/StylizedNature/Textures/*.png.
    ///
    /// Menu: Afterhumans → Meadow → Build Stylized Nature Materials
    /// </summary>
    public static class StylizedNatureMaterialBuilder
    {
        private const string TexDir = "Assets/ThirdParty/StylizedNature/Textures";
        private const string MatDir = "Assets/_Project/Materials/Nature/Stylized";

        // (material name, albedo texture, normal texture or null, is leaf foliage with alpha)
        private static readonly (string mat, string albedo, string normal, bool alpha)[] MaterialSpecs = new[]
        {
            ("Mat_Bark_NormalTree",    "Bark_NormalTree.png",    "Bark_NormalTree_Normal.png",   false),
            ("Mat_Bark_DeadTree",      "Bark_DeadTree.png",      "Bark_DeadTree_Normal.png",     false),
            ("Mat_Bark_TwistedTree",   "Bark_TwistedTree.png",   "Bark_TwistedTree_Normal.png",  false),
            ("Mat_Leaves_NormalTree",  "Leaves_NormalTree.png",  "Leaves_NormalTree_C.png",      true),
            ("Mat_Leaves_TwistedTree", "Leaves_TwistedTree.png", "Leaves_TwistedTree_C.png",     true),
            ("Mat_Leaves_Pine",        "Leaves_Pine.png",        null,                           true),
            ("Mat_Leaves_GiantPine",   "Leaves_GiantPine.png",   null,                           true),
            ("Mat_Leaves_Generic",     "Leaves.png",             null,                           true),
            ("Mat_Grass",              "Grass.png",              null,                           true),
            ("Mat_Flowers",            "Flowers.png",            null,                           true),
            ("Mat_Mushroom",           "Mushroom.png",           null,                           false),
            ("Mat_Rocks_Medium",       "Rocks_Diffuse.png",      null,                           false),
            ("Mat_PathRocks",          "PathRocks_Diffuse.png",  null,                           false),
            ("Mat_Rocks_Desert",       "Rocks_Desert_Diffuse.png", null,                         false),
            ("Mat_PathRocks_Desert",   "PathRocks_Desert_Diffuse.png", null,                     false),
        };

        [MenuItem("Afterhumans/Meadow/Build Stylized Nature Materials")]
        public static void BuildAll()
        {
            Debug.Log("[StylizedNature] Building URP/Lit materials...");

            if (!Directory.Exists(TexDir))
            {
                Debug.LogError($"[StylizedNature] Texture folder not found: {TexDir}. Unpack MegaKit zip into Assets/ThirdParty/StylizedNature/ first.");
                return;
            }
            EnsureFolder(MatDir);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("[StylizedNature] URP/Lit shader not found. Is URP installed and active?");
                return;
            }

            int created = 0, skipped = 0;
            foreach (var spec in MaterialSpecs)
            {
                string matPath = $"{MatDir}/{spec.mat}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                bool isNew = mat == null;
                if (isNew)
                {
                    mat = new Material(urpLit) { name = spec.mat };
                    AssetDatabase.CreateAsset(mat, matPath);
                    created++;
                }
                else skipped++;

                var albedo = LoadTexture(spec.albedo);
                if (albedo != null)
                    mat.SetTexture("_BaseMap", albedo);
                else
                    Debug.LogWarning($"[StylizedNature] Missing texture: {spec.albedo} for {spec.mat}");

                if (spec.normal != null)
                {
                    EnsureImporterIsNormalMap(spec.normal);
                    var nrm = LoadTexture(spec.normal);
                    if (nrm != null)
                    {
                        mat.SetTexture("_BumpMap", nrm);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                }

                if (spec.alpha)
                {
                    mat.SetFloat("_AlphaClip", 1f);
                    mat.SetFloat("_Cutoff", 0.2f);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.SetOverrideTag("RenderType", "TransparentCutout");
                    mat.renderQueue = 2450;

                    mat.doubleSidedGI = true;
                    mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                    mat.SetFloat("_CullMode", (float)UnityEngine.Rendering.CullMode.Off);
                }

                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Smoothness", 0.1f);
                mat.color = Color.white;

                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StylizedNature] Done. Created {created}, updated {skipped} materials under {MatDir}.");
        }

        private static Texture2D LoadTexture(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return null;
            string path = $"{TexDir}/{filename}";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureImporterIsNormalMap(string filename)
        {
            string path = $"{TexDir}/{filename}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
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
