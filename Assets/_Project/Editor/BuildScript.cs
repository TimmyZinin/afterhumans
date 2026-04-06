using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Build automation for Afterhumans Episode 0.
    /// Call via Unity CLI batchmode:
    ///   Unity -batchmode -nographics -quit -projectPath ~/afterhumans \
    ///     -buildTarget StandaloneOSX \
    ///     -executeMethod Afterhumans.EditorTools.BuildScript.BuildMacOS \
    ///     -logFile /dev/stdout
    /// </summary>
    public static class BuildScript
    {
        /// <summary>
        /// Get scenes from EditorBuildSettings (respects manual reorder via ProjectSetup.SetBotanikaFirstForTesting).
        /// Falls back to hardcoded order if no enabled scenes in settings.
        /// </summary>
        private static string[] GetScenes()
        {
            var enabled = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();

            if (enabled.Length > 0)
            {
                Debug.Log($"[BuildScript] Using {enabled.Length} scenes from EditorBuildSettings:");
                foreach (var s in enabled) Debug.Log($"  - {s}");
                return enabled;
            }

            Debug.LogWarning("[BuildScript] No enabled scenes in EditorBuildSettings, using hardcoded fallback");
            return new[]
            {
                "Assets/_Project/Scenes/Scene_Botanika.unity",
                "Assets/_Project/Scenes/Scene_MainMenu.unity",
                "Assets/_Project/Scenes/Scene_City.unity",
                "Assets/_Project/Scenes/Scene_Desert.unity",
                "Assets/_Project/Scenes/Scene_Credits.unity",
            };
        }

        [MenuItem("Afterhumans/Build macOS (Apple Silicon)")]
        public static void BuildMacOS()
        {
            string outputPath = GetBuildOutputPath();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.timzinin.afterhumans");
            PlayerSettings.productName = "Послелюди";
            PlayerSettings.companyName = "Tim Zinin";
            PlayerSettings.bundleVersion = "0.1.0";

            // Apple Silicon native
            PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 2); // 2 = ARM64 / Apple Silicon

            // QA Bug 4 fix: default to windowed mode so screen capture tools work.
            // Metal exclusive fullscreen blocks macOS compositor → screencapture
            // and Computer Use MCP return black/empty frames.
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;

            // Reduce build size, acceptable for macOS standalone
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Low);

            var options = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Build SUCCEEDED: {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime}");
                Debug.Log($"[BuildScript] Output: {outputPath}");
            }
            else
            {
                Debug.LogError($"[BuildScript] Build FAILED: {summary.result}");
                throw new Exception($"Build failed: {summary.result}");
            }
        }

        private static string GetBuildOutputPath()
        {
            string custom = Environment.GetEnvironmentVariable("BUILD_OUTPUT");
            if (!string.IsNullOrEmpty(custom)) return custom;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, "build", "Afterhumans.app");
        }
    }
}
