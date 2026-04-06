using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterhumans.Dialogue
{
    /// <summary>
    /// Canvas-based dialogue UI. Subscribes to DialogueManager events to display
    /// typewriter text, choices, and hide when dialogue ends.
    ///
    /// BOT-N06: speaker name prefix — parses "Name: text" pattern at start of
    /// each line. If present, shows Name in speakerText TMP with accent color.
    /// BOT-N07: typewriter 22 cps + skip-on-E via RequestContinue().
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [Header("UI Refs")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI lineText;
        [SerializeField] private TextMeshProUGUI speakerText;  // BOT-N06
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Typewriter")]
        [SerializeField] private float charsPerSecond = 22f;  // BOT-N07: was 30

        private Coroutine _typingCoroutine;
        private string _fullLine;
        private bool _isTyping;
        private List<Button> _activeChoices = new List<Button>();
        private bool _subscribed;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void Update()
        {
            // Lazy subscribe in case DialogueManager wasn't ready at Start.
            if (!_subscribed) TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (DialogueManager.Instance == null) return;
            DialogueManager.Instance.OnDialogueLine += HandleLine;
            DialogueManager.Instance.OnDialogueChoices += HandleChoices;
            DialogueManager.Instance.OnDialogueEnd += HandleEnd;
            _subscribed = true;
            Debug.Log("[DialogueUI] Subscribed to DialogueManager events");
        }

        private void OnDisable()
        {
            if (_subscribed && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueLine -= HandleLine;
                DialogueManager.Instance.OnDialogueChoices -= HandleChoices;
                DialogueManager.Instance.OnDialogueEnd -= HandleEnd;
                _subscribed = false;
            }
        }

        private void HandleLine(string line)
        {
            if (panel != null) panel.SetActive(true);
            ClearChoices();

            // BOT-N06: parse "Name: text" speaker prefix (handles Cyrillic names)
            // mm-review MEDIUM fix: reject URL-like colons (http:, https:, ftp:)
            // and enforce max speaker length 20 chars to avoid false positives.
            string speaker = null;
            string content = line;
            int colonIdx = line.IndexOf(':');
            if (colonIdx > 1 && colonIdx < 20)
            {
                string candidate = line.Substring(0, colonIdx).Trim();
                // Reject URL protocols and numeric prefixes
                string lower = candidate.ToLowerInvariant();
                bool isUrl = lower == "http" || lower == "https" || lower == "ftp";
                // Validate: speaker name contains only letters/spaces (no URLs, no numbers)
                bool valid = !isUrl;
                if (valid)
                {
                    foreach (char c in candidate)
                    {
                        if (!char.IsLetter(c) && c != ' ' && c != '-') { valid = false; break; }
                    }
                }
                if (valid && candidate.Length >= 2)
                {
                    speaker = candidate;
                    content = line.Substring(colonIdx + 1).TrimStart();
                }
            }

            if (speakerText != null)
            {
                if (speaker != null)
                {
                    speakerText.text = speaker;
                    speakerText.gameObject.SetActive(true);
                }
                else
                {
                    speakerText.gameObject.SetActive(false);
                }
            }

            _fullLine = content;
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            if (isActiveAndEnabled)
                _typingCoroutine = StartCoroutine(TypeLine(content));
        }

        private IEnumerator TypeLine(string text)
        {
            _isTyping = true;
            if (lineText == null)
            {
                _isTyping = false;
                yield break;
            }
            lineText.text = "";
            float delay = 1f / Mathf.Max(charsPerSecond, 1f);
            foreach (char c in text)
            {
                lineText.text += c;
                yield return new WaitForSeconds(delay);
            }
            _isTyping = false;
        }

        private void HandleChoices(List<Choice> choices)
        {
            ClearChoices();
            if (choiceButtonPrefab == null || choicesContainer == null) return;

            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                int idx = i;
                Button btn = Instantiate(choiceButtonPrefab, choicesContainer);
                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = $"> {choice.text}";
                btn.onClick.AddListener(() => OnChoiceSelected(idx));
                _activeChoices.Add(btn);
            }
        }

        private void OnChoiceSelected(int index)
        {
            DialogueManager.Instance?.ChooseChoice(index);
            ClearChoices();
        }

        private void ClearChoices()
        {
            foreach (var btn in _activeChoices)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _activeChoices.Clear();
        }

        private void HandleEnd()
        {
            ClearChoices();
            if (panel != null) panel.SetActive(false);
        }

        /// <summary>
        /// Called from PlayerInteraction when E/Space is pressed during dialogue.
        /// If typing, finish the line immediately. Otherwise advance story.
        /// </summary>
        public void RequestContinue()
        {
            if (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive) return;
            if (_isTyping)
            {
                if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
                if (lineText != null) lineText.text = _fullLine;
                _isTyping = false;
            }
            else if (_activeChoices.Count == 0)
            {
                DialogueManager.Instance.ContinueStory();
            }
        }
    }
}
