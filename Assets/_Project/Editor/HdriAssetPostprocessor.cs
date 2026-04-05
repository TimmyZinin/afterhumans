using UnityEditor;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-A02: AssetPostprocessor hook для Poly Haven HDRI файлов.
    ///
    /// Skill `3d-games` §2: HDRI import settings для Skybox/Panoramic shader:
    /// - textureShape = Texture2D (equirectangular, NOT Cube — Cube is only for Skybox/Cubemap shader)
    /// - sRGBTexture = false (HDR linear)
    /// - mipmapEnabled = true (skybox нужны mip levels)
    /// - wrapMode = Repeat (horizontal seamless wrap)
    /// - filterMode = Trilinear
    /// - textureCompression = Uncompressed (HDR precision preserved)
    ///
    /// self-adversarial fix: исходная версия форсила TextureCube, что вызывало
    /// runtime error *«Error assigning CUBE texture to 2D texture property '_MainTex'»*
    /// при привязке к Skybox/Panoramic. Panoramic ожидает 2D equirectangular; Cube
    /// layout существует только для Skybox/Cubemap и требует HDRI→Cubemap конвертацию.
    ///
    /// Применяется только к файлам в Vendor/PolyHaven/ чтобы не затрагивать
    /// baked lightmaps и прочие HDR usage.
    /// </summary>
    public class HdriAssetPostprocessor : AssetPostprocessor
    {
        private const string HdriRoot = "Assets/_Project/Vendor/PolyHaven/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(HdriRoot)) return;
            if (!assetPath.EndsWith(".hdr")) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = false;
            ti.mipmapEnabled = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Trilinear;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.maxTextureSize = 2048;
        }
    }
}
