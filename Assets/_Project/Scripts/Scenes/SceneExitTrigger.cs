using UnityEngine;

namespace Afterhumans.Scenes
{
    /// <summary>
    /// Box collider set as trigger — when the Player enters, fades to black and
    /// loads the target scene via SceneTransition. Used as the walking-skeleton
    /// way to stitch Botanika → City → Desert → Credits.
    ///
    /// Ink-gated doors (e.g. met_nikolai) use DoorToCity.cs instead.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneExitTrigger : MonoBehaviour
    {
        [Tooltip("Name of the scene to load when the player walks into the trigger")]
        public string targetScene;

        private bool _fired;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_fired) return;
            if (!other.CompareTag("Player")) return;
            if (string.IsNullOrEmpty(targetScene)) return;

            _fired = true;
            Debug.Log($"[SceneExitTrigger] Player entered exit for {targetScene}");

            var transition = SceneTransition.Instance;
            if (transition != null)
            {
                transition.LoadScene(targetScene);
            }
            else
            {
                // Fallback: hard load without fade
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
            }
        }
    }
}
