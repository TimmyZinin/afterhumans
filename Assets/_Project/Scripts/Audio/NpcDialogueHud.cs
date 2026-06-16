using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Afterhumans.Audio
{
    /// <summary>
    /// BOT-N12: Self-contained on-screen dialogue box. Created lazily at runtime
    /// (no editor wiring, no Ink, no DialogueManager) so it ALWAYS renders — the
    /// previous Ink/DialogueUI path showed NO window in the real WebGL build and
    /// froze on E. NpcVoice writes here directly when the dog approaches an NPC.
    ///
    /// Cyrillic comes from TMP's default font asset (LiberationSans SDF, dynamic).
    /// </summary>
    public class NpcDialogueHud : MonoBehaviour
    {
        private static NpcDialogueHud _inst;
        private TextMeshProUGUI _speaker, _line;
        private GameObject _panel;
        private float _hideAt;

        public static NpcDialogueHud Get()
        {
            if (_inst != null) return _inst;
            var go = new GameObject("NpcDialogueHud");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<NpcDialogueHud>();
            _inst.Build();
            return _inst;
        }

        private void Build()
        {
            var canvasGo = new GameObject("HudCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var img = _panel.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.78f);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.10f, 0.05f); rt.anchorMax = new Vector2(0.90f, 0.27f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            _speaker = MakeText(_panel.transform, 36f, new Color(1f, 0.85f, 0.4f), FontStyles.Bold,
                new Vector2(0.03f, 0.66f), new Vector2(0.97f, 0.95f));
            _line = MakeText(_panel.transform, 30f, Color.white, FontStyles.Normal,
                new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.62f));
            _panel.SetActive(false);
        }

        private TextMeshProUGUI MakeText(Transform p, float size, Color c, FontStyles st, Vector2 amin, Vector2 amax)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(p, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.color = c; t.fontStyle = st;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.enableWordWrapping = true;
            var f = TMP_Settings.defaultFontAsset;
            if (f != null) t.font = f;
            else Debug.LogWarning("[NpcDialogueHud] TMP defaultFontAsset is null — Cyrillic subtitles may render as boxes. Import TMP Essential Resources.");
            var rt = t.rectTransform;
            rt.anchorMin = amin; rt.anchorMax = amax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return t;
        }

        public void Show(string speaker, string line, float holdSeconds)
        {
            if (_panel == null) return;
            _panel.SetActive(true);
            if (_speaker != null) _speaker.text = speaker;
            if (_line != null) _line.text = line;
            _hideAt = Time.time + holdSeconds;
        }

        public void Hide() { if (_panel != null) _panel.SetActive(false); }

        // Reset the static singleton when destroyed (scene reload / editor stop-play) so a
        // fresh, fully-built HUD is created next time instead of returning a dead zombie.
        private void OnDestroy() { if (_inst == this) _inst = null; }

        private void Update()
        {
            if (_panel != null && _panel.activeSelf && Time.time > _hideAt) _panel.SetActive(false);
        }
    }
}
