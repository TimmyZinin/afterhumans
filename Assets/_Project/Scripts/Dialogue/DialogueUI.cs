using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Afterhumans.Dialogue
{
    /// <summary>
    /// Canvas-based dialogue UI. Subscribes to DialogueManager events to display
    /// typewriter text, choices, and hide when dialogue ends.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI lineText;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonPrefab;

        [Header("Typewriter")]
        [SerializeField] private float charsPerSecond = 30f;
        [SerializeField] private InputActionReference continueAction;
        [SerializeField] private InputActionReference skipAction;

        private Coroutine _typingCoroutine;
        private string _fullLine;
        private bool _isTyping;
        private List<Button> _activeChoices = new List<Button>();

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueLine += HandleLine;
                DialogueManager.Instance.OnDialogueChoices += HandleChoices;
                DialogueManager.Instance.OnDialogueEnd += HandleEnd;
            }

            if (continueAction != null)
            {
                continueAction.action.performed += OnContinue;
                continueAction.action.Enable();
            }
            if (skipAction != null)
            {
                skipAction.action.performed += OnSkip;
                skipAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueLine -= HandleLine;
                DialogueManager.Instance.OnDialogueChoices -= HandleChoices;
                DialogueManager.Instance.OnDialogueEnd -= HandleEnd;
            }
            if (continueAction != null) continueAction.action.performed -= OnContinue;
            if (skipAction != null) skipAction.action.performed -= OnSkip;
        }

        private void HandleLine(string line)
        {
            if (panel != null) panel.SetActive(true);
            ClearChoices();
            _fullLine = line;
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _typingCoroutine = StartCoroutine(TypeLine(line));
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

        private void OnContinue(InputAction.CallbackContext ctx)
        {
            if (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive) return;
            if (_isTyping)
            {
                // Skip typewriter — show full line
                if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
                if (lineText != null) lineText.text = _fullLine;
                _isTyping = false;
            }
            else if (_activeChoices.Count == 0)
            {
                DialogueManager.Instance.ContinueStory();
            }
        }

        private void OnSkip(InputAction.CallbackContext ctx)
        {
            OnContinue(ctx);
        }
    }
}
