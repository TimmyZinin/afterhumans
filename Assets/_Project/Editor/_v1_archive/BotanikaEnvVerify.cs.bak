using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-A03 verification: opens Scene_Botanika and runs BotanikaEnvProps.Verify.
    /// Menu item + batch-mode entry point for BOT-T01 integration.
    /// </summary>
    public static class BotanikaEnvVerify
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_Botanika.unity";

        [MenuItem("Afterhumans/Art/Verify Botanika Env Props")]
        public static void Run()
        {
            // Open scene so GameObject.Find works
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[BotanikaEnvVerify] Failed to open scene at {ScenePath}");
                EditorApplication.Exit(2);
                return;
            }

            string reason;
            bool ok = BotanikaEnvProps.Verify(out reason);
            if (ok)
            {
                Debug.Log($"[BotanikaEnvVerify] PASS — {reason}");
            }
            else
            {
                Debug.LogError($"[BotanikaEnvVerify] FAIL — {reason}");
                EditorApplication.Exit(1);
            }
        }
    }
}
