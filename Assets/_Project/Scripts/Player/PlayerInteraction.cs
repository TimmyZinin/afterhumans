using UnityEngine;
using UnityEngine.InputSystem;
using Afterhumans.Dialogue;

namespace Afterhumans.Player
{
    /// <summary>
    /// Raycasts from camera forward every frame to find the closest Interactable.
    /// Shows prompt UI and triggers Interact() on E press.
    /// Attach to Player GameObject alongside the FirstPerson controller.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float maxDistance = 3f;
        [SerializeField] private LayerMask interactableLayer = ~0;

        [Header("UI")]
        [Tooltip("World-space or screen-space prompt UI (assign from inspector)")]
        [SerializeField] private InteractionPrompt prompt;

        [Header("Input")]
        [SerializeField] private InputActionReference interactAction;

        private Interactable _currentTarget;

        private void OnEnable()
        {
            if (interactAction != null)
            {
                interactAction.action.performed += OnInteractPressed;
                interactAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.action.performed -= OnInteractPressed;
                interactAction.action.Disable();
            }
        }

        private void Update()
        {
            if (playerCamera == null) return;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                SetTarget(null);
                return;
            }

            Interactable found = null;
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer, QueryTriggerInteraction.Collide))
            {
                found = hit.collider.GetComponentInParent<Interactable>();
                if (found != null && !found.IsAvailable)
                {
                    found = null;
                }
            }

            SetTarget(found);
        }

        private void SetTarget(Interactable target)
        {
            if (target == _currentTarget) return;
            _currentTarget = target;

            if (prompt != null)
            {
                if (target != null)
                {
                    prompt.Show(target.transform.position + Vector3.up * 1.2f, target.promptText);
                }
                else
                {
                    prompt.Hide();
                }
            }
        }

        private void OnInteractPressed(InputAction.CallbackContext ctx)
        {
            if (_currentTarget != null)
            {
                _currentTarget.Interact();
            }
        }
    }
}
