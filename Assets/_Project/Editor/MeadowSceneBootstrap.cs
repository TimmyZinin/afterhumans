using System.IO;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Afterhumans.Kafka;
using Afterhumans.CameraRigs;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// One-shot bootstrap for Scene_MeadowForest_Greybox.unity (sandbox).
    /// Creates the scene, ground plane, directional light, Kafka prefab-as-player,
    /// Cinemachine FreeLook third-person camera, Volume (copy of VP_Botanika),
    /// saves. Does NOT register the scene in Build Settings.
    ///
    /// Menu: Afterhumans → Meadow → Bootstrap Sandbox Scene
    /// CLI:
    ///   Unity -batchmode -nographics -quit -projectPath ~/afterhumans \
    ///     -executeMethod Afterhumans.EditorTools.MeadowSceneBootstrap.Bootstrap \
    ///     -logFile /dev/stdout
    /// </summary>
    public static class MeadowSceneBootstrap
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";
        private const string GroundMaterialPath = "Assets/_Project/Materials/Nature/Mat_Meadow_Greybox.mat";
        private const string VolumeProfileSrc = "Assets/_Project/Settings/URP/VolumeProfiles/VP_Botanika.asset";
        private const string VolumeProfileDst = "Assets/_Project/Settings/URP/VolumeProfiles/VP_MeadowForest.asset";
        private const string KafkaFbxPath = "Assets/_Project/Models/kafka_corgi.fbx";
        private const string KafkaControllerPath = "Assets/_Project/Models/KafkaAnimator.controller";
        private const string GreyboxTag = "MeadowGreybox";

        [MenuItem("Afterhumans/Meadow/Bootstrap Sandbox Scene")]
        public static void Bootstrap()
        {
            Debug.Log("[MeadowBootstrap] Starting...");

            EnsureDirectory("Assets/_Project/Scenes");
            EnsureDirectory("Assets/_Project/Materials/Nature");
            EnsureDirectory("Assets/_Project/Settings/URP/VolumeProfiles");
            EnsureTag(GreyboxTag);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            RemoveDefaultMainCamera(scene);
            ConfigureDirectionalLight(scene);
            ConfigureRenderSettings();

            var groundMat = LoadOrCreateGroundMaterial();
            CreateGround(groundMat);

            var kafka = CreateKafka();

            CreateFreeLookCamera(kafka.transform);
            CreateMainCamera();

            CreateVolumeProfile();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[MeadowBootstrap] Failed to save scene");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MeadowBootstrap] DONE. Scene: {ScenePath}. NOT added to Build Settings (sandbox).");
            Debug.Log("[MeadowBootstrap] Next: run menu 'Afterhumans → Greybox → Build Meadow Forest' to populate trees.");
        }

        private static void RemoveDefaultMainCamera(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Main Camera")
                {
                    Object.DestroyImmediate(root);
                    return;
                }
            }
        }

        private static void ConfigureDirectionalLight(Scene scene)
        {
            GameObject dirLightGO = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Directional Light")
                {
                    dirLightGO = root;
                    break;
                }
            }
            if (dirLightGO == null)
            {
                dirLightGO = new GameObject("Directional Light");
                dirLightGO.AddComponent<Light>().type = LightType.Directional;
            }

            var light = dirLightGO.GetComponent<Light>();
            light.color = new Color(1.0f, 0.91f, 0.78f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            dirLightGO.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.85f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.84f, 0.82f, 0.69f);
            RenderSettings.ambientGroundColor = new Color(0.29f, 0.42f, 0.23f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.85f, 0.69f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 110f;
        }

        private static Material LoadOrCreateGroundMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (existing != null) return existing;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogWarning("[MeadowBootstrap] URP/Lit shader not found, falling back to Standard.");
                urpLit = Shader.Find("Standard");
            }
            var mat = new Material(urpLit) { name = "Mat_Meadow_Greybox" };
            var c = new Color(0.48f, 0.63f, 0.36f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(mat, GroundMaterialPath);
            return mat;
        }

        private static void CreateGround(Material mat)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Meadow_Ground";
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            ground.GetComponent<Renderer>().sharedMaterial = mat;
            ground.isStatic = true;
        }

        private static GameObject CreateKafka()
        {
            var root = new GameObject("Kafka");
            root.tag = "Player";
            root.transform.position = Vector3.zero;

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(KafkaFbxPath);
            if (fbx != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                inst.transform.SetParent(root.transform, worldPositionStays: false);
                inst.transform.localPosition = Vector3.zero;
                // kafka_corgi.fbx is oriented so the right side faces +Z by default.
                // Rotate -90° around Y so the nose faces +Z (= transform.forward in Unity).
                inst.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

                var animator = inst.GetComponent<Animator>() ?? inst.GetComponentInChildren<Animator>();
                if (animator == null) animator = inst.AddComponent<Animator>();

                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KafkaControllerPath);
                if (ctrl != null) animator.runtimeAnimatorController = ctrl;
                else Debug.LogWarning($"[MeadowBootstrap] KafkaAnimator.controller not found at {KafkaControllerPath}");
            }
            else
            {
                Debug.LogWarning($"[MeadowBootstrap] kafka_corgi.fbx not found at {KafkaFbxPath}, falling back to capsule placeholder.");
                var placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                placeholder.transform.SetParent(root.transform, false);
                placeholder.transform.localScale = new Vector3(0.4f, 0.3f, 0.6f);
                placeholder.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            }

            var cc = root.AddComponent<CharacterController>();
            cc.height = 0.6f;
            cc.radius = 0.25f;
            cc.center = new Vector3(0f, 0.3f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.2f;

            root.AddComponent<KafkaDirectController>();

            return root;
        }

        private static void CreateFreeLookCamera(Transform target)
        {
            var freeLookGO = new GameObject("CM_FreeLook_Kafka");
            var freeLook = freeLookGO.AddComponent<CinemachineFreeLook>();
            freeLook.Follow = target;
            freeLook.LookAt = target;

            // Tighter orbit so Kafka (0.6m tall) fills ~1/4 of the frame.
            freeLook.m_Orbits[0] = new CinemachineFreeLook.Orbit(2.0f, 1.6f);   // top
            freeLook.m_Orbits[1] = new CinemachineFreeLook.Orbit(1.0f, 2.2f);   // middle
            freeLook.m_Orbits[2] = new CinemachineFreeLook.Orbit(0.25f, 1.8f);  // bottom

            freeLook.m_XAxis.m_MaxSpeed = 300f;
            freeLook.m_XAxis.m_InputAxisName = "Mouse X";
            freeLook.m_XAxis.Value = 180f; // start camera behind Kafka
            freeLook.m_YAxis.m_MaxSpeed = 2f;
            freeLook.m_YAxis.m_InputAxisName = "Mouse Y";
            freeLook.m_YAxis.m_InvertInput = false;
        }

        private static void CreateMainCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<CinemachineBrain>();
            camGO.AddComponent<KafkaFollowCamera>().enabled = false;
        }

        private static void CreateVolumeProfile()
        {
            if (!File.Exists(VolumeProfileDst))
            {
                if (File.Exists(VolumeProfileSrc))
                {
                    if (!AssetDatabase.CopyAsset(VolumeProfileSrc, VolumeProfileDst))
                        Debug.LogWarning($"[MeadowBootstrap] Copy VP failed: {VolumeProfileSrc} → {VolumeProfileDst}");
                }
                else
                {
                    Debug.Log("[MeadowBootstrap] VP_Botanika template not found; skipping Volume profile copy.");
                }
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfileDst);
            if (profile == null) return;

            var volGO = new GameObject("Meadow_GlobalVolume");
            var vol = volGO.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 0f;
            vol.sharedProfile = profile;
        }

        private static void EnsureDirectory(string assetDir)
        {
            string fs = Path.Combine(Directory.GetCurrentDirectory(), assetDir);
            if (!Directory.Exists(fs))
            {
                Directory.CreateDirectory(fs);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureTag(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
