using UnityEngine;
using Afterhumans.Dialogue;

namespace Afterhumans.Player
{
    /// <summary>
    /// Scans scene for Interactable objects each frame and picks the closest one
    /// within interactRadius of the player. Triggers Interact() on E press.
    ///
    /// Distance-based (not raycast) so player doesn't need to aim precisely.
    ///
    /// Uses legacy Input Manager (Input.GetKeyDown) to match SimpleFirstPersonController.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float maxDistance = 4f;

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode continueKey = KeyCode.Space;

        [Header("Debug HUD")]
        [Tooltip("BOT-F09: off by default in production. Use Afterhumans/Debug/Toggle PlayerInteraction HUD to enable for QA.")]
        [SerializeField] private bool showDebugHud = false;

        private Interactable _currentTarget;
        private float _currentDistance = Mathf.Infinity;
        private string _lastEvent = "—";

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Update()
        {
            var dm = DialogueManager.Instance;

            // While dialogue is active, E/Space advances dialogue, no world scan.
            // BOT-N07: route through DialogueUI.RequestContinue for typewriter
            // skip-on-first-press + advance-on-second semantics. Direct
            // ContinueStory() was skipping lines mid-typing without showing them.
            if (dm != null && dm.IsDialogueActive)
            {
                _currentTarget = null;
                if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(continueKey))
                {
                    _lastEvent = "Continue pressed";
                    if (DialogueUI.Instance != null)
                    {
                        DialogueUI.Instance.RequestContinue();
                    }
                    else
                    {
                        dm.ContinueStory();  // fallback if UI not found
                    }
                }
                return;
            }

            // BOT-F08: use static cached list (Interactable.All) вместо FindObjectsByType
            // каждый кадр. Skill game-development anti-pattern «update everything every frame».
            Interactable closest = null;
            float closestDistSq = maxDistance * maxDistance;
            var playerPos = transform.position;
            var all = Interactable.All;
            for (int i = 0; i < all.Count; i++)
            {
                var it = all[i];
                if (it == null || !it.IsAvailable) continue;
                float dSq = (it.transform.position - playerPos).sqrMagnitude;
                if (dSq <= closestDistSq)
                {
                    closestDistSq = dSq;
                    closest = it;
                }
            }

            _currentTarget = closest;
            _currentDistance = closest != null ? Mathf.Sqrt(closestDistSq) : Mathf.Infinity;

            if (_currentTarget != null && Input.GetKeyDown(interactKey))
            {
                _lastEvent = $"E pressed, Interact({_currentTarget.knotName})";
                _currentTarget.Interact();
            }
        }

        private void OnGUI()
        {
            if (!showDebugHud) return;

            var dm = DialogueManager.Instance;
            string dmStatus = dm == null ? "NULL" : (dm.IsDialogueActive ? "ACTIVE" : "idle");
            string tgtStatus = _currentTarget == null
                ? "none"
                : $"{_currentTarget.name} ({_currentDistance:F2}m) knot={_currentTarget.knotName}";

            string hud =
                $"DialogueManager: {dmStatus}\n" +
                $"Target: {tgtStatus}\n" +
                $"Last event: {_lastEvent}\n" +
                $"Press E to interact";

            var style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = Color.yellow;
            style.normal.background = Texture2D.whiteTexture;

            var bgStyle = new GUIStyle();
            bgStyle.normal.background = Texture2D.whiteTexture;

            // Background rectangle
            var oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(10, 10, 520, 110), Texture2D.whiteTexture);
            GUI.color = oldColor;

            // Text
            GUI.Label(new Rect(20, 16, 500, 100), hud, new GUIStyle
            {
                fontSize = 18,
                normal = new GUIStyleState { textColor = Color.yellow }
            });
        }
    }
}
