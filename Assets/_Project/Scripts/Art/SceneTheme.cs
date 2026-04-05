using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

namespace Afterhumans.Art
{
    /// <summary>
    /// BOT-F06: Data-driven theme per scene. Один asset на сцену (Botanika, City,
    /// Desert, MainMenu, Credits). Все системы (Lighting, Dressers, DialogueUI,
    /// AudioSources) читают активный theme at scene load.
    ///
    /// Skill references:
    /// - `theme-factory`: cohesive palette + consistent application across artifacts
    /// - `game-art`: palette selection (warm/cool/mood), consistency rule «same object = same colour family»
    /// - `frontend-design`: Tim's calibration profile (colorful 6/7, display serif headlines, asymmetric)
    ///
    /// ART_BIBLE §3.1 Botanika palette:
    ///   primary #E8A75C (warm amber), secondary #2D4A3E (dark leaf green),
    ///   tertiary #8B6F4E (faded wood), accent1 #F2C084 (pink-orange lamp),
    ///   accent2 #A33F2D (deep red tile), shadow #3A2819 (warm brown shadow).
    /// </summary>
    [CreateAssetMenu(menuName = "Afterhumans/SceneTheme", fileName = "SceneTheme")]
    public class SceneTheme : ScriptableObject
    {
        [Header("Identity")]
        public string displayName;
        [TextArea] public string description;

        [Header("Palette (ART_BIBLE §3)")]
        [Tooltip("Dominant hue — warm amber in Botanika, cold blue-grey in City, sunset orange in Desert")]
        public Color primary = new Color(0.91f, 0.655f, 0.361f);    // #E8A75C
        public Color secondary = new Color(0.176f, 0.290f, 0.243f); // #2D4A3E
        public Color tertiary = new Color(0.545f, 0.435f, 0.306f);  // #8B6F4E
        public Color accent1 = new Color(0.949f, 0.753f, 0.518f);   // #F2C084
        public Color accent2 = new Color(0.639f, 0.247f, 0.176f);   // #A33F2D
        public Color shadow = new Color(0.227f, 0.157f, 0.098f);    // #3A2819

        [Header("Lighting (ART_BIBLE §4)")]
        [Tooltip("Kelvin temperature of directional sun light (3200 Botanika warm / 7500 City cool / 2400 Desert sunset)")]
        public float sunTemperatureKelvin = 3200f;
        [Tooltip("Euler rotation of directional sun")]
        public Vector3 sunRotation = new Vector3(25f, -45f, 0f);
        [Tooltip("Sun intensity multiplier")]
        public float sunIntensity = 1.4f;
        [Tooltip("Sun color sampled from temperature if not overridden")]
        public Color sunColorOverride = Color.clear;  // clear = derive from temp

        [Header("Ambient")]
        public Color ambientLight = new Color(0.58f, 0.48f, 0.34f);
        public float ambientIntensity = 1.25f;
        public AmbientMode ambientMode = AmbientMode.Flat;

        [Header("Fog (ART_BIBLE §4.1)")]
        public bool fogEnabled = true;
        public FogMode fogMode = FogMode.ExponentialSquared;
        public Color fogColor = new Color(0.95f, 0.80f, 0.58f);
        public float fogDensity = 0.008f;

        [Header("Sky")]
        [Tooltip("Skybox material; if null, camera clearColor = fogColor")]
        public Material skyboxMaterial;

        [Header("Camera")]
        [Tooltip("Default camera FOV for this scene")]
        public float cameraFov = 65f;

        [Header("Post-processing (URP Volume)")]
        [Tooltip("VolumeProfile asset с Bloom, Tonemapping, ColorAdjustments, Vignette, FilmGrain, DoF")]
        public VolumeProfile postFxProfile;

        [Header("Dialogue UI theming")]
        public Color dialoguePanelColor = new Color(0.227f, 0.157f, 0.098f, 0.85f); // shadow w/ alpha
        public Color dialogueTextColor = new Color(0.949f, 0.753f, 0.518f);          // accent1
        public Color dialogueSpeakerNameColor = new Color(0.91f, 0.655f, 0.361f);    // primary
        public TMP_FontAsset dialogueFont;

        [Header("Audio")]
        [Tooltip("Looping ambient music for this scene")]
        public AudioClip ambientMusicLoop;
        [Tooltip("Looping ambient SFX (wind, drone, etc.)")]
        public AudioClip ambientSfxLoop;

        [Header("Footsteps (skill game-audio §7 layering)")]
        [Tooltip("Primary footstep surface variations (4-5 clips preferred)")]
        public AudioClip[] footstepsPrimary;
        [Tooltip("Secondary surface variations (rug, path, etc.)")]
        public AudioClip[] footstepsSecondary;

        public static SceneTheme Active { get; private set; }

        public void MakeActive()
        {
            Active = this;
        }

        /// <summary>
        /// mm-review polish: clear static Active reference on scene unload so
        /// Editor domain reload + scene transitions don't leave stale references.
        /// Called from ThemeLoader.OnDestroy when owning theme matches Active.
        /// </summary>
        public static void ClearActive()
        {
            Active = null;
        }
    }
}
