using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterhumans.Dialogue
{
    /// <summary>
    /// Singleton manager for Ink dialogue system. Loads dataland.ink compiled JSON,
    /// starts knots on interaction, dispatches text/choices to DialogueUI.
    /// Persistent across scenes via DontDestroyOnLoad.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Ink Story")]
        [Tooltip("Compiled Ink JSON file (dataland.json generated from dataland.ink)")]
        public TextAsset inkJsonAsset;

        [Header("UI References")]
        [SerializeField] private DialogueUI dialogueUI;

        public Story story { get; private set; }
        public bool IsDialogueActive { get; private set; }

        public event Action<string> OnDialogueLine;
        public event Action<List<Choice>> OnDialogueChoices;
        public event Action OnDialogueEnd;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (inkJsonAsset != null)
            {
                story = new Story(inkJsonAsset.text);
            }
            else
            {
                Debug.LogError("[DialogueManager] inkJsonAsset not assigned");
            }
        }

        /// <summary>
        /// Start a dialogue knot by name. Called from Interactable on E press.
        /// </summary>
        public void StartKnot(string knotName)
        {
            if (story == null)
            {
                Debug.LogError("[DialogueManager] Story not initialized");
                return;
            }

            try
            {
                story.ChoosePathString(knotName);
                IsDialogueActive = true;
                ContinueStory();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueManager] Failed to start knot '{knotName}': {e.Message}");
            }
        }

        /// <summary>
        /// Continue the story, emitting text lines and choices via events.
        /// Called on Space/E press or automatically after knot start.
        /// </summary>
        public void ContinueStory()
        {
            if (story == null || !IsDialogueActive) return;

            if (story.canContinue)
            {
                string line = story.Continue().Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    OnDialogueLine?.Invoke(line);
                    return;
                }
                // Empty line, skip
                ContinueStory();
                return;
            }

            if (story.currentChoices.Count > 0)
            {
                OnDialogueChoices?.Invoke(story.currentChoices);
                return;
            }

            EndDialogue();
        }

        /// <summary>
        /// Player selected a choice (0-indexed). Ink continues with that branch.
        /// </summary>
        public void ChooseChoice(int index)
        {
            if (story == null || !IsDialogueActive) return;
            if (index < 0 || index >= story.currentChoices.Count) return;

            story.ChooseChoiceIndex(index);
            ContinueStory();
        }

        /// <summary>
        /// Force end dialogue (e.g. on scene transition).
        /// </summary>
        public void EndDialogue()
        {
            IsDialogueActive = false;
            OnDialogueEnd?.Invoke();
        }

        /// <summary>
        /// Emit a line to the dialogue UI without going through Ink.
        /// Used by Kafka reactions and other standalone subtitle sources.
        /// </summary>
        public void EmitLine(string line)
        {
            OnDialogueLine?.Invoke(line);
        }

        /// <summary>
        /// Get a boolean variable from the Ink story state (for gate logic etc).
        /// </summary>
        public bool GetBoolVar(string name)
        {
            if (story == null) return false;
            try { return (bool)story.variablesState[name]; }
            catch { return false; }
        }

        /// <summary>
        /// Get an integer variable from the Ink story state.
        /// </summary>
        public int GetIntVar(string name)
        {
            if (story == null) return 0;
            try { return (int)story.variablesState[name]; }
            catch { return 0; }
        }
    }
}
