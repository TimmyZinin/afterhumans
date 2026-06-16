using UnityEngine;

namespace Afterhumans.Audio
{
    /// <summary>
    /// Sits on the dog (player). On E-press it makes the NEAREST NPC within keyRadius speak its
    /// Russian line (audio + on-screen subtitle via NpcVoice/NpcDialogueHud). Tim's muscle memory
    /// presses E to talk, and auto-proximity alone wasn't reliably triggering in the build (the dog
    /// couldn't always get inside the small talk radius), so this is the explicit, dependable path.
    /// No Ink, no recursion → cannot freeze. The first E-press is also the user gesture that resumes
    /// the WebGL AudioContext, so sound starts working.
    /// </summary>
    public class NpcInteractor : MonoBehaviour
    {
        public float keyRadius = 7f;
        public KeyCode key = KeyCode.E;

        private void Update()
        {
            if (!Input.GetKeyDown(key)) return;
            NpcVoice best = null;
            float bestD = keyRadius;
            foreach (var v in Object.FindObjectsByType<NpcVoice>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(transform.position, v.transform.position);
                if (d <= bestD) { bestD = d; best = v; }
            }
            if (best != null) best.ForceSpeak();
        }
    }
}
