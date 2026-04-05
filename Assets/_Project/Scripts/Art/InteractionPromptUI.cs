using UnityEngine;
using TMPro;
using Afterhumans.Dialogue;

namespace Afterhumans.Art
{
    /// <summary>
    /// BOT-N05: Worldspace interaction prompt that hovers above NPCs when the
    /// player is within interactRadius. Billboard-locked to camera for readability,
    /// fades in/out for polish.
    ///
    /// Setup: SceneEnricher or BotanikaNpcPopulator creates a child Canvas (World
    /// Space) with this component on every Interactable NPC. Text defaults to
    /// «[E] говорить» but reads from Interactable.promptText if set.
    ///
    /// Skill references:
    /// - `ui-ux-pro-max`: proximity prompts are the #1 user need for narrative
    ///   walkers — "can I talk to this?" must be answered within 0.5s glance
    /// - `game-design`: diegetic → non-diegetic bridge (player HUD reads as part
    ///   of world when worldspace, not screen-overlay)
    /// - `3d-games` perf: single LateUpdate per NPC, no Find/GetComponent in loop
    ///
    /// Creates NO GameObjects itself — is purely a runtime fade-billboard behavior.
    /// BotanikaNpcPopulator.SpawnPrompt() creates the Canvas + TMP child.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("Fade")]
        public float fadeSpeed = 4f;
        public float showRadius = 4f;

        private CanvasGroup _group;
        private Transform _camera;
        private Interactable _interactable;
        private float _targetAlpha;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            _interactable = GetComponentInParent<Interactable>();
        }

        private void Start()
        {
            _camera = Camera.main != null ? Camera.main.transform : null;
            if (_camera == null)
            {
                var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cams.Length > 0) _camera = cams[0].transform;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null) return;

            // Billboard: always face camera
            transform.rotation = Quaternion.LookRotation(
                transform.position - _camera.position, Vector3.up);

            // Fade based on player distance + interactable availability
            float dist = Vector3.Distance(_camera.position, transform.position);
            bool show = dist <= showRadius && _interactable != null && _interactable.IsAvailable;

            // Hide during active dialogue
            var dm = DialogueManager.Instance;
            if (dm != null && dm.IsDialogueActive) show = false;

            _targetAlpha = show ? 1f : 0f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }
}
