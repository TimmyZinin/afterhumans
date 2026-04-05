using UnityEditor;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-F02: Auto-fixes Kenney FBX import settings on preprocess.
    ///
    /// Kenney FBX files ship с UnitScaleFactor=100 (exported from Blender в cm).
    /// Unity FBX importer с `useFileUnits=true` даёт непредсказуемый scale на разных
    /// платформах. Skill `3d-games` anti-pattern: *«inconsistent scale across assets»*.
    /// Skill `game-art`: *«3D: 1 unit = 1 meter (industry standard)»*.
    ///
    /// Fix: force globalScale=0.01 + useFileScale=false на всех FBX в
    /// Assets/_Project/Vendor/Kenney/. Также disable import of cameras/lights/animations
    /// (Kenney props не используют их) и disable auto-collider (BOT-F05 добавит
    /// BoxCollider helper-ом, не mesh).
    ///
    /// Чтобы применить к уже импортированным FBX — run <see cref="ReimportKenney"/>.
    /// </summary>
    public class KenneyAssetPostprocessor : AssetPostprocessor
    {
        private const string KenneyRoot = "Assets/_Project/Vendor/Kenney/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(KenneyRoot)) return;

            var mi = (ModelImporter)assetImporter;

            // BOT-F02 CRITICAL fix (empirically tuned via ScaleDiagnostics):
            // - globalScale=1 + useFileScale=true: sofa measured 11.2m (target 2m), 5.6×
            // - globalScale=0.01 + useFileScale=false: sofa 0.11m, 100× too small
            // - globalScale=0.2 + useFileScale=false: sofa ≈ 2.24m ✓ (target 2m)
            // Kenney FBX files have vertex data ~5× oversized from their stated cm
            // unit; compensate with 0.2× global scale + disable file scale to avoid
            // double application.
            mi.globalScale = 0.2f;
            mi.useFileScale = false;

            // Kenney props: no animations, no cameras, no lights
            mi.importCameras = false;
            mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.None;

            // BOT-F05 adds BoxColliders via helper; don't use FBX auto mesh collider
            mi.addCollider = false;

            // Generate lightmap UVs for baked GI (BOT-A09)
            mi.generateSecondaryUV = true;

            // Read/write off — saves memory on M1 8GB
            mi.isReadable = false;
        }

        /// <summary>
        /// Force reimport всех Kenney FBX чтобы применить новые settings.
        /// Run once after BOT-F02 first install, or when settings change.
        /// </summary>
        [MenuItem("Afterhumans/Setup/Reimport Kenney Assets")]
        public static void ReimportKenney()
        {
            Debug.Log("[KenneyAssetPostprocessor] Reimporting all Kenney FBX...");

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Vendor/Kenney" });
            int count = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".fbx") || path.EndsWith(".FBX"))
                    {
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                        count++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[KenneyAssetPostprocessor] Reimported {count} FBX files.");
        }
    }
}
