using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Afterhumans.Art;

namespace Afterhumans.EditorTools
{
    /// <summary>
    /// BOT-F06: Создаёт 5 SceneTheme.asset файлов с палитрами из ART_BIBLE §3.
    /// </summary>
    public static class SceneThemeBuilder
    {
        private const string ThemesDir = "Assets/_Project/Art/Themes";

        [MenuItem("Afterhumans/Setup/Build Scene Themes")]
        public static void BuildAll()
        {
            if (!Directory.Exists(ThemesDir))
            {
                Directory.CreateDirectory(ThemesDir);
                AssetDatabase.Refresh();
            }

            BuildBotanika();
            BuildCity();
            BuildDesert();
            BuildMainMenu();
            BuildCredits();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneThemeBuilder] All 5 theme assets created in " + ThemesDir);
        }

        private static SceneTheme GetOrCreate(string name)
        {
            string path = $"{ThemesDir}/{name}.asset";
            var theme = AssetDatabase.LoadAssetAtPath<SceneTheme>(path);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<SceneTheme>();
                AssetDatabase.CreateAsset(theme, path);
            }
            return theme;
        }

        private static void BuildBotanika()
        {
            var t = GetOrCreate("Botanika");
            t.displayName = "Botanika";
            t.description = "Warm golden hour oasis — greenhouse interior with afternoon sun through glass ceiling";
            // ART_BIBLE §3.1
            t.primary   = Hex("#E8A75C");
            t.secondary = Hex("#2D4A3E");
            t.tertiary  = Hex("#8B6F4E");
            t.accent1   = Hex("#F2C084");
            t.accent2   = Hex("#A33F2D");
            t.shadow    = Hex("#3A2819");
            // Lighting
            t.sunTemperatureKelvin = 3200f;
            t.sunIntensity = 1.4f;
            t.sunRotation = new Vector3(25f, -45f, 0f);
            t.ambientLight = Hex("#9A7A57");
            t.ambientIntensity = 1.25f;
            t.ambientMode = AmbientMode.Flat;
            // Fog — tight interior
            t.fogEnabled = true;
            t.fogMode = FogMode.ExponentialSquared;
            t.fogColor = Hex("#F5D8A3");
            t.fogDensity = 0.008f;
            // Dialogue
            t.dialoguePanelColor = new Color(0.227f, 0.157f, 0.098f, 0.85f);
            t.dialogueTextColor = Hex("#F2C084");
            t.dialogueSpeakerNameColor = Hex("#E8A75C");
            t.cameraFov = 65f;
            EditorUtility.SetDirty(t);
        }

        private static void BuildCity()
        {
            var t = GetOrCreate("City");
            t.displayName = "City";
            t.description = "Sterile Homo Deus — cool clinical daylight, blue-grey concrete";
            // ART_BIBLE §3.2
            t.primary   = Hex("#E8EEF2");
            t.secondary = Hex("#FAFCFD");
            t.tertiary  = Hex("#4A6578");
            t.accent1   = Hex("#B8C5CC");
            t.accent2   = Hex("#2B3640");
            t.shadow    = Hex("#1A2530");
            t.sunTemperatureKelvin = 7500f;
            t.sunIntensity = 0.9f;
            t.sunRotation = new Vector3(60f, 20f, 0f);
            t.ambientLight = Hex("#8090A0");
            t.ambientIntensity = 1.1f;
            t.fogEnabled = true;
            t.fogMode = FogMode.ExponentialSquared;
            t.fogColor = Hex("#D8E0E8");
            t.fogDensity = 0.022f;
            t.dialoguePanelColor = new Color(0.10f, 0.14f, 0.19f, 0.85f);
            t.dialogueTextColor = Hex("#E8EEF2");
            t.dialogueSpeakerNameColor = Hex("#B8C5CC");
            t.cameraFov = 65f;
            EditorUtility.SetDirty(t);
        }

        private static void BuildDesert()
        {
            var t = GetOrCreate("Desert");
            t.displayName = "Desert";
            t.description = "Dune sunset — oranjevyi scorched wasteland, long shadows, cosmic silence";
            // ART_BIBLE §3.3
            t.primary   = Hex("#D2602F");
            t.secondary = Hex("#6B3F8C");
            t.tertiary  = Hex("#C9955E");
            t.accent1   = Hex("#E87656");
            t.accent2   = Hex("#1E2440");
            t.shadow    = Hex("#3D1A1C");
            t.sunTemperatureKelvin = 2400f;
            t.sunIntensity = 2.8f;
            t.sunRotation = new Vector3(8f, 170f, 0f);
            t.ambientLight = Hex("#B8694A");
            t.ambientIntensity = 0.9f;
            t.fogEnabled = true;
            t.fogMode = FogMode.ExponentialSquared;
            t.fogColor = Hex("#C97648");
            t.fogDensity = 0.040f;
            t.dialoguePanelColor = new Color(0.24f, 0.10f, 0.11f, 0.85f);
            t.dialogueTextColor = Hex("#E87656");
            t.dialogueSpeakerNameColor = Hex("#D2602F");
            t.cameraFov = 65f;
            EditorUtility.SetDirty(t);
        }

        private static void BuildMainMenu()
        {
            var t = GetOrCreate("MainMenu");
            t.displayName = "MainMenu";
            t.description = "Episode cover art — uses Desert sunset palette to prefigure finale";
            t.primary   = Hex("#D2602F");
            t.secondary = Hex("#6B3F8C");
            t.tertiary  = Hex("#C9955E");
            t.accent1   = Hex("#F5E3C9");
            t.accent2   = Hex("#1E2440");
            t.shadow    = Hex("#3D1A1C");
            t.fogEnabled = false;
            t.cameraFov = 60f;
            EditorUtility.SetDirty(t);
        }

        private static void BuildCredits()
        {
            var t = GetOrCreate("Credits");
            t.displayName = "Credits";
            t.description = "Final reveal — warm cursor text on deep void";
            t.primary   = Hex("#F5E3C9");
            t.secondary = Hex("#1E2440");
            t.tertiary  = Hex("#8B6F4E");
            t.accent1   = Hex("#E87656");
            t.accent2   = Hex("#A33F2D");
            t.shadow    = Hex("#000000");
            t.fogEnabled = false;
            t.cameraFov = 60f;
            EditorUtility.SetDirty(t);
        }

        private static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }
    }
}
