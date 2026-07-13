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
            // E-sprint diagnostic (team-lead 12 июл: Stas/Kirill don't respond to E, Sasha's
            // cycle fires instead) — log EVERY candidate's distance on every E-press so the
            // NEXT reproduction pins down whether this is a selection-logic bug or a
            // logical-position-vs-visual-mesh mismatch, instead of guessing blind.
            var sb = new System.Text.StringBuilder();
            sb.Append($"[NpcInteractor] E at dogPos={transform.position} candidates: ");
            foreach (var v in Object.FindObjectsByType<NpcVoice>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(transform.position, v.transform.position);
                sb.Append($"{v.gameObject.name}@{v.transform.position}(d={d:F2}) ");
                if (d <= bestD) { bestD = d; best = v; }
            }
            sb.Append($"-> PICKED={(best != null ? best.gameObject.name : "none")}");
            Debug.Log(sb.ToString());
            if (best != null) best.ForceSpeak();
        }
    }
}
