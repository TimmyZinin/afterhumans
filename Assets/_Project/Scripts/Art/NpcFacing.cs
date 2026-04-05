using UnityEngine;
using Afterhumans.Dialogue;

namespace Afterhumans.Art
{
    /// <summary>
    /// BOT-N08: Optional runtime component that rotates an NPC to face the
    /// player when the dialogue Interactable fires OnInteracted. Used by
    /// Николай who "поворачивается когда подходишь" per STORY §3.2.
    ///
    /// Other NPCs keep static orientation — enable turnOnInteract per-NPC
    /// only where narrative calls for it (avoid uncanny-valley constant
    /// head tracking).
    ///
    /// Slerps over ~1 second to target yaw — fast enough to feel responsive,
    /// slow enough to not jump. Lerp resets when interaction ends so NPC
    /// can return to default pose.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class NpcFacing : MonoBehaviour
    {
        [Tooltip("If true, NPC rotates to face player on Interact event")]
        public bool turnOnInteract = true;
        [Tooltip("Rotation speed in degrees per second")]
        public float rotationSpeed = 120f;
        [Tooltip("Only rotate around Y axis (no head tilt)")]
        public bool yawOnly = true;

        private Quaternion _targetRot;
        private bool _interacting;
        private Interactable _interactable;

        private void Awake()
        {
            _targetRot = transform.rotation;
            _interactable = GetComponent<Interactable>();
        }

        private void OnEnable()
        {
            if (_interactable != null)
            {
                _interactable.onInteracted.AddListener(HandleInteracted);
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.onInteracted.RemoveListener(HandleInteracted);
            }
        }

        private void HandleInteracted()
        {
            if (!turnOnInteract) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            var delta = player.transform.position - transform.position;
            if (yawOnly) delta.y = 0f;
            if (delta.sqrMagnitude < 0.001f) return;
            _targetRot = Quaternion.LookRotation(delta, Vector3.up);
            _interacting = true;
        }

        private void Update()
        {
            if (!_interacting) return;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, _targetRot, rotationSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, _targetRot) < 0.5f)
            {
                _interacting = false;
            }
        }
    }
}
