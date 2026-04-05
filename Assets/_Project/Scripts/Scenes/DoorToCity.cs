using UnityEngine;
using Afterhumans.Dialogue;

namespace Afterhumans.Scenes
{
    /// <summary>
    /// Gate for the door from Botanika to City.
    /// Only opens after met_nikolai == true (set by Nikolai's knot in Ink).
    /// Until then, plays the "Не сейчас. Ты ещё не узнал, что снаружи." voice line.
    /// </summary>
    public class DoorToCity : MonoBehaviour
    {
        [SerializeField] private string inkVarName = "door_to_city_open";
        [SerializeField] private string targetScene = "Scene_City";
        [SerializeField] private string lockedMessageKnot = "door_to_city";

        public void TryOpen()
        {
            var dm = DialogueManager.Instance;
            if (dm == null) return;

            bool isOpen = dm.GetBoolVar(inkVarName);
            if (isOpen)
            {
                // Fade and load city scene
                SceneTransition.Instance?.LoadScene(targetScene);
            }
            else
            {
                // Play locked voice line via dialogue knot
                dm.StartKnot(lockedMessageKnot);
            }
        }
    }
}
