using System.Collections;
using TMPro;
using UnityEngine;

namespace Afterhumans.UI
{
    /// <summary>
    /// BOT-S08: Chapter title indicator that fades in after scene load.
    ///
    /// Shows large text "I. Ботаника" top-right, holds 2s, fades out 0.5s.
    /// Triggered automatically after configurable delay from Awake (default
    /// 8s to allow wake-up cinematic to finish per STORY §3.1 timing).
    ///
    /// Self-contained Canvas + TMP, no prefab needed.
    /// </summary>
    public class ChapterIndicatorUI : MonoBehaviour
    {
        [SerializeField] private string chapterText = "I. Ботаника";
        [SerializeField] private float showDelay = 8f;
        [SerializeField] private float holdDuration = 2.5f;
        [SerializeField] private float fadeDuration = 0.5f;

        private CanvasGroup _group;

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            gameObject.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var textGo = new GameObject("ChapterText");
            textGo.transform.SetParent(transform, false);
            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.6f, 0.82f);
            rect.anchorMax = new Vector2(0.95f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = chapterText;
            tmp.fontSize = 42;
            tmp.color = new Color(0.95f, 0.88f, 0.72f, 1f);
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.fontStyle = FontStyles.Normal;

            _group = textGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        private void Start()
        {
            StartCoroutine(ShowChapter());
        }

        private IEnumerator ShowChapter()
        {
            yield return new WaitForSeconds(showDelay);

            // Fade in
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _group.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
            _group.alpha = 1f;

            yield return new WaitForSeconds(holdDuration);

            // Fade out
            t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _group.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
            _group.alpha = 0f;

            Destroy(gameObject, 0.5f);
        }
    }
}
