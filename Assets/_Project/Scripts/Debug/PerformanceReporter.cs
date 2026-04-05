using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afterhumans.DebugTools
{
    /// <summary>
    /// BOT-F07: Runtime performance baseline reporter.
    ///
    /// Skill `game-development` §4: performance budget 60 FPS = 16.67ms per frame.
    /// Target на M1 8GB: >=40 FPS average, < 500 draw calls, < 1.5 GB RAM.
    ///
    /// Attached to Player в Debug Build only. Every 60 frames samples:
    /// - 1f / Time.deltaTime (instant FPS)
    /// - System.GC.GetTotalMemory(false) (managed heap)
    /// - UnityStats.drawCalls via reflection (editor-only, not built players)
    /// Appends к `Application.persistentDataPath/PerformanceBaseline.txt` (readable
    /// from /Users/timofeyzinin/Library/Application Support/Tim Zinin/Послелюди/).
    /// </summary>
    public class PerformanceReporter : MonoBehaviour
    {
        [SerializeField] private int sampleIntervalFrames = 60;
        [SerializeField] private float durationSeconds = 60f;
        [SerializeField] private string fileName = "PerformanceBaseline.txt";

        private int _frameCounter;
        private float _startTime;
        private int _samplesWritten;
        private float _fpsSum;
        private int _fpsCount;
        private string _outputPath;

        private void Start()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _startTime = Time.realtimeSinceStartup;
            _outputPath = Path.Combine(Application.persistentDataPath, fileName);

            using (var sw = new StreamWriter(_outputPath, append: false))
            {
                sw.WriteLine($"# Afterhumans PerformanceBaseline");
                sw.WriteLine($"# Start: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sw.WriteLine($"# Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                sw.WriteLine($"# Unity: {Application.unityVersion}");
                sw.WriteLine($"# Platform: {Application.platform}");
                sw.WriteLine();
                sw.WriteLine("time_s\tfps\tmem_mb");
            }
            Debug.Log($"[PerformanceReporter] Logging to {_outputPath}");
#else
            enabled = false;
#endif
        }

        private void Update()
        {
            _fpsSum += 1f / Time.deltaTime;
            _fpsCount++;
            _frameCounter++;

            if (_frameCounter >= sampleIntervalFrames)
            {
                float t = Time.realtimeSinceStartup - _startTime;
                float avgFps = _fpsSum / _fpsCount;
                long memBytes = System.GC.GetTotalMemory(false);
                float memMb = memBytes / (1024f * 1024f);

                File.AppendAllText(_outputPath, $"{t:F2}\t{avgFps:F1}\t{memMb:F1}\n");

                _frameCounter = 0;
                _fpsSum = 0f;
                _fpsCount = 0;
                _samplesWritten++;

                if (t >= durationSeconds)
                {
                    Debug.Log($"[PerformanceReporter] Complete: {_samplesWritten} samples over {t:F1}s");
                    enabled = false;
                }
            }
        }
    }
}
