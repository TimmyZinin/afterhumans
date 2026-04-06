using System.IO;
using UnityEditor;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Generates procedural textures for scene materials.
    /// Replaces flat solid-color materials with textured ones
    /// that have visible grain, pattern, and depth.
    /// </summary>
    public static class ProceduralTextures
    {
        private const string TextureDir = "Assets/_Project/Textures/Procedural";
        private const int Size = 512; // increased from 256 for better quality

        private static void EnsureDir()
        {
            if (!Directory.Exists(TextureDir))
            {
                Directory.CreateDirectory(TextureDir);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Wood plank floor: warm brown with horizontal grain lines</summary>
        public static Texture2D WoodFloor()
        {
            var path = $"{TextureDir}/tex_wood_floor.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            EnsureDir();
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var baseColor = new Color(0.52f, 0.36f, 0.22f);
            var darkGrain = new Color(0.38f, 0.25f, 0.14f);
            var lightGrain = new Color(0.62f, 0.44f, 0.28f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = x / (float)Size;
                    float ny = y / (float)Size;

                    // Wood grain: stretched Perlin noise along X
                    float grain = Mathf.PerlinNoise(nx * 3f, ny * 20f);
                    float fineGrain = Mathf.PerlinNoise(nx * 8f, ny * 60f) * 0.3f;

                    // Plank lines every ~32px
                    float plankLine = Mathf.Abs(Mathf.Sin(ny * Size / 32f * Mathf.PI)) < 0.05f ? 0.7f : 1f;

                    float t = grain + fineGrain;
                    Color c = Color.Lerp(darkGrain, lightGrain, t) * plankLine;
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(tex, path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Plaster wall: warm beige with subtle roughness variation</summary>
        public static Texture2D PlasterWall()
        {
            var path = $"{TextureDir}/tex_plaster_wall.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            EnsureDir();
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var baseColor = new Color(0.78f, 0.67f, 0.52f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = x / (float)Size;
                    float ny = y / (float)Size;

                    // Large plaster variation
                    float noise1 = Mathf.PerlinNoise(nx * 6f, ny * 6f) * 0.12f;
                    // Fine grain
                    float noise2 = Mathf.PerlinNoise(nx * 25f, ny * 25f) * 0.06f;
                    // Tiny speckle
                    float noise3 = Mathf.PerlinNoise(nx * 80f, ny * 80f) * 0.03f;

                    float variation = noise1 + noise2 + noise3 - 0.1f;
                    Color c = baseColor + new Color(variation, variation * 0.9f, variation * 0.7f, 0f);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(tex, path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Tile floor: terracotta tiles with grout lines</summary>
        public static Texture2D TileFloor()
        {
            var path = $"{TextureDir}/tex_tile_floor.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            EnsureDir();
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var tileColor = new Color(0.60f, 0.42f, 0.28f);
            var groutColor = new Color(0.45f, 0.38f, 0.32f);

            int tileSize = 64; // 4x4 tiles per texture
            int groutWidth = 3;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int tx = x % tileSize;
                    int ty = y % tileSize;
                    bool isGrout = tx < groutWidth || ty < groutWidth;

                    float nx = x / (float)Size;
                    float ny = y / (float)Size;
                    float noise = Mathf.PerlinNoise(nx * 15f, ny * 15f) * 0.08f;

                    Color c = isGrout ? groutColor : tileColor + new Color(noise, noise * 0.8f, noise * 0.5f, 0f);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(tex, path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Fabric: warm upholstery with crosshatch weave</summary>
        public static Texture2D Fabric()
        {
            var path = $"{TextureDir}/tex_fabric.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            EnsureDir();
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var baseColor = new Color(0.50f, 0.32f, 0.22f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Crosshatch weave pattern
                    float weaveH = Mathf.Abs(Mathf.Sin(x * 0.8f)) * 0.08f;
                    float weaveV = Mathf.Abs(Mathf.Sin(y * 0.8f)) * 0.08f;
                    float weave = (weaveH + weaveV) * 0.5f;

                    float nx = x / (float)Size;
                    float ny = y / (float)Size;
                    float noise = Mathf.PerlinNoise(nx * 12f, ny * 12f) * 0.05f;

                    Color c = baseColor + new Color(weave + noise, (weave + noise) * 0.8f, (weave + noise) * 0.5f, 0f);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(tex, path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Wood grain for furniture: darker, more pronounced grain</summary>
        public static Texture2D WoodFurniture()
        {
            var path = $"{TextureDir}/tex_wood_furniture.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            EnsureDir();
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var baseColor = new Color(0.42f, 0.28f, 0.16f);
            var grainDark = new Color(0.30f, 0.18f, 0.10f);
            var grainLight = new Color(0.55f, 0.38f, 0.22f);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = x / (float)Size;
                    float ny = y / (float)Size;

                    // Strong wood grain
                    float grain = Mathf.PerlinNoise(nx * 2f, ny * 30f);
                    float ring = Mathf.Sin(grain * 12f) * 0.5f + 0.5f;
                    float detail = Mathf.PerlinNoise(nx * 10f, ny * 50f) * 0.2f;

                    Color c = Color.Lerp(grainDark, grainLight, ring + detail);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            SaveTexture(tex, path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Delete cached textures to force regeneration</summary>
        public static void ClearCache()
        {
            if (Directory.Exists(TextureDir))
            {
                foreach (var f in Directory.GetFiles(TextureDir, "tex_*.png"))
                    AssetDatabase.DeleteAsset(f.Replace(Application.dataPath.Replace("Assets", ""), ""));
                AssetDatabase.Refresh();
            }
        }

        /// <summary>Generate normal map from a height function using Sobel filter</summary>
        public static Texture2D GenerateNormalMap(string name, System.Func<float, float, float> heightFunc, float strength = 1.5f)
        {
            var path = $"{TextureDir}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            EnsureDir();
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);

            for (int y = 1; y < Size - 1; y++)
            {
                for (int x = 1; x < Size - 1; x++)
                {
                    float nx = x / (float)Size;
                    float ny = y / (float)Size;
                    float step = 1f / Size;

                    // Sobel filter for height → normal
                    float hL = heightFunc(nx - step, ny);
                    float hR = heightFunc(nx + step, ny);
                    float hD = heightFunc(nx, ny - step);
                    float hU = heightFunc(nx, ny + step);

                    Vector3 normal = new Vector3(
                        (hL - hR) * strength,
                        (hD - hU) * strength,
                        1f
                    ).normalized;

                    // Pack to 0-1 range
                    tex.SetPixel(x, y, new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z * 0.5f + 0.5f,
                        1f));
                }
            }
            tex.Apply();
            SaveTexture(tex, path, isNormal: true);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Tile floor normal map — grout lines create depth</summary>
        public static Texture2D TileFloorNormal()
        {
            return GenerateNormalMap("tex_tile_floor_normal", (x, y) =>
            {
                int tileSize = 64;
                int px = (int)(x * Size) % tileSize;
                int py = (int)(y * Size) % tileSize;
                bool isGrout = px < 4 || py < 4;
                return isGrout ? 0f : 0.5f + Mathf.PerlinNoise(x * 15, y * 15) * 0.2f;
            }, 2.0f);
        }

        /// <summary>Plaster wall normal map — rough surface variation</summary>
        public static Texture2D PlasterWallNormal()
        {
            return GenerateNormalMap("tex_plaster_wall_normal", (x, y) =>
            {
                return Mathf.PerlinNoise(x * 20, y * 20) * 0.5f
                     + Mathf.PerlinNoise(x * 60, y * 60) * 0.2f;
            }, 1.5f);
        }

        /// <summary>Wood normal map — grain direction</summary>
        public static Texture2D WoodNormal()
        {
            return GenerateNormalMap("tex_wood_normal", (x, y) =>
            {
                float grain = Mathf.PerlinNoise(x * 2, y * 30);
                return Mathf.Sin(grain * 12f) * 0.3f + 0.5f;
            }, 1.2f);
        }

        private static void SaveTexture(Texture2D tex, string assetPath, bool isNormal = false)
        {
            var bytes = tex.EncodeToPNG();
            var fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath);
            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(assetPath);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = Size;
                importer.SaveAndReimport();
            }
        }
    }
}
