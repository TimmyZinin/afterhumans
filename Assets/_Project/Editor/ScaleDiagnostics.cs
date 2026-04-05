using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Diagnostic: print real-world bounds of key Kenney props в Scene_Botanika
    /// to verify BOT-F02 scale fix. Resolves mm-review CRITICAL finding.
    /// </summary>
    public static class ScaleDiagnostics
    {
        [MenuItem("Afterhumans/Debug/Verify Kenney Scale")]
        public static void VerifyKenneyScale()
        {
            Debug.Log("[ScaleDiagnostics] === Opening Scene_Botanika to measure real bounds ===");
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Scene_Botanika.unity", OpenSceneMode.Single);

            // Find specific Kenney instances by name
            string[] targets = {
                "loungeDesignSofa",
                "tableCoffeeGlassSquare",
                "lampRoundFloor",
                "bookcaseOpen",
                "wallWindow",
                "floorFull"
            };

            foreach (var t in targets)
            {
                var found = GameObject.Find(t);
                if (found == null)
                {
                    // Try to find any GO whose name CONTAINS target
                    var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                    foreach (var go in all)
                    {
                        if (go.name.StartsWith(t))
                        {
                            found = go;
                            break;
                        }
                    }
                }
                if (found == null)
                {
                    Debug.Log($"[ScaleDiagnostics] {t} → not found in scene");
                    continue;
                }

                var rend = found.GetComponentInChildren<Renderer>();
                if (rend == null)
                {
                    Debug.Log($"[ScaleDiagnostics] {t} → no renderer");
                    continue;
                }

                var b = rend.bounds;
                Debug.Log($"[ScaleDiagnostics] {t}: size={b.size.x:F2}×{b.size.y:F2}×{b.size.z:F2} m, world-pos={b.center.x:F2},{b.center.y:F2},{b.center.z:F2}");
            }

            // Also check Placeholder_NPC_Sasha cube which is 0.7×1.8×0.7 by design
            var sasha = GameObject.Find("Placeholder_NPC_Sasha");
            if (sasha != null)
            {
                var r = sasha.GetComponent<Renderer>();
                if (r != null)
                {
                    var b = r.bounds;
                    Debug.Log($"[ScaleDiagnostics] REFERENCE Placeholder_NPC_Sasha (should be ~0.7×1.8×0.7): actual={b.size.x:F2}×{b.size.y:F2}×{b.size.z:F2}");
                }
            }

            // Player CharacterController height should be 1.8
            var player = GameObject.Find("Player");
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    Debug.Log($"[ScaleDiagnostics] REFERENCE Player CharacterController height={cc.height:F2} radius={cc.radius:F2}");
                }
            }

            Debug.Log("[ScaleDiagnostics] === Done ===");
        }
    }
}
