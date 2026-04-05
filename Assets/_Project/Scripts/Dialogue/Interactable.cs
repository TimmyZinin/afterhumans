using UnityEngine;
using UnityEngine.Events;

namespace Afterhumans.Dialogue
{
    /// <summary>
    /// Attach to any GameObject that the player can interact with (NPC, Kafka, Cursor, door).
    /// On E press within interactRadius, starts the Ink knot with matching name.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Interactable : MonoBehaviour
    {
        [Header("Dialogue")]
        [Tooltip("Ink knot name to jump to when E pressed")]
        public string knotName;

        [Tooltip("Text shown above this object when in range, e.g. 'говорить', 'взять'")]
        public string promptText = "говорить";

        [Header("Interaction")]
        [Tooltip("Max distance from player for interaction in meters")]
        public float interactRadius = 2.5f;

        [Tooltip("If true, can only be triggered once (notes, pickups). NPCs should be false.")]
        public bool oneTime = false;

        [Header("Optional Events")]
        public UnityEvent onInteracted;

        private bool _used;

        public bool IsAvailable => !_used || !oneTime;

        public void Interact()
        {
            if (!IsAvailable) return;

            if (!string.IsNullOrEmpty(knotName) && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartKnot(knotName);
            }

            onInteracted?.Invoke();

            if (oneTime)
            {
                _used = true;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
