using System.IO;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Quick patch: open Scene_MeadowForest_Greybox, find GameObject "Kafka",
    /// rotate its FBX child so the dog's nose faces +Z (transform.forward).
    /// Avoids re-running the full Bootstrap chain (which wipes the forest).
    ///
    /// Menu: Afterhumans → Meadow → Fix Kafka Rotation
    /// </summary>
    public static class KafkaRotationFix
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";

        [MenuItem("Afterhumans/Meadow/Fix Kafka Rotation")]
        public static void Fix()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[KafkaRotationFix] Scene missing: {ScenePath}");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject kafka = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Kafka")
                {
                    kafka = root;
                    break;
                }
            }
            if (kafka == null)
            {
                Debug.LogError("[KafkaRotationFix] 'Kafka' GameObject not found in scene.");
                return;
            }

            if (kafka.transform.childCount == 0)
            {
                Debug.LogError("[KafkaRotationFix] Kafka has no child (FBX missing?).");
                return;
            }

            var fbxChild = kafka.transform.GetChild(0);
            fbxChild.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Debug.Log($"[KafkaRotationFix] Set {fbxChild.name}.localRotation = (0, -90, 0).");

            // Also reposition Cinemachine FreeLook camera behind Kafka.
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "CM_FreeLook_Kafka") continue;
                var fl = root.GetComponent<Cinemachine.CinemachineFreeLook>();
                if (fl != null)
                {
                    fl.m_XAxis.Value = 180f;
                    Debug.Log("[KafkaRotationFix] CM_FreeLook_Kafka X axis set to 180 (behind dog).");
                }
                break;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[KafkaRotationFix] DONE.");
        }
    }
}
