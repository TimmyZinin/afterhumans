using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace Afterhumans.EditorTools
{
    public static class FixKirillMaterial
    {
        [MenuItem("Afterhumans/Fix Kirill Material")]
        public static void Fix()
        {
            var fbxPath = "Assets/_Project/Models/Generated/kirill.fbx";
            var fbmDir = "Assets/_Project/Models/Generated/kirill.fbm";

            // Load textures from .fbm/
            Texture2D colorTex = null, normalTex = null, ormTex = null;
            var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { fbmDir });
            foreach (var guid in texGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;
                var name = tex.name.ToLower();
                if (name.Contains("color")) colorTex = tex;
                else if (name.Contains("normal"))
                {
                    normalTex = tex;
                    // Fix normal map import
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp != null && imp.textureType != TextureImporterType.NormalMap)
                    {
                        imp.textureType = TextureImporterType.NormalMap;
                        imp.SaveAndReimport();
                    }
                }
                else if (name.Contains("orm")) ormTex = tex;
                Debug.Log($"[FixKirill] Texture: {tex.name} ({path})");
            }

            if (colorTex == null)
            {
                Debug.LogError("[FixKirill] No color texture found in " + fbmDir);
                return;
            }

            // Create URP/Lit material
            var matDir = "Assets/_Project/Models/Generated/Materials";
            if (!AssetDatabase.IsValidFolder(matDir))
                AssetDatabase.CreateFolder("Assets/_Project/Models/Generated", "Materials");

            var matPath = matDir + "/Kirill_Material.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("[FixKirill] URP/Lit shader not found!");
                return;
            }

            var mat = new Material(shader);
            mat.SetTexture("_BaseMap", colorTex);
            mat.SetTexture("_MainTex", colorTex);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (ormTex != null)
            {
                mat.SetTexture("_OcclusionMap", ormTex);
                mat.SetTexture("_MetallicGlossMap", ormTex);
                mat.EnableKeyword("_OCCLUSIONMAP");
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FixKirill] Material created: {matPath}");

            // Remap FBX material to our custom one
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                // Use external materials
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.materialLocation = ModelImporterMaterialLocation.External;

                var remap = new AssetImporter.SourceAssetIdentifier(
                    typeof(Material), "tripo_material_14f261ba-24c2-4c4f-a5ad-52473ec4cd76");
                importer.AddRemap(remap, mat);
                importer.SaveAndReimport();
                Debug.Log("[FixKirill] Material remapped on FBX importer");
            }

            // Also apply to any existing instances in scene
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r.gameObject.name.Contains("Kirill") ||
                        (r.gameObject.transform.parent != null &&
                         r.gameObject.transform.parent.name.Contains("Kirill")))
                    {
                        r.sharedMaterial = mat;
                        Debug.Log($"[FixKirill] Applied material to {r.gameObject.name}");
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[FixKirill] DONE — material fixed");
        }
    }
}
