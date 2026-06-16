using System.Collections;
using UnityEngine;
using Afterhumans.Dialogue;
using Afterhumans.Scenes;

namespace Afterhumans.Cursor
{
    /// <summary>
    /// The mighty blinking ASCII cursor `> _` in the desert.
    /// Final interaction of Episode 0: player chooses one of 5 input options,
    /// which becomes the canonical baseline for Episode 1.
    ///
    /// On approach within triggerRadius, zoom camera FOV slightly and start cursor knot.
    /// </summary>
    public class CursorFinale : MonoBehaviour
    {
        [Header("Trigger")]
        [SerializeField] private Transform player;
        [SerializeField] private float triggerRadius = 2f;

        [Header("Camera")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float baseFov = 65f;
        [SerializeField] private float focusFov = 50f;

        [Header("Cursor Visuals")]
        [SerializeField] private float blinkInterval = 1f;
        [SerializeField] private Renderer cursorRenderer;
        [SerializeField] private Light cursorLight;

        [Header("Dialogue")]
        [SerializeField] private string cursorKnotName = "cursor";

        private bool _triggered;
        private float _blinkTimer;

        private void Start()
        {
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = baseFov;
            }
            if (player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null) player = go.transform;
            }
        }

        private void Update()
        {
            // Blink animation (visual only, text is handled by UI)
            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= blinkInterval)
            {
                _blinkTimer = 0f;
                if (cursorRenderer != null) cursorRenderer.enabled = !cursorRenderer.enabled;
                if (cursorLight != null) cursorLight.enabled = !cursorLight.enabled;
            }

            if (_triggered || player == null || playerCamera == null) return;

            float distance = Vector3.Distance(player.position, transform.position);
            if (distance <= triggerRadius)
            {
                _triggered = true;
                StartCoroutine(FinaleSequence());
            }
            else if (distance < 30f)
            {
                // Gradual FOV zoom as player approaches
                float t = 1f - Mathf.Clamp01((distance - triggerRadius) / (30f - triggerRadius));
                playerCamera.fieldOfView = Mathf.Lerp(baseFov, focusFov, t);
            }
        }

        private IEnumerator FinaleSequence()
        {
            // Freeze player movement (would go through PlayerController in real impl)
            yield return new WaitForSeconds(1f);

            // Start the cursor knot (player picks one of 5 inputs). Guard the manager once:
            // it may be absent (the Botanika flow strips Ink dialogue) — without this the
            // second deref below threw an NRE and broke the finale (Codex HIGH).
            var dm = DialogueManager.Instance;
            if (dm == null)
            {
                SceneTransition.Instance?.LoadScene("Scene_Credits");
                yield break;
            }
            dm.OnDialogueEnd += OnCursorDialogueEnd;   // subscribe before starting the knot
            dm.StartKnot(cursorKnotName);
        }

        private void OnCursorDialogueEnd()
        {
            var dm = DialogueManager.Instance;
            if (dm != null) dm.OnDialogueEnd -= OnCursorDialogueEnd;
            SceneTransition.Instance?.LoadScene("Scene_Credits");
        }
    }
}
