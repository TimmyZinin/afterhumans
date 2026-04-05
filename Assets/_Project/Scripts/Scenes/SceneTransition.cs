using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Afterhumans.Scenes
{
    /// <summary>
    /// Handles fade-to-black scene transitions.
    /// Singleton, persistent across scenes.
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
            DontDestroyOnLoad(gameObject);

            if (fadeOverlay != null)
            {
                fadeOverlay.color = new Color(0, 0, 0, 0);
                fadeOverlay.gameObject.SetActive(true);
            }
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
