using UnityEngine;
using Afterhumans.Dialogue;

namespace Afterhumans.Scenes
{
    /// <summary>
    /// Box collider set as trigger — when the Player enters, fades to black and
    /// loads the target scene via SceneTransition. Used as the walking-skeleton
    /// way to stitch Botanika → City → Desert → Credits.
    ///
    /// Optional Ink gate: if gateInkVarName is set, the trigger only loads the
    /// next scene when that bool variable is true in the current Ink story;
    /// otherwise it starts the lockedKnot as a "not yet" message. This is used
    /// for the Botanika → City door that requires talking to Nikolai first.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneExitTrigger : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Name of the scene to load when the player walks into the trigger")]
        public string targetScene;

        [Header("Optional Ink gate")]
        [Tooltip("If set, the scene only loads when this Ink bool var is true")]
        public string gateInkVarName;

        [Tooltip("Ink knot to start when the gate is closed (e.g. 'door_to_city')")]
        public string lockedKnot;

        [Tooltip("Seconds of cooldown after a locked-gate attempt before the trigger can re-fire")]
        public float lockedCooldown = 3f;

        private bool _fired;
        private float _lockedUntil;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleEnter(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // Re-check on stay so the player can try again after they talk to Nikolai.
            if (_fired) return;
            if (Time.time < _lockedUntil) return;
            HandleEnter(other);
        }

        private void HandleEnter(Collider other)
        {
            if (_fired) return;
            if (!other.CompareTag("Player")) return;
            if (string.IsNullOrEmpty(targetScene)) return;

            // Ink gate check
            if (!string.IsNullOrEmpty(gateInkVarName))
            {
                var dm = DialogueManager.Instance;
                if (dm == null)
                {
                    Debug.LogWarning("[SceneExitTrigger] Gated exit but DialogueManager not ready");
                    return;
                }
                bool open = dm.GetBoolVar(gateInkVarName);
                if (!open)
                {
                    if (Time.time < _lockedUntil) return;
                    _lockedUntil = Time.time + lockedCooldown;
                    if (!string.IsNullOrEmpty(lockedKnot))
                    {
                        Debug.Log($"[SceneExitTrigger] Gate {gateInkVarName} closed → knot {lockedKnot}");
                        dm.StartKnot(lockedKnot);
                    }
                    return;
                }
            }

            _fired = true;
            Debug.Log($"[SceneExitTrigger] Player entered exit for {targetScene}");

            var transition = SceneTransition.Instance;
            if (transition != null)
            {
                transition.LoadScene(targetScene);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
            }
        }
    }
}
