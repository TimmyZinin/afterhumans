using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Afterhumans.Scenes
{
    /// <summary>
    /// Handles fade-to-black scene transitions (GDD §8: 0.8s fade out → load → 0.8s fade in).
    /// Singleton, persistent across scenes.
    ///
    /// E-sprint fix: this component previously had NO spawn site anywhere in the live scenes
    /// (only an archived, non-compiled _v1_archive script created it) — every caller used
    /// `SceneTransition.Instance?.LoadScene(...)`, which silently no-opped. It now self-builds
    /// its own full-screen black overlay if none is wired in the Inspector, matching the
    /// self-contained pattern already used by NpcDialogueHud/DoorCueUI, so a single
    /// `gameObject.AddComponent&lt;SceneTransition&gt;()` anywhere is enough to make it work.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        [Header("Fade")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private float fadeDuration = 0.8f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Guarded: this Awake also fires once at EDIT time when a build script does
            // AddComponent<SceneTransition>() (see EnsureInstance below) — DontDestroyOnLoad
            // is play-mode-only and would log an edit-mode error otherwise. The scene is saved
            // with this component baked in, so the REAL Awake (Application.isPlaying == true)
            // fires normally when the built player loads the scene.
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            if (fadeOverlay == null) fadeOverlay = BuildFadeOverlay();

            fadeOverlay.color = new Color(0, 0, 0, 0);
            fadeOverlay.gameObject.SetActive(true);
        }

        private Image BuildFadeOverlay()
        {
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9000; // above NpcDialogueHud (5000) — fade must cover everything
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var imgGo = new GameObject("FadeOverlay");
            imgGo.transform.SetParent(canvasGo.transform, false);
            var img = imgGo.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return img;
        }

        /// <summary>
        /// Idempotent spawn helper for editor build scripts: creates the singleton if it
        /// doesn't already exist in the scene. Safe to call every build.
        /// </summary>
        public static void EnsureInstance()
        {
            if (Instance != null) return;
            var existing = Object.FindObjectOfType<SceneTransition>();
            if (existing != null) return; // Awake will assign Instance on next play/build
            var go = new GameObject("SceneTransition");
            go.AddComponent<SceneTransition>();
        }

        public void LoadScene(string sceneName)
        {
            StartCoroutine(FadeAndLoad(sceneName));
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            yield return StartCoroutine(Fade(0f, 1f));
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            yield return StartCoroutine(Fade(1f, 0f));
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeOverlay == null) yield break;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t));
                yield return null;
            }
            fadeOverlay.color = new Color(0, 0, 0, to);
        }
    }
}
