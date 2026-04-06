using System.Collections;
using TMPro;
using UnityEngine;
using Afterhumans.Dialogue;

namespace Afterhumans.UI
{
    /// <summary>
    /// BOT-S05: Monitors Ink variable `door_to_city_open` and shows a
    /// bottom-screen subtitle when the gate opens after Nikolai's dialogue.
    ///
    /// STORY §3.2: "После Николая — дверь открыта. Ты можешь идти."
    ///
    /// Creates its own TMP element on Awake (self-contained, no prefab).
    /// Fades in over 0.5s, holds 3s, fades out 0.5s. One-shot — doesn't
    /// re-trigger if player revisits Николай.
    /// </summary>
    public class DoorCueUI : MonoBehaviour
    {
        [SerializeField] private string inkVarName = "door_to_city_open";
        [SerializeField] private string cueText = "Дверь открыта. Ты можешь идти.";
        [SerializeField] private float holdDuration = 3f;
        [SerializeField] private float fadeDuration = 0.5f;

        private bool _triggered;
        private bool _lastValue;
        private TextMeshProUGUI _text;
        private CanvasGroup _group;

        private void Awake()
        {
            // Self-contained UI — create Canvas + TMP
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            gameObject.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var textGo = new GameObject("DoorCueText");
            textGo.transform.SetParent(transform, false);
            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.05f);
            rect.anchorMax = new Vector2(0.85f, 0.12f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.text = cueText;
            _text.fontSize = 26;
            _text.color = new Color(0.95f, 0.88f, 0.72f);
            _text.alignment = TextAlignmentOptions.Center;
            _text.fontStyle = FontStyles.Italic;

            _group = textGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        private void Update()
        {
            if (_triggered) return;
            var dm = DialogueManager.Instance;
            if (dm == null) return;

            bool doorOpen = dm.GetBoolVar(inkVarName);
            if (doorOpen && !_lastValue)
            {
                _triggered = true;
                StartCoroutine(ShowCue());
            }
            _lastValue = doorOpen;
        }

        private IEnumerator ShowCue()
        {
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
        }
    }
}
