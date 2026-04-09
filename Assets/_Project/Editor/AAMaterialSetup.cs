using UnityEditor;
using UnityEngine;
using System.IO;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// AA-quality PBR material setup for Botanika scene.
    /// Creates URP/Lit materials from downloaded textures (Poly Haven, ambientCG).
    /// </summary>
    public static class AAMaterialSetup
    {
        private const string MatDir = "Assets/_Project/Materials/AA";

        [MenuItem("Afterhumans/AA/Setup Materials")]
        public static void SetupAllMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            if (!AssetDatabase.IsValidFolder(MatDir))
                AssetDatabase.CreateFolder("Assets/_Project/Materials", "AA");

            CreateFloorMaterial();
            CreateWallMaterial();
            CreateCeilingMaterial();
            CreatePillarMaterial();
            CreateGlassMaterial();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AAMaterials] All materials created in " + MatDir);
        }

        private static void CreateFloorMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            mat.name = "Mat_Floor_WoodWorn";

            var basePath = "Assets/_Project/Vendor/PolyHaven/Textures/wood_floor_worn/";
            SetTexture(mat, "_BaseMap", basePath + "wood_floor_worn_diff_2k.png");
            SetNormalMap(mat, "_BumpMap", basePath + "wood_floor_worn_nor_2k.png");
            SetTexture(mat, "_OcclusionMap", basePath + "wood_floor_worn_arm_2k.png");

            // Roughness → invert for Smoothness in URP
            var roughTex = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "wood_floor_worn_rough_2k.png");
            if (roughTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", roughTex);
                mat.SetFloat("_Smoothness", 0.0f); // roughness map controls this
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_OCCLUSIONMAP");

            // Tiling for 12x10m floor
            mat.SetTextureScale("_BaseMap", new Vector2(6, 5));
            mat.SetTextureScale("_BumpMap", new Vector2(6, 5));
            mat.SetTextureScale("_OcclusionMap", new Vector2(6, 5));
            mat.SetTextureScale("_MetallicGlossMap", new Vector2(6, 5));

            AssetDatabase.CreateAsset(mat, MatDir + "/Mat_Floor_WoodWorn.mat");
            Debug.Log("[AAMaterials] Floor material created");
        }

        private static void CreateWallMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            mat.name = "Mat_Walls_Plaster";

            var basePath = "Assets/_Project/Vendor/ambientCG/PaintedPlaster017/";
            SetTexture(mat, "_BaseMap", basePath + "PaintedPlaster017_2K-PNG_Color.png");
            SetNormalMap(mat, "_BumpMap", basePath + "PaintedPlaster017_2K-PNG_NormalGL.png");

            // Look for roughness
            var roughPath = basePath + "PaintedPlaster017_2K-PNG_Roughness.png";
            if (File.Exists(Path.GetFullPath(roughPath)))
                SetTexture(mat, "_MetallicGlossMap", roughPath);

            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", 0.3f);

            // Tiling for tall walls
            mat.SetTextureScale("_BaseMap", new Vector2(2, 4));
            mat.SetTextureScale("_BumpMap", new Vector2(2, 4));

            AssetDatabase.CreateAsset(mat, MatDir + "/Mat_Walls_Plaster.mat");
            Debug.Log("[AAMaterials] Wall material created");
        }

        private static void CreateCeilingMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            mat.name = "Mat_Ceiling_White";

            var basePath = "Assets/_Project/Vendor/ambientCG/Plaster001/";
            SetTexture(mat, "_BaseMap", basePath + "Plaster001_2K-PNG_Color.png");
            SetNormalMap(mat, "_BumpMap", basePath + "Plaster001_2K-PNG_NormalGL.png");

            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", 0.2f);
            mat.SetColor("_BaseColor", new Color(0.95f, 0.93f, 0.90f)); // slight warm tint

            mat.SetTextureScale("_BaseMap", new Vector2(4, 3));
            mat.SetTextureScale("_BumpMap", new Vector2(4, 3));

            AssetDatabase.CreateAsset(mat, MatDir + "/Mat_Ceiling_White.mat");
            Debug.Log("[AAMaterials] Ceiling material created");
        }

        private static void CreatePillarMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            mat.name = "Mat_Pillars_Concrete";

            var basePath = "Assets/_Project/Vendor/ambientCG/Concrete012/";
            SetTexture(mat, "_BaseMap", basePath + "Concrete012_2K-PNG_Color.png");
            SetNormalMap(mat, "_BumpMap", basePath + "Concrete012_2K-PNG_NormalGL.png");

            var roughPath = basePath + "Concrete012_2K-PNG_Roughness.png";
            if (File.Exists(Path.GetFullPath(roughPath)))
            {
                SetTexture(mat, "_MetallicGlossMap", roughPath);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", 0.15f);

            mat.SetTextureScale("_BaseMap", new Vector2(1, 4));
            mat.SetTextureScale("_BumpMap", new Vector2(1, 4));

            AssetDatabase.CreateAsset(mat, MatDir + "/Mat_Pillars_Concrete.mat");
            Debug.Log("[AAMaterials] Pillar material created");
        }

        private static void CreateGlassMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            mat.name = "Mat_Glass_Window";

            // Transparent surface
            mat.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
            mat.SetFloat("_Blend", 0);   // Alpha blend
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

            // Glass properties
            mat.SetColor("_BaseColor", new Color(0.85f, 0.92f, 0.88f, 0.08f)); // very subtle tint
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetFloat("_Metallic", 0.05f);

            AssetDatabase.CreateAsset(mat, MatDir + "/Mat_Glass_Window.mat");
            Debug.Log("[AAMaterials] Glass material created");
        }

        // --- Helpers ---

        private static void SetTexture(Material mat, string prop, string path)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                mat.SetTexture(prop, tex);
                Debug.Log($"  Texture {prop}: {path}");
            }
            else
            {
                Debug.LogWarning($"  Texture NOT FOUND: {path}");
            }
        }

        private static void SetNormalMap(Material mat, string prop, string path)
        {
            // Ensure texture is imported as Normal Map
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                mat.SetTexture(prop, tex);
                mat.SetFloat("_BumpScale", 1.0f);
                Debug.Log($"  NormalMap {prop}: {path}");
            }
            else
            {
                Debug.LogWarning($"  NormalMap NOT FOUND: {path}");
            }
        }
    }
}
