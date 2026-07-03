using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Sprint 4 / BOT visual review harness. Renders 4 cinematic hero shots
    /// of Scene_Botanika to /tmp/afterhumans_visual_review/01_shot_{N}_{name}.png
    /// at 1920×1080 from a temporary off-scene Camera.
    ///
    /// Source of truth for shot coordinates:
    ///   1. Try ~/afterhumans/docs/BOTANIKA_HERO_SHOTS.md (game-designer doc).
    ///   2. Fallback to 4 hard-coded defaults baked from the room layout.
    ///
    /// Parser format expected in BOTANIKA_HERO_SHOTS.md:
    ///   ## Shot N — name
    ///   - Camera: (x, y, z)
    ///   - Look-at: (x, y, z)
    ///   - FOV: 55
    /// </summary>
    public static class BotanikaScreenshotHarness
    {
        private const string OutputDir = "/tmp/afterhumans_visual_review";
        private const int Width  = 1920;
        private const int Height = 1080;

        private const string BotanikaScenePath =
            "Assets/_Project/Scenes/Scene_Botanika.unity";

        private static readonly string HeroShotsDoc =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                "afterhumans/docs/BOTANIKA_HERO_SHOTS.md");

        private struct Shot
        {
            public string name;
            public Vector3 position;
            public Vector3 lookAt;
            public float fov;
        }

        private static readonly Shot[] Defaults = new[]
        {
            new Shot {
                name = "shot1_wakeup",
                position = new Vector3(2f, 0.95f, 1.5f),
                lookAt   = new Vector3(4f, 1.2f, 3f),
                fov = 55f,
            },
            new Shot {
                name = "shot2_doorframe",
                position = new Vector3(5f, 1.65f, 4f),
                lookAt   = new Vector3(9f, 1.5f, 6f),
                fov = 60f,
            },
            new Shot {
                name = "shot3_serverrack",
                position = new Vector3(8f, 1.4f, 7f),
                lookAt   = new Vector3(10f, 1.2f, 8.5f),
                fov = 50f,
            },
            new Shot {
                name = "shot4_window",
                position = new Vector3(3f, 1.65f, 2f),
                lookAt   = new Vector3(1f, 1.5f, 0.5f),
                fov = 65f,
            },
        };

        [MenuItem("Afterhumans/Sprint4/Capture Cinematic Shots")]
        public static void CaptureCinematicShotsMenu()
        {
            CaptureCinematicShots();
        }

        public static void CaptureCinematicShots()
        {
            Directory.CreateDirectory(OutputDir);

            // Open Scene_Botanika if not active.
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

            var shots = LoadShots();

            for (int i = 0; i < shots.Count; i++)
            {
                var shot = shots[i];
                string fileName = string.Format(
                    CultureInfo.InvariantCulture,
                    "01_shot_{0}_{1}.png", i + 1, shot.name);
                string outPath = Path.Combine(OutputDir, fileName);
                CaptureShot(shot, outPath);
                Debug.Log(
                    $"[ScreenshotHarness] Shot {i + 1} ({shot.name}) → " +
                    $"{outPath}");
            }

            Debug.Log(
                $"[ScreenshotHarness] Captured {shots.Count} shots to " +
                $"{OutputDir}");
        }

        private static List<Shot> LoadShots()
        {
            if (File.Exists(HeroShotsDoc))
            {
                try
                {
                    var parsed = ParseHeroShotsDoc(HeroShotsDoc);
                    if (parsed.Count >= 4)
                    {
                        Debug.Log(
                            $"[ScreenshotHarness] Loaded {parsed.Count} " +
                            $"shots from {HeroShotsDoc}");
                        return parsed;
                    }
                    Debug.LogWarning(
                        $"[ScreenshotHarness] {HeroShotsDoc} parsed only " +
                        $"{parsed.Count} shots (need 4) — falling back to " +
                        $"defaults.");
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[ScreenshotHarness] Failed to parse " +
                        $"{HeroShotsDoc}: {e.Message} — using defaults.");
                }
            }
            return new List<Shot>(Defaults);
        }

        private static List<Shot> ParseHeroShotsDoc(string path)
        {
            var lines = File.ReadAllLines(path);
            var result = new List<Shot>();

            // Match: ## Shot N — name   (em-dash or hyphen)
            var headerRx = new Regex(
                @"^##\s+Shot\s+(\d+)\s*[—\-:]\s*(.+?)\s*$",
                RegexOptions.IgnoreCase);
            // Match: - Camera: (x, y, z)  (allows position/cam/camera)
            var camRx = new Regex(
                @"^[\s\-\*]*(?:Camera|Position)\s*[:=]\s*" +
                @"\(?\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*,\s*([-\d\.]+)",
                RegexOptions.IgnoreCase);
            var lookRx = new Regex(
                @"^[\s\-\*]*Look\-?at\s*[:=]\s*" +
                @"\(?\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*,\s*([-\d\.]+)",
                RegexOptions.IgnoreCase);
            var fovRx = new Regex(
                @"^[\s\-\*]*FOV\s*[:=]\s*([-\d\.]+)",
                RegexOptions.IgnoreCase);

            Shot? cur = null;
            foreach (var raw in lines)
            {
                var line = raw ?? string.Empty;

                var hm = headerRx.Match(line);
                if (hm.Success)
                {
                    if (cur.HasValue && IsShotComplete(cur.Value))
                    {
                        result.Add(cur.Value);
                    }
                    var name = hm.Groups[2].Value
                        .Trim()
                        .ToLowerInvariant()
                        .Replace(' ', '_')
                        .Replace('/', '_');
                    cur = new Shot { name = $"shot{hm.Groups[1].Value}_{name}" };
                    continue;
                }

                if (!cur.HasValue) continue;
                var s = cur.Value;

                var cm = camRx.Match(line);
                if (cm.Success)
                {
                    s.position = ParseVec3(cm);
                    cur = s;
                    continue;
                }
                var lm = lookRx.Match(line);
                if (lm.Success)
                {
                    s.lookAt = ParseVec3(lm);
                    cur = s;
                    continue;
                }
                var fm = fovRx.Match(line);
                if (fm.Success)
                {
                    s.fov = ParseFloat(fm.Groups[1].Value);
                    cur = s;
                    continue;
                }
            }
            if (cur.HasValue && IsShotComplete(cur.Value))
            {
                result.Add(cur.Value);
            }

            return result;
        }

        private static bool IsShotComplete(Shot s)
        {
            // FOV 0 indicates we never saw a FOV line; treat as incomplete.
            return s.fov > 0.01f;
        }

        private static Vector3 ParseVec3(Match m)
        {
            return new Vector3(
                ParseFloat(m.Groups[1].Value),
                ParseFloat(m.Groups[2].Value),
                ParseFloat(m.Groups[3].Value));
        }

        private static float ParseFloat(string s)
        {
            return float.Parse(s, CultureInfo.InvariantCulture);
        }

        private static void CaptureShot(Shot shot, string outPath)
        {
            // Spawn a temporary camera. Don't reuse Main Camera — we don't
            // want to mutate the player rig.
            var go = new GameObject("AH_TempCaptureCam");
            try
            {
                go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.position = shot.position;
                go.transform.LookAt(shot.lookAt, Vector3.up);

                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = shot.fov;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 1000f;
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.backgroundColor = new Color(0.95f, 0.80f, 0.58f, 1f);
                cam.allowHDR = true;
                cam.allowMSAA = true;

                var rt = new RenderTexture(Width, Height, 24,
                    RenderTextureFormat.ARGB32);
                rt.antiAliasing = 2;

                var prevActive = RenderTexture.active;
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(
                    Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();

                byte[] png = tex.EncodeToPNG();
                File.WriteAllBytes(outPath, png);

                RenderTexture.active = prevActive;
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
