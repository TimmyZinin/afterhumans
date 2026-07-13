using System.Collections.Generic;
using UnityEngine;

namespace Afterhumans.Audio
{
    /// <summary>
    /// E-sprint (E1.3): tracks which of the 4 Botanika NPCs (sasha/kirill/mila/stas) the
    /// player has talked to at least once. Pure static state hooked from
    /// NpcVoice.SpeakNext() (one line per call site) — no Ink, no per-frame polling
    /// MonoBehaviour, so this cannot reintroduce the Sprint D WebGL freeze (that path was
    /// PlayerInteraction → Interactable → DialogueManager; this is a plain static method call).
    ///
    /// Nikolai is deliberately NOT in the tracked set — he's the gatekeeper who reacts to the
    /// other 4 being met, not one of the 4 himself.
    /// </summary>
    public static class NpcProgressTracker
    {
        private static readonly HashSet<string> TrackedIds = new HashSet<string> { "sasha", "kirill", "mila", "stas" };
        private static readonly HashSet<string> Met = new HashSet<string>();

        // E-sprint (12 июл, P0-3 diagnostic): bare instrumental-case names (no preposition —
        // UnmetSummary attaches "с"/"со" once, at the front of the list, per Russian grammar).
        private static readonly Dictionary<string, string> DisplayInstrBare = new Dictionary<string, string>
        {
            { "sasha", "Сашей" }, { "kirill", "Кириллом" }, { "mila", "Милой" }, { "stas", "Стасом" },
        };

        public static bool AllFourMet => Met.Count >= TrackedIds.Count;

        /// <summary>
        /// "Поговори ещё с X и Y." for whichever tracked NPCs are NOT yet met — used by
        /// CityDoorGate's locked message so a refused entry tells the player exactly what's
        /// missing instead of a vague "не сейчас". Empty string once AllFourMet.
        /// </summary>
        public static string UnmetSummary()
        {
            var missing = new List<string>();
            foreach (var id in TrackedIds)
                if (!Met.Contains(id) && DisplayInstrBare.TryGetValue(id, out var n)) missing.Add(n);
            if (missing.Count == 0) return "";
            // "со Стасом" (not "с Стасом") when Stas leads the list — phonetic rule on the
            // word immediately after the preposition; mid-list "и Стасом" needs no preposition.
            string prep = missing[0] == "Стасом" ? "со " : "с ";
            string list = missing.Count == 1
                ? prep + missing[0]
                : prep + string.Join(", ", missing.GetRange(0, missing.Count - 1)) + " и " + missing[missing.Count - 1];
            return "Поговори ещё " + list + ".";
        }

        /// <summary>
        /// True once Nikolai has actually SPOKEN a gate line aloud (not merely once
        /// AllFourMet flips true) — this is the flag CityDoorGate opens on, matching the
        /// story beat "когда все 4 → Николай даёт финальную реплику и открывается дверь".
        /// </summary>
        public static bool DoorUnlocked { get; private set; }

        /// <summary>Called from NpcVoice.SpeakNext() for every normal (non-gate) line spoken.</summary>
        public static void OnSpoken(string npcGameObjectName)
        {
            string id = NormalizeId(npcGameObjectName);
            bool tracked = id != null && TrackedIds.Contains(id);
            // E-sprint (12 июл, P0-3): Tim's #5d playtest — door stayed locked after visiting
            // all 4, AND a door-adjacent line came from the WRONG NPC too early. Per team-lead
            // (IL-3): log full state on every call, don't guess-fix. This line, plus NpcVoice's
            // own [VoiceDiag] log, is what a live console capture needs to show WHICH NPC/id
            // triggered what, and whether Met ever contains something it shouldn't.
            Debug.Log($"[ProgressDiag] OnSpoken raw='{npcGameObjectName}' id='{id}' tracked={tracked} metBefore=[{string.Join(",", Met)}]");
            if (!tracked) return;

            bool wasAllMet = AllFourMet;
            Met.Add(id);
            Debug.Log($"[ProgressDiag] OnSpoken metAfter=[{string.Join(",", Met)}] AllFourMet={AllFourMet}");
            if (!wasAllMet && AllFourMet)
            {
                // One-shot nudge toward Nikolai — no Ink, just the existing self-contained HUD.
                NpcDialogueHud.Get().Show("", "Кажется, стоит поговорить с Николаем.", 4f);
            }
        }

        /// <summary>Called from NpcVoice.SpeakNext() when a gate line is actually spoken.</summary>
        public static void OnGateLineSpoken(string npcGameObjectName)
        {
            Debug.Log($"[ProgressDiag] OnGateLineSpoken raw='{npcGameObjectName}' AllFourMet={AllFourMet} DoorUnlockedBefore={DoorUnlocked} met=[{string.Join(",", Met)}]");
            if (AllFourMet) DoorUnlocked = true;
            Debug.Log($"[ProgressDiag] OnGateLineSpoken DoorUnlockedAfter={DoorUnlocked}");
        }

        private static string NormalizeId(string goName)
        {
            if (string.IsNullOrEmpty(goName)) return null;
            const string prefix = "NPC_";
            string s = goName.StartsWith(prefix) ? goName.Substring(prefix.Length) : goName;
            return s.ToLowerInvariant();
        }

        /// <summary>Reset hook for a future New Game flow. Not wired to any runtime path yet.</summary>
        public static void ResetAll()
        {
            Met.Clear();
            DoorUnlocked = false;
        }
    }
}
