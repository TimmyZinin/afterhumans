using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// Mac Apple Silicon standalone build containing ONLY the meadow sandbox scene.
    /// Output: build/MeadowSandbox.app — ad-hoc signed (Gatekeeper: right-click → Open).
    ///
    /// Menu: Afterhumans → Meadow → Build Mac Sandbox
    /// CLI:
    ///   Unity -batchmode -quit -projectPath ~/afterhumans \
    ///     -buildTarget StandaloneOSX \
    ///     -executeMethod Afterhumans.EditorTools.MeadowSandboxBuild.BuildMac \
    ///     -logFile /tmp/unity_sandbox_build.log
    /// </summary>
    public static class MeadowSandboxBuild
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_MeadowForest_Greybox.unity";
        private const string OutputDir = "build";
        private const string OutputApp = "build/MeadowSandbox.app";

        [MenuItem("Afterhumans/Meadow/Build Mac Sandbox")]
        public static void BuildMac()
        {
            Debug.Log("[MeadowBuild] Starting Mac Apple Silicon build...");

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[MeadowBuild] Scene missing: {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputApp,
                target = BuildTarget.StandaloneOSX,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneOSX;
            PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 2);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.productName = "Meadow Sandbox";
            PlayerSettings.bundleVersion = "0.1.0";

            BuildReport report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[MeadowBuild] SUCCESS: {OutputApp} ({summary.totalSize} bytes, {summary.totalTime})");
            }
            else
            {
                Debug.LogError($"[MeadowBuild] FAILED: {summary.result}, {summary.totalErrors} errors");
                EditorApplication.Exit(1);
            }
        }
    }
}
