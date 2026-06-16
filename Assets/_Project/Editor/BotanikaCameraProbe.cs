using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint 4 Day 2 / fix #2: Day 1 hero shot coordinates from
    /// BOTANIKA_HERO_SHOTS.md were not anchored to actual Sprint1_Greybox
    /// geometry. Shot 3 (2.5, 1.65, -1.8) shoots into the sky.
    ///
    /// This probe is a *diagnostic* tool. It refuses to trust hero docs
    /// and instead anchors all four cameras to whatever Player/spawn
    /// reference can be found in Scene_Botanika — exactly where the player
    /// will stand at game start. Then renders 4 cardinal-direction shots
    /// (forward/right/back/left) at eye level 1.65m, FOV 60.
    ///
    /// Goal: see what the scene actually looks like before composing
    /// hero shots.
    ///
    /// Output: /tmp/afterhumans_visual_review/02_probe_{N}_{direction}.png
    /// 1920×1080 PNGs.
    ///
    /// Player resolution order:
    ///   1. GameObject with tag "Player"
    ///   2. GameObject named "Player" / "PlayerStart"
    ///   3. GameObject with SimpleFirstPersonController-like component
    ///      (any with CharacterController)
    ///   4. Fallback: center of all Renderer bounds in scene + 1.65m height
    /// </summary>
    public static class BotanikaCameraProbe
    {
        private const string OutputDir = "/tmp/afterhumans_visual_review";
        private const int Width  = 1920;
        private const int Height = 1080;
        private const float EyeLevel = 1.65f;
        private const float Fov = 60f;

        private const string BotanikaScenePath =
            "Assets/_Project/Scenes/Scene_Botanika.unity";

        private static readonly (string label, Vector3 forward)[] Directions =
        {
            // Player spawns at (0,0,-3) facing into room. In-game forward
            // points at +Z (interior); -Z = door behind.
            ("forward", new Vector3(0f, 0f,  1f)),
            ("right",   new Vector3(1f, 0f,  0f)),
            ("back",    new Vector3(0f, 0f, -1f)),
            ("left",    new Vector3(-1f, 0f, 0f)),
        };

        [MenuItem("Afterhumans/Sprint4/Auto Camera Probe")]
        public static void CaptureFromPlayerSpawnMenu()
        {
            CaptureFromPlayerSpawn();
        }

        public static void CaptureFromPlayerSpawn()
        {
            Directory.CreateDirectory(OutputDir);

            // Open Scene_Botanika.
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

            var origin = ResolvePlayerOrigin();
            // Force eye level regardless of source.
            origin.y = EyeLevel;

            Debug.Log(
                $"[CameraProbe] Spawn origin resolved → {origin}");

            // The greybox has NO lighting yet (G-04 comes later). Without any
            // light/ambient/skybox, URP renders the grey geometry black-on-
            // black. For a SCALE diagnostic we force flat ambient + a temp
            // directional sun so the shells are actually visible.
            var prevAmbMode = RenderSettings.ambientMode;
            var prevAmbLight = RenderSettings.ambientLight;
            var prevAmbIntensity = RenderSettings.ambientIntensity;
            RenderSettings.ambientMode =
                UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(1.0f, 1.0f, 1.0f);
            RenderSettings.ambientIntensity = 1f;

            // The scene carries URP Volume(s) (VP_Botanika) with tonemapping/
            // exposure that crush brightness — disable them for the flat scale
            // diagnostic, restore after.
            var volumes = Object.FindObjectsByType<UnityEngine.Rendering.Volume>(
                FindObjectsSortMode.None);
            var volPrev = new System.Collections.Generic.Dictionary<
                UnityEngine.Rendering.Volume, bool>();
            foreach (var v in volumes)
            {
                volPrev[v] = v.enabled;
                v.enabled = false;
            }

            // The vault is now a CLOSED opaque shell → no sky light. In the
            // headless SubmitRenderRequest path URP IGNORES flat-ambient and
            // point lights — only DIRECTIONAL lights actually contribute. So we
            // light the greybox evenly with 3 shadowless directionals from
            // different angles (key from above-front, fill from opposite, top-
            // down to lift the floor).
            var lightGo = new GameObject("AH_TempProbeLights");
            lightGo.hideFlags = HideFlags.HideAndDontSave;

            void AddDir(Vector3 euler, float intensity)
            {
                var g = new GameObject("dir");
                g.transform.SetParent(lightGo.transform);
                g.transform.rotation = Quaternion.Euler(euler);
                var l = g.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = intensity;
                l.color = Color.white;
                l.shadows = LightShadows.None;
            }
            AddDir(new Vector3(50f, -30f, 0f), 1.5f);  // key
            AddDir(new Vector3(35f, 160f, 0f), 0.9f);  // fill (opposite)
            AddDir(new Vector3(90f, 0f, 0f), 0.9f);    // top-down lifts floor + ceiling

            try
            {
                for (int i = 0; i < Directions.Length; i++)
                {
                    var (label, fwd) = Directions[i];
                    string fileName = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "02_probe_{0}_{1}.png", i + 1, label);
                    string outPath = Path.Combine(OutputDir, fileName);
                    CaptureProbe(origin, fwd, outPath);
                    Debug.Log(
                        $"[CameraProbe] {i + 1}/{Directions.Length} {label} → " +
                        $"{outPath}");
                }
            }
            finally
            {
                Object.DestroyImmediate(lightGo);
                RenderSettings.ambientMode = prevAmbMode;
                RenderSettings.ambientLight = prevAmbLight;
                RenderSettings.ambientIntensity = prevAmbIntensity;
                foreach (var kv in volPrev)
                    if (kv.Key != null) kv.Key.enabled = kv.Value;
            }
        }

        private static Vector3 ResolvePlayerOrigin()
        {
            // 1. Tag "Player".
            try
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null)
                {
                    Debug.Log(
                        $"[CameraProbe] Found Player by tag → " +
                        $"{tagged.name} @ {tagged.transform.position}");
                    return tagged.transform.position;
                }
            }
            catch (UnityException)
            {
                // Tag may be undefined; ignore.
            }

            // 2. Name "Player" / "PlayerStart".
            string[] nameCandidates = { "Player", "PlayerStart" };
            foreach (var n in nameCandidates)
            {
                var byName = GameObject.Find(n);
                if (byName != null)
                {
                    Debug.Log(
                        $"[CameraProbe] Found by name '{n}' → " +
                        $"{byName.transform.position}");
                    return byName.transform.position;
                }
            }

            // 3. Any GameObject with CharacterController (FirstPersonController).
            var ccs = Object.FindObjectsByType<CharacterController>(
                FindObjectsSortMode.None);
            if (ccs != null && ccs.Length > 0)
            {
                Debug.Log(
                    $"[CameraProbe] Found CharacterController on " +
                    $"'{ccs[0].name}' → {ccs[0].transform.position}");
                return ccs[0].transform.position;
            }

            // 4. Fallback: center of all Renderer bounds.
            var renderers = Object.FindObjectsByType<Renderer>(
                FindObjectsSortMode.None);
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning(
                    "[CameraProbe] No renderers found; using origin (0, 1.65, 0)");
                return new Vector3(0f, EyeLevel, 0f);
            }

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            Debug.Log(
                $"[CameraProbe] Fallback bounds center: {bounds.center} " +
                $"(size {bounds.size})");
            return bounds.center;
        }

        private static void CaptureProbe(
            Vector3 origin, Vector3 forward, string outPath)
        {
            var go = new GameObject("AH_TempProbeCam");
            try
            {
                go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.position = origin;
                go.transform.rotation = Quaternion.LookRotation(
                    forward.normalized, Vector3.up);

                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = Fov;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 1000f;
                // SolidColor (not Skybox): the greybox has no skybox material,
                // so Skybox clear yields black. A sky-blue-grey reads against
                // the grey shells for the scale check.
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.45f, 0.55f, 0.68f, 1f);
                cam.allowHDR = true;
                // MSAA resolve on llvmpipe (software GL) is unreliable and
                // yields black readback → no MSAA for headless probe.
                cam.allowMSAA = false;

                var rt = new RenderTexture(Width, Height, 24,
                    RenderTextureFormat.ARGB32);
                rt.antiAliasing = 1;
                rt.Create();

                var prevActive = RenderTexture.active;
                cam.targetTexture = rt;

                // URP/Unity 6: cam.Render() does NOT execute the
                // ScriptableRenderPipeline pass in batchmode → black frame.
                // Must drive the SRP explicitly via SubmitRenderRequest.
                var srp = UnityEngine.Rendering
                    .GraphicsSettings.currentRenderPipeline;
                if (srp != null)
                {
                    var req = new UnityEngine.Rendering
                        .RenderPipeline.StandardRequest { destination = rt };
                    if (UnityEngine.Rendering.RenderPipeline
                            .SupportsRenderRequest(cam, req))
                    {
                        UnityEngine.Rendering.RenderPipeline
                            .SubmitRenderRequest(cam, req);
                    }
                    else
                    {
                        cam.Render();
                    }
                }
                else
                {
                    cam.Render();
                }

                GL.Flush();

                RenderTexture.active = rt;
                var tex = new Texture2D(
                    Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();

                // DIAG: log actual pixel colours so we know what was captured
                // (black / clear-colour / geometry) without guessing.
                var cCenter = tex.GetPixel(Width / 2, Height / 2);
                var cCorner = tex.GetPixel(20, 20);
                Debug.Log(
                    $"[CameraProbe] px center={cCenter} corner={cCorner} → " +
                    $"{Path.GetFileName(outPath)}");

                byte[] png = tex.EncodeToPNG();
                File.WriteAllBytes(outPath, png);

                RenderTexture.active = prevActive;
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // =====================================================================
        // LIT CAPTURE — renders the scene with its OWN lighting (sun + HDRI +
        // point lights + post-FX Volume). No diagnostic overrides. Use AFTER the
        // lighting pass (Sprint3_Lighting) to show the real look.
        // =====================================================================
        public static void CaptureLit()
        {
            Directory.CreateDirectory(OutputDir);
            EditorSceneManager.OpenScene(BotanikaScenePath, OpenSceneMode.Single);

            // 1. LIT 3/4 — the proven hero formula (elevated, gentle down-north so the
            // sunlit floor fills the frame), shifted west/back for a distinct angle on
            // the lounge + column + lattice. Reads warm & lit (Tim: must SEE the room).
            CaptureLitShot(new Vector3(-1.3f, 2.7f, -9.9f),
                Quaternion.LookRotation(new Vector3(0.07f, -0.12f, 1f), Vector3.up),
                "10_lit_forward.png");

            // 2. HERO 3/4 — raised but GENTLE downward (Tim-proxy hated the floor-
            // down view). Off-axis for depth; shows the whole lived-in cluster.
            CaptureLitShot(new Vector3(1.8f, 2.7f, -9.5f),
                Quaternion.LookRotation(new Vector3(-0.07f, -0.12f, 1f), Vector3.up),
                "11_lit_hero.png");

            // 3. LOUNGE close — eye level from the SE, north of the foreground ferns
            // so they don't block it; frames sofa + CRT desk + column + server glow.
            CaptureLitShot(new Vector3(3.4f, 1.5f, -4.3f),
                Quaternion.LookRotation(new Vector3(-0.32f, 0.03f, 1f), Vector3.up),
                "12_lit_mid.png");

            Debug.Log("[CameraProbe] LIT capture done (3 shots) → " + OutputDir);
        }

        // =====================================================================
        // NPC CLOSEUPS — bright FLAT ambient + front fill + NO post-FX so NPC
        // faces/materials are actually VISIBLE for review (the ACES + backlit
        // HDRI otherwise crush NPCs to black silhouettes). Camera sits on the
        // room-centre side of each NPC so the interior — not the blown-out
        // glass walls — sits behind them. Non-destructive (render only).
        // =====================================================================
        public static void CaptureNpcCloseups()
        {
            Directory.CreateDirectory(OutputDir);
            EditorSceneManager.OpenScene(BotanikaScenePath, OpenSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.92f, 0.90f, 0.84f);
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.fog = false;

            var fill = new GameObject("AH_ReviewFill");
            fill.hideFlags = HideFlags.HideAndDontSave;
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional; fl.intensity = 1.2f; fl.shadows = LightShadows.None;
            fl.color = new Color(1f, 0.96f, 0.9f);
            fill.transform.rotation = Quaternion.Euler(55f, 20f, 0f);

            // second fill from the opposite side so NPCs facing either way are lit
            var fill2 = new GameObject("AH_ReviewFill2");
            fill2.hideFlags = HideFlags.HideAndDontSave;
            var fl2 = fill2.AddComponent<Light>();
            fl2.type = LightType.Directional; fl2.intensity = 0.9f; fl2.shadows = LightShadows.None;
            fl2.color = new Color(0.95f, 0.95f, 1f);
            fill2.transform.rotation = Quaternion.Euler(50f, 210f, 0f);

            string[] ids = { "nikolai", "mila", "kirill", "stas", "sasha" };
            var center = new Vector3(0f, 1.2f, 0f);
            foreach (var id in ids)
            {
                var npc = GameObject.Find("NPC_" + id);
                if (npc == null) { Debug.LogWarning("[NpcCloseup] NPC_" + id + " NOT FOUND"); continue; }
                var rends = npc.GetComponentsInChildren<Renderer>(true);
                var b = rends.Length > 0 ? rends[0].bounds : new Bounds(npc.transform.position, Vector3.one * 1.6f);
                foreach (var r in rends) b.Encapsulate(r.bounds);
                var aim = new Vector3(b.center.x, b.max.y - 0.18f, b.center.z); // near head
                // camera in FRONT of the NPC (along its facing) so we see the face and
                // the column/interior sits behind it, never occluding.
                var fwd = npc.transform.forward; fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.back;
                fwd.Normalize();
                var camPos = new Vector3(npc.transform.position.x, aim.y + 0.05f, npc.transform.position.z) + fwd * 2.4f;
                var rot = Quaternion.LookRotation((aim - camPos).normalized, Vector3.up);
                Debug.Log($"[NpcCloseup] {id} cam={camPos} aim={aim} headY={b.max.y:0.00} baseY={b.min.y:0.00}");
                CaptureLitShot(camPos, rot, "np_" + id + ".png", false);
            }

            // bright wide review (no post-FX) for overall composition
            CaptureLitShot(new Vector3(0f, 2.4f, -9.2f),
                Quaternion.LookRotation(new Vector3(0f, -0.10f, 1f), Vector3.up),
                "np_wide.png", false);

            Object.DestroyImmediate(fill);
            Object.DestroyImmediate(fill2);
            Debug.Log("[CameraProbe] NPC closeups done → " + OutputDir);
        }

        private static void CaptureLitShot(Vector3 pos, Quaternion rot, string fileName, bool postFx = true)
        {
            var go = new GameObject("AH_TempLitCam");
            try
            {
                go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.position = pos;
                go.transform.rotation = rot;

                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 1000f;
                // Use the scene skybox (HDRI) — real look, no flat fill.
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.allowHDR = true;
                cam.allowMSAA = false;

                // Apply the scene's global post-FX Volume to THIS camera so the
                // hero shot gets ACES/Bloom/etc (URP additional-cam render).
                var addData = cam.GetUniversalAdditionalCameraData();
                if (addData != null) addData.renderPostProcessing = postFx;

                var rt = new RenderTexture(Width, Height, 24,
                    RenderTextureFormat.ARGB32);
                rt.antiAliasing = 1;
                rt.Create();

                var prevActive = RenderTexture.active;
                cam.targetTexture = rt;

                var srp = UnityEngine.Rendering
                    .GraphicsSettings.currentRenderPipeline;
                if (srp != null)
                {
                    var req = new UnityEngine.Rendering
                        .RenderPipeline.StandardRequest { destination = rt };
                    if (UnityEngine.Rendering.RenderPipeline
                            .SupportsRenderRequest(cam, req))
                        UnityEngine.Rendering.RenderPipeline
                            .SubmitRenderRequest(cam, req);
                    else
                        cam.Render();
                }
                else
                {
                    cam.Render();
                }

                GL.Flush();
                RenderTexture.active = rt;
                var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();
                var c = tex.GetPixel(Width / 2, Height / 2);
                Debug.Log($"[CameraProbe] LIT px center={c} → {fileName}");

                File.WriteAllBytes(Path.Combine(OutputDir, fileName),
                    tex.EncodeToPNG());

                RenderTexture.active = prevActive;
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
