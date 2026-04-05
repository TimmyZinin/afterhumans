using TMPro;
using UnityEngine;

namespace Afterhumans.Dialogue
{
    /// <summary>
    /// World-space prompt that floats above interactable objects.
    /// Shown/hidden by PlayerInteraction based on raycast target.
    /// </summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private string keyPrefix = "[E] ";

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
            Hide();
        }

        private void LateUpdate()
        {
            if (worldCanvas != null && worldCanvas.gameObject.activeSelf && _mainCamera != null)
            {
                // Billboard — always face camera
                transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);
            }
        }

        public void Show(Vector3 worldPosition, string action)
        {
            if (worldCanvas != null)
            {
                worldCanvas.transform.position = worldPosition;
                worldCanvas.gameObject.SetActive(true);
            }
            if (promptText != null)
            {
                promptText.text = keyPrefix + action;
            }
        }

        public void Hide()
        {
            if (worldCanvas != null)
            {
                worldCanvas.gameObject.SetActive(false);
            }
        }
    }
}
