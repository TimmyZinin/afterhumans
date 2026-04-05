using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Afterhumans.Player;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-F09 polish: QA toggle for PlayerInteraction debug HUD.
    /// Flips the serialized `showDebugHud` flag across all PlayerInteraction
    /// instances in the currently open scene and saves the scene.
    /// </summary>
    public static class DebugMenuToggles
    {
        [MenuItem("Afterhumans/Debug/Toggle PlayerInteraction HUD")]
        public static void TogglePlayerInteractionHud()
        {
            var instances = Object.FindObjectsByType<PlayerInteraction>(FindObjectsSortMode.None);
            if (instances.Length == 0)
            {
                Debug.LogWarning("[DebugMenuToggles] No PlayerInteraction in open scene");
                return;
            }

            foreach (var pi in instances)
            {
                var so = new SerializedObject(pi);
                var prop = so.FindProperty("showDebugHud");
                if (prop == null) continue;
                prop.boolValue = !prop.boolValue;
                so.ApplyModifiedProperties();
                Debug.Log($"[DebugMenuToggles] PlayerInteraction.showDebugHud = {prop.boolValue} on {pi.gameObject.name}");
            }

            EditorSceneManager.MarkAllScenesDirty();
        }
    }
}
