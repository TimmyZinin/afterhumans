using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Linq;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Place processed 3D models into Unity scenes via batchmode.
    /// Supports static, animated, and npc modes.
    ///
    /// Usage:
    ///   Unity -batchmode -executeMethod Afterhumans.EditorTools.AssetPlacer.Place \
    ///     -customArgs "fbx=Assets/...;name=Chair;pos=1,0,2;mode=static"
    /// </summary>
    public static class AssetPlacer
    {
        private const string DefaultScene = "Assets/_Project/Scenes/Scene_Botanika.unity";

        [MenuItem("Afterhumans/Place Asset (Test)")]
        public static void Place()
        {
            var args = ParseCustomArgs();
            string fbxPath = args.Get("fbx", "");
            string objName = args.Get("name", "NewAsset");
            string mode = args.Get("mode", "static");
            Vector3 pos = ParseVector3(args.Get("pos", "0,0,0"));
            float rotY = float.Parse(args.Get("rot", "0"));
            float scale = float.Parse(args.Get("scale", "1"));
            string scenePath = args.Get("scene", DefaultScene);
            string knot = args.Get("knot", "");

            Debug.Log($"[AssetPlacer] mode={mode} name={objName} fbx={fbxPath} pos={pos} rot={rotY} scale={scale}");

            // Open scene
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Fix import settings for animated models
            if (mode == "animated" || mode == "npc")
                FixImportSettings(fbxPath);

            // Load FBX
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null)
            {
                Debug.LogError($"[AssetPlacer] FBX not found: {fbxPath}");
                return;
            }

            // Find or create container
            var container = GameObject.Find("GeneratedAssets");
            if (container == null)
                container = new GameObject("GeneratedAssets");

            // Instantiate model
            var root = new GameObject(objName);
            root.transform.SetParent(container.transform, worldPositionStays: false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0, rotY, 0);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.name = $"{objName}_Model";
            model.transform.SetParent(root.transform, worldPositionStays: false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0, 90, 0); // Tripo rotation fix
            model.transform.localScale = Vector3.one * scale;

            switch (mode)
            {
                case "static":
                    SetupStatic(root, model);
                    break;
                case "animated":
                    SetupAnimated(root, model, fbxPath);
                    break;
                case "npc":
                    SetupAnimated(root, model, fbxPath);
                    SetupNpc(root, knot);
                    break;
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[AssetPlacer] {mode} '{objName}' placed at {pos}. Scene saved.");
        }

        private static void FixImportSettings(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                changed = true;
            }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }
            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }
            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[AssetPlacer] Fixed import settings for {fbxPath}");
            }
        }

        private static void SetupStatic(GameObject root, GameObject model)
        {
            // Auto-fit BoxCollider
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers)
                    bounds.Encapsulate(r.bounds);

                var col = root.AddComponent<BoxCollider>();
                col.center = root.transform.InverseTransformPoint(bounds.center);
                col.size = bounds.size;
            }
            Debug.Log($"[AssetPlacer] Static setup done: BoxCollider added");
        }

        private static void SetupAnimated(GameObject root, GameObject model, string fbxPath)
        {
            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[AssetPlacer] No Animator on model — animation won't play");
                return;
            }

            // Create AnimatorController: Idle ↔ Walk
            var ctrlDir = System.IO.Path.GetDirectoryName(fbxPath);
            var ctrlName = root.name + "_Animator.controller";
            var ctrlPath = $"{ctrlDir}/{ctrlName}";

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ctrlPath) != null)
                AssetDatabase.DeleteAsset(ctrlPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);

            var sm = controller.layers[0].stateMachine;
            var idleState = sm.AddState("Idle");
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk");
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToArray();

            if (clips.Length > 0)
            {
                walkState.motion = clips[0];
                Debug.Log($"[AssetPlacer] Walk clip: '{clips[0].name}' ({clips.Length} total)");
            }

            var toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
            toWalk.hasExitTime = false;
            toWalk.duration = 0.15f;

            var toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;

            animator.runtimeAnimatorController = controller;
            AssetDatabase.SaveAssets();

            // Add follow behavior
            root.AddComponent<Afterhumans.Kafka.KafkaFollowSimple>();
            Debug.Log($"[AssetPlacer] Animated setup done: Animator + Follow");
        }

        private static void SetupNpc(GameObject root, string knotName)
        {
            // CapsuleCollider
            var renderers = root.GetComponentsInChildren<Renderer>();
            float height = 1.8f;
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                height = bounds.size.y;
            }
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, height / 2, 0);
            col.height = height;
            col.radius = 0.35f;

            // Remove BoxCollider if added by static
            var boxCol = root.GetComponent<BoxCollider>();
            if (boxCol != null) UnityEngine.Object.DestroyImmediate(boxCol);

            // Interactable
            if (!string.IsNullOrEmpty(knotName))
            {
                var interactable = root.AddComponent<Afterhumans.Dialogue.Interactable>();
                var so = new SerializedObject(interactable);
                var knotProp = so.FindProperty("knotName");
                if (knotProp != null)
                {
                    knotProp.stringValue = knotName;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // NPC idle bob
            var hasBob = root.GetComponent<Afterhumans.Art.NpcIdleBob>();
            if (hasBob == null)
                root.AddComponent<Afterhumans.Art.NpcIdleBob>();

            Debug.Log($"[AssetPlacer] NPC setup done: Collider + Interactable(knot={knotName}) + IdleBob");
        }

        // --- Helpers ---

        private static ArgMap ParseCustomArgs()
        {
            var args = new ArgMap();
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("-customArgs"))
                    continue;
                // Look for the arg after -customArgs
            }
            // Find -customArgs value
            var cmdArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < cmdArgs.Length; i++)
            {
                if (cmdArgs[i] == "-customArgs" && i + 1 < cmdArgs.Length)
                {
                    foreach (var pair in cmdArgs[i + 1].Split(';'))
                    {
                        var kv = pair.Split('=');
                        if (kv.Length == 2)
                            args[kv[0].Trim()] = kv[1].Trim();
                    }
                }
            }
            return args;
        }

        private static Vector3 ParseVector3(string s)
        {
            var parts = s.Split(',');
            return new Vector3(
                float.Parse(parts[0]),
                parts.Length > 1 ? float.Parse(parts[1]) : 0,
                parts.Length > 2 ? float.Parse(parts[2]) : 0
            );
        }

        private class ArgMap : System.Collections.Generic.Dictionary<string, string>
        {
            public string Get(string key, string defaultValue)
            {
                return TryGetValue(key, out var val) ? val : defaultValue;
            }
        }
    }
}
