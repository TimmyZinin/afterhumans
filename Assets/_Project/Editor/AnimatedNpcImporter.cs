using UnityEditor;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint D — import defaults for the new skeletal NPC FBX rigs dropped into
    /// Assets/_Project/Models/Animated/ (Blender/Tripo skeleton, decimated mesh, no baked
    /// clip). Two things matter here that the generic prop importer above gets wrong for a
    /// skinned character:
    ///
    ///  1. optimizeGameObjects MUST be false. Unity's "optimize game objects" collapses bone
    ///     Transforms that don't affect skinning weights into the animation clip's internal
    ///     curve table and removes them from the hierarchy — our procedural scripts
    ///     (NpcArmStir/NpcFidget, same recipe as CorgiStateAnimator) drive named bone
    ///     Transforms directly in LateUpdate and would find nothing.
    ///  2. animationType = Generic (Tripo/Blender rigs are not Mecanim-humanoid-mapped), and
    ///     importAnimation is left OFF by default here since our NPCs are driven procedurally,
    ///     not by baked clips — if a future NPC (Sasha/Mila/Nikolai) DOES ship a baked Action,
    ///     the build script gates on AnimationClip count itself (see BotanikaBuilder) rather
    ///     than relying on this importer to guess intent.
    /// </summary>
    public class AnimatedNpcImporter : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Models/Animated/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root)) return;
            var mi = assetImporter as ModelImporter;
            if (mi == null) return;

            mi.animationType = ModelImporterAnimationType.Generic;
            mi.optimizeGameObjects = false;   // keep every bone Transform in the hierarchy
            mi.importAnimation = true;        // pick up any baked Action; harmless if none exist
            mi.isReadable = false;
            mi.useFileScale = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
        }
    }
}
