using UnityEditor;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint 4 Day 2 / fix #4: AssetPostprocessor for GLB props imported
    /// into Vendor/Sketchfab/Botanika/, Vendor/Tripo/Botanika/, and
    /// Vendor/PolyHaven/Models/. Sets sane import defaults so 3d-artist
    /// drops a GLB and it just works in URP.
    ///
    /// Settings:
    ///   - meshCompression = Medium  (smaller .meta diff, fast import)
    ///   - isReadable = false        (runtime can't ReadPixels — fine)
    ///   - generateLightmapUVs = true (Sprint 4 BOT-A09 needs them)
    ///   - importNormals = Import     (use file-baked normals, not recalc)
    ///   - useFileScale = true        (GLB scale matches Blender export)
    ///   - importMaterials = true     (materials remap to URP/Lit below)
    ///
    /// OnPostprocessModel hook upgrades any imported Standard or no-pipeline
    /// shaders to URP/Lit using the same logic as UrpActivation.ConvertMaterialsToUrp.
    /// </summary>
    public class AfterhumansGLBImporter : AssetPostprocessor
    {
        private static readonly string[] BotanikaRoots =
        {
            "Assets/_Project/Vendor/Sketchfab/Botanika/",
            "Assets/_Project/Vendor/Tripo/Botanika/",
            "Assets/_Project/Vendor/PolyHaven/Models/",
        };

        private bool IsBotanikaGlb(string path)
        {
            if (!path.EndsWith(".glb") && !path.EndsWith(".gltf"))
            {
                return false;
            }
            foreach (var root in BotanikaRoots)
            {
                if (path.StartsWith(root)) return true;
            }
            return false;
        }

        void OnPreprocessModel()
        {
            if (!IsBotanikaGlb(assetPath)) return;

            // gltfast 6.x routes .glb through its own GltfImporter, not
            // ModelImporter. In that case assetImporter is GltfImporter and
            // a hard cast throws — bail gracefully. URP/Lit material remap
            // still happens via OnPostprocessModel below for non-gltfast paths.
            var mi = assetImporter as ModelImporter;
            if (mi == null) return;
            mi.meshCompression = ModelImporterMeshCompression.Medium;
            mi.isReadable = false;
            mi.generateSecondaryUV = true; // lightmap UVs
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.useFileScale = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.importAnimation = false; // static props
            mi.optimizeMeshPolygons = true;
            mi.optimizeMeshVertices = true;
            mi.weldVertices = true;
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!IsBotanikaGlb(assetPath)) return;

            // Walk all renderers → upgrade Standard to URP/Lit so the prop
            // doesn't appear magenta. Materials embedded in GLB sometimes
            // arrive as Standard (Specular setup) or no-pipeline shader.
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) return; // URP not active — nothing to do.

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || m.shader == null) continue;
                    string n = m.shader.name;
                    if (n.StartsWith("Universal Render Pipeline/")) continue;

                    // Capture base color/texture before swap.
                    Color baseColor = m.HasProperty("_Color")
                        ? m.GetColor("_Color")
                        : (m.HasProperty("_BaseColor")
                            ? m.GetColor("_BaseColor")
                            : Color.white);
                    Texture mainTex = m.HasProperty("_MainTex")
                        ? m.GetTexture("_MainTex")
                        : (m.HasProperty("_BaseMap")
                            ? m.GetTexture("_BaseMap")
                            : null);

                    m.shader = urpLit;
                    if (m.HasProperty("_BaseColor"))
                    {
                        m.SetColor("_BaseColor", baseColor);
                    }
                    if (m.HasProperty("_BaseMap") && mainTex != null)
                    {
                        m.SetTexture("_BaseMap", mainTex);
                    }
                    changed = true;
                }
                if (changed)
                {
                    r.sharedMaterials = mats;
                }
            }
        }
    }
}
