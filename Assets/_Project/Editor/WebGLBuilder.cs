using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Afterhumans.Kafka;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Builds a WebGL player of Scene_Botanika with the Main Camera parked at the
    /// HERO angle, so opening the build in a real-GPU browser renders the scene with
    /// FULL lighting (GI/ambient/point lights/fog/bloom) that the headless soft-GL
    /// preview on Contabo cannot show. This is the true-look / AAA render path.
    /// Headless: -executeMethod Afterhumans.EditorTools.WebGLBuilder.BuildHero
    /// </summary>
    public static class WebGLBuilder
    {
        private const string Scene = "Assets/_Project/Scenes/Scene_Botanika.unity";
        private const string OutDir = "/root/afterhumans/Build/WebGL";
        private const string ProfilePath = "Assets/_Project/Settings/URP/VolumeProfiles/VP_Botanika_v2.asset";

        public static void BuildHero()
        {
            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            // === Pick the camera that will render, park it, disable every OTHER camera ===
            // (so no player/corgi camera renders on top WITHOUT post-processing and flattens
            //  the frame — a prime suspect for the flat GPU look).
            var allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Camera cam = Camera.main;
            if (cam == null && allCams.Length > 0) cam = allCams[0];
            foreach (var c in allCams)
                if (c != cam) { c.enabled = false; Debug.Log("[WebGLBuilder] disabled extra camera: " + c.name); }

            if (cam != null)
            {
                cam.enabled = true;
                cam.tag = "MainCamera";
                // HERO FRAMING = the reference's iconic shot (acceptance ×3 CRITICAL:
                // "high tilted off-axis 3/4 destroys the symmetry"). Eye-level, dead-
                // centered on the nave long axis, level horizon, straight one-point
                // perspective toward the far glazed gable (the golden key source) with
                // the central column as the spine. Slight downward to seat the floor.
                // Cycle L: was parked against the south wall (z=-12.8) staring down the
                // full 28 m nave → furniture tiny/distant, hall read EMPTY. Reference is an
                // INTIMATE shot: sofa fills the lower third, column center-spine, glazing
                // recedes behind. Pull the camera INTO the seating area (z≈-9) so the
                // sofa/table/corgi cluster fills the frame and the bare far floor is cropped.
                cam.transform.position = new Vector3(0f, 1.5f, -9.2f);
                cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.06f, 1f), Vector3.up);
                cam.fieldOfView = 56f;
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.allowHDR = true;

                // CRITICAL: enable URP post-processing on the RENDER camera. Without it the
                // scene's global Volume (ACES/bloom/grade/vignette) is ignored → flat ungraded
                // GPU render. Also force volumeLayerMask = Everything so the global Volume is
                // never excluded by a layer mismatch (the headless probe worked, build didn't).
                var addData = cam.GetUniversalAdditionalCameraData();
                if (addData != null)
                {
                    addData.renderPostProcessing = true;
                    addData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    addData.antialiasingQuality = AntialiasingQuality.High;
                    addData.renderShadows = true;
                    addData.volumeLayerMask = ~0;       // Everything
                    addData.volumeTrigger = cam.transform;
                }

                // Attach the global post-FX Volume + profile DIRECTLY to the render camera,
                // removing any ambiguity about which object carries the grade.
                var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(ProfilePath);
                var vol = cam.GetComponent<UnityEngine.Rendering.Volume>();
                if (vol == null) vol = cam.gameObject.AddComponent<UnityEngine.Rendering.Volume>();
                vol.isGlobal = true;
                vol.priority = 10f;
                if (profile != null) vol.profile = profile;
                cam.gameObject.layer = 0; // Default — inside Everything mask

                Debug.Log($"[WebGLBuilder] render cam='{cam.name}' postFX=ON SMAA=ON " +
                          $"volumeMask=Everything profile={(profile != null ? profile.name + "(" + profile.components.Count + " fx)" : "NULL")}");

                // Disable any player/camera controller so the view stays put on load.
                foreach (var mb in cam.GetComponentsInParent<MonoBehaviour>(true))
                {
                    if (mb == null) continue;
                    var n = mb.GetType().Name.ToLower();
                    if (n.Contains("controller") || n.Contains("player") || n.Contains("move") || n.Contains("look"))
                        mb.enabled = false;
                }
            }
            else
            {
                Debug.LogWarning("[WebGLBuilder] No camera found!");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Scene);

            // WebGL player settings — keep it lean for a fast-ish build.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = false;
            try { PlayerSettings.WebGL.threadsSupport = false; } catch {}

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = OutDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[WebGLBuilder] RESULT=" + report.summary.result +
                      " totalSize=" + report.summary.totalSize +
                      " out=" + OutDir);

            // RETINA PERF FIX: force the WebGL canvas to render at devicePixelRatio = 1.
            // On a retina/hi-DPI display the browser reports dPR=2 and Unity renders the
            // canvas at 2x linear resolution (= 4x the pixels = 4x fragment cost), which
            // is the single biggest GPU killer for a heavy URP scene in a browser.
            // No custom WebGLTemplate exists, so we patch the built index.html here.
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                ForceDevicePixelRatioOne(Path.Combine(OutDir, "index.html"));
        }

        /// <summary>
        /// Injects/uncomments `devicePixelRatio: 1,` inside the `var config = {...}` object
        /// of the built WebGL index.html (Unity 6 default template) so retina displays render
        /// 1x pixels instead of 2x (4x fragment work). Idempotent and tolerant of the various
        /// shapes Unity's template can emit (commented line, missing line, alt anchor).
        /// </summary>
        private static void ForceDevicePixelRatioOne(string indexPath)
        {
            try
            {
                if (!File.Exists(indexPath))
                {
                    Debug.LogWarning("[WebGLBuilder] dPR patch SKIPPED — index.html not found at " + indexPath);
                    return;
                }

                string html = File.ReadAllText(indexPath);

                // Already correctly set? (handles re-runs)
                if (Regex.IsMatch(html, @"devicePixelRatio\s*:\s*1\b"))
                {
                    Debug.Log("[WebGLBuilder] dPR patch: devicePixelRatio:1 already present — no change.");
                    return;
                }

                // Case 1: an existing devicePixelRatio entry with another value → set to 1.
                if (Regex.IsMatch(html, @"devicePixelRatio\s*:\s*[^,}\s]+"))
                {
                    html = Regex.Replace(html, @"devicePixelRatio\s*:\s*[^,}\s]+", "devicePixelRatio: 1");
                    File.WriteAllText(indexPath, html);
                    Debug.Log("[WebGLBuilder] dPR patch: rewrote existing devicePixelRatio → 1.");
                    return;
                }

                // Case 2: commented-out template line: "// devicePixelRatio: 1," → uncomment.
                if (Regex.IsMatch(html, @"//\s*devicePixelRatio\s*:\s*[^,\r\n]+,?"))
                {
                    html = Regex.Replace(html, @"//\s*devicePixelRatio\s*:\s*[^,\r\n]+,?", "devicePixelRatio: 1,");
                    File.WriteAllText(indexPath, html);
                    Debug.Log("[WebGLBuilder] dPR patch: uncommented template line → devicePixelRatio:1.");
                    return;
                }

                // Case 3: no entry — inject right after the config object opens.
                var m = Regex.Match(html, @"var\s+config\s*=\s*\{");
                if (m.Success)
                {
                    int insertAt = m.Index + m.Length;
                    html = html.Insert(insertAt, "\n      devicePixelRatio: 1,");
                    File.WriteAllText(indexPath, html);
                    Debug.Log("[WebGLBuilder] dPR patch: injected devicePixelRatio:1 into config object.");
                    return;
                }

                Debug.LogWarning("[WebGLBuilder] dPR patch FAILED — no 'var config = {' anchor or " +
                                 "devicePixelRatio key found in index.html. Retina will render 4x pixels!");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[WebGLBuilder] dPR patch threw: " + e.Message);
            }
        }

        // ===================== GAIT TEST HARNESS =====================
        private const string GaitScene = "Assets/_Project/Scenes/GaitTest.unity";
        private const string GaitOut   = "/root/afterhumans/Build/WebGLGait";

        /// <summary>
        /// Builds a minimal isolated scene to PROVE the corgi walk biomechanics: a gridded floor
        /// + the corgi auto-walking SLOWLY in a straight line + a FIXED camera at 90° to the
        /// motion (pure side profile). Lets the acceptance agent capture a clean single-stride
        /// sequence (the live follow-camera + slow screenshot rate can't). Output: Build/WebGLGait.
        /// Headless: -executeMethod Afterhumans.EditorTools.WebGLBuilder.BuildGaitTest
        /// </summary>
        public static void BuildGaitTest()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- gridded floor (checker → foot-contact + travel reference) ---
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "GridFloor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2f, 1f, 2f); // 20×20 m
            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var floorMat = new Material(litShader) { name = "GridMat" };
            var checker = new Texture2D(512, 512) { name = "Checker", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var a = new Color(0.62f, 0.62f, 0.64f); var b = new Color(0.40f, 0.40f, 0.43f);
            for (int y = 0; y < 512; y++)
                for (int x = 0; x < 512; x++)
                    checker.SetPixel(x, y, (((x >> 6) + (y >> 6)) & 1) == 0 ? a : b);
            checker.Apply();
            if (floorMat.HasProperty("_BaseMap")) { floorMat.SetTexture("_BaseMap", checker); floorMat.SetTextureScale("_BaseMap", new Vector2(40f, 40f)); }
            if (floorMat.HasProperty("_Smoothness")) floorMat.SetFloat("_Smoothness", 0.1f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // --- corgi root (CharacterController + AutoWalk) ---
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/kafka_corgi.fbx");
            var root = new GameObject("GaitCorgi");
            root.transform.position = new Vector3(0f, 0f, 3f);
            root.transform.rotation = Quaternion.identity;            // forward = +Z (walk dir)
            var cc = root.AddComponent<CharacterController>();
            cc.radius = 0.25f; cc.height = 0.6f; cc.center = new Vector3(0f, 0.3f, 0f); cc.stepOffset = 0.2f;
            var aw = root.AddComponent<AutoWalk>(); aw.speed = 0.28f; aw.startZ = 2f; aw.endZ = 8f;

            GameObject mesh = null;
            if (fbx != null)
            {
                mesh = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                mesh.name = "GaitCorgiMesh";
                mesh.transform.SetParent(root.transform, false);
                mesh.transform.localPosition = Vector3.zero;
                mesh.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // nose +X → +Z
                mesh.transform.localScale = Vector3.one * 2.2f;
                // texture the dog so it isn't white
                var cAlb = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Models/kafka_textures/cardiganwelshcorgi3dmodel_basecolor.png");
                if (cAlb != null)
                    foreach (var smr in mesh.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        var m = new Material(litShader) { name = "GaitCorgiMat" };
                        if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", cAlb); m.SetColor("_BaseColor", Color.white); }
                        smr.sharedMaterial = m;
                        smr.updateWhenOffscreen = true;
                    }
                var anim = mesh.GetComponent<Animator>() ?? mesh.AddComponent<Animator>();
                anim.runtimeAnimatorController = null; anim.applyRootMotion = false;
                var csa = mesh.AddComponent<CorgiStateAnimator>();
                csa.logGaitContacts = true;   // objective ground-contact log → browser console (gait-test diagnosis)
            }
            else Debug.LogWarning("[GaitTest] corgi FBX not found");

            // --- FIXED side camera (90° to +Z motion), pure profile ---
            var camGO = new GameObject("GaitCam");
            var cam = camGO.AddComponent<Camera>();
            cam.transform.position = new Vector3(5.2f, 0.75f, 5f);
            cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.42f, 5f) - cam.transform.position, Vector3.up);
            cam.fieldOfView = 30f; cam.tag = "MainCamera"; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.2f, 0.24f); cam.allowHDR = true;

            // --- lighting: raking key (legs separation + paw shadows) + fill ambient ---
            var sunGO = new GameObject("Sun");
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.5f; sun.color = new Color(1f, 0.96f, 0.9f);
            sun.shadows = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(42f, 35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.46f);

            EditorSceneManager.SaveScene(scene, GaitScene);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = false;
            try { PlayerSettings.WebGL.threadsSupport = false; } catch {}

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { GaitScene },
                locationPathName = GaitOut,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[GaitTest] RESULT=" + report.summary.result + " totalSize=" + report.summary.totalSize + " out=" + GaitOut);
        }
    }
}
