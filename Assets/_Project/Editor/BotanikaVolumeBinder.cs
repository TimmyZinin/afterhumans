using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint 4 Day 2 / fix #1: Day 1 G1 root cause — Scene_Botanika has no
    /// Global Volume GameObject, so VP_Botanika.asset (tuned by PostFXTuner)
    /// is never bound at runtime. PostFX overrides exist on disk but no
    /// camera ever sees them.
    ///
    /// This binder ensures Scene_Botanika contains a single GameObject named
    /// "Global_Volume" with a UnityEngine.Rendering.Volume component, marked
    /// isGlobal=true, priority=0, weight=1, and sharedProfile pointing at
    /// VP_Botanika.asset.
    ///
    /// Idempotent: if the GO + component already exist, only re-binds the
    /// profile reference and saves.
    /// </summary>
    public static class BotanikaVolumeBinder
    {
        private const string BotanikaScenePath =
            "Assets/_Project/Scenes/Scene_Botanika.unity";
        private const string ProfilePath =
            "Assets/_Project/Settings/URP/VolumeProfiles/VP_Botanika.asset";
        private const string GoName = "Global_Volume";

        [MenuItem("Afterhumans/Sprint4/Bind Global Volume")]
        public static void ApplyMenu()
        {
            Apply();
        }

        public static void Apply()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                ProfilePath);
            if (profile == null)
            {
                Debug.LogError(
                    $"[BotanikaVolumeBinder] VP_Botanika not found at {ProfilePath}");
                return;
            }

            // Open Scene_Botanika as single active scene.
            var active = EditorSceneManager.GetActiveScene();
            if (!active.IsValid() || active.path != BotanikaScenePath)
            {
                if (active.IsValid() && active.isDirty)
                {
                    EditorSceneManager.SaveOpenScenes();
                }
                EditorSceneManager.OpenScene(
                    BotanikaScenePath, OpenSceneMode.Single);
            }

            var scene = EditorSceneManager.GetActiveScene();

            // Find or create Global_Volume GameObject.
            var go = GameObject.Find(GoName);
            if (go == null)
            {
                go = new GameObject(GoName);
                SceneManager.MoveGameObjectToScene(go, scene);
                Debug.Log(
                    $"[BotanikaVolumeBinder] Created GameObject '{GoName}'");
            }
            else
            {
                Debug.Log(
                    $"[BotanikaVolumeBinder] Reusing existing GameObject " +
                    $"'{GoName}'");
            }

            // Attach Volume component if missing.
            var vol = go.GetComponent<Volume>();
            if (vol == null)
            {
                vol = go.AddComponent<Volume>();
            }

            vol.isGlobal = true;
            vol.priority = 0f;
            vol.weight = 1f;
            vol.sharedProfile = profile;
            vol.enabled = true;

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log(
                $"[BotanikaVolumeBinder] Bound VP_Botanika → " +
                $"{GoName}.Volume (isGlobal=true, priority=0, weight=1). " +
                $"Profile: {profile.name}");
        }
    }
}
