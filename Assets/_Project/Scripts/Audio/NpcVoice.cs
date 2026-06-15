using UnityEngine;
using Afterhumans.Dialogue;

namespace Afterhumans.Audio
{
    /// <summary>
    /// BOT-N10: Proximity voice. When the dog (player) walks near an NPC, the
    /// NPC speaks its own pre-recorded Russian voice line on a 3D AudioSource
    /// AND pushes a subtitle ("Speaker: text") to the DialogueUI. No key press —
    /// Tim's requirement: «когда собака ходит нпс в игре — они с ней говорят».
    ///
    /// Lines are wired at build time by BotanikaBuilder.WireBotanikaNpcs
    /// (clips[] + subtitles[] in lock-step from lines.tsv + Audio/NPC/*.ogg).
    ///
    /// Only ONE NPC speaks at a time (static _activeSpeaker lock) so overlapping
    /// proximity radii don't produce a cacophony — the dog gets a one-on-one
    /// conversation with whoever it walked up to. The speaker cycles through its
    /// repertoire line by line while the dog stays close, then releases + hides
    /// the subtitle when the dog leaves.
    ///
    /// Voice quality is NOT robovoice: Piper neural multi-voice (denis/dmitri/
    /// irina/ruslan), verified by whisper round-trip WER (docs/NPC_STATE.md).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class NpcVoice : MonoBehaviour
    {
        [Header("Repertoire (wired at build time, lock-step)")]
        public AudioClip[] clips;
        [TextArea] public string[] subtitles;

        [Header("Identity")]
        public string speakerName = "NPC";

        [Header("Proximity")]
        [Tooltip("Dog must be within this distance for the NPC to start talking.")]
        public float talkRadius = 3.2f;
        [Tooltip("Silence between consecutive lines while the dog stays close.")]
        public float gapBetweenLines = 0.7f;

        [Header("Target (the dog)")]
        public string targetTag = "Player";
        public string targetName = "Hero_Corgi";

        // Only one NPC holds the floor at a time.
        private static NpcVoice _activeSpeaker;

        private AudioSource _src;
        private Transform _target;
        private int _idx;
        private float _nextAllowed;
        private bool _showingSubtitle;

        private void Awake()
        {
            _src = GetComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 1f;             // full 3D — voice comes from the NPC
            _src.rolloffMode = AudioRolloffMode.Linear;
            if (_src.minDistance < 1.5f) _src.minDistance = 1.5f;
            if (_src.maxDistance < 14f) _src.maxDistance = 14f;
        }

        private void OnDisable()
        {
            if (_activeSpeaker == this) Release();
        }

        /// <summary>
        /// Pure decision (unit-testable, no audio device needed): may this NPC
        /// start its next line right now?
        /// </summary>
        public static bool ShouldStartLine(bool inRange, bool slotFreeOrMine, bool isPlaying, float now, float nextAllowed)
        {
            return inRange && slotFreeOrMine && !isPlaying && now >= nextAllowed;
        }

        private void FindTarget()
        {
            if (_target != null) return;
            GameObject go = null;
            if (!string.IsNullOrEmpty(targetTag))
            {
                try { go = GameObject.FindGameObjectWithTag(targetTag); } catch { /* tag may be undefined */ }
            }
            if (go == null && !string.IsNullOrEmpty(targetName)) go = GameObject.Find(targetName);
            if (go != null) _target = go.transform;
        }

        private void Update()
        {
            FindTarget();
            if (_target == null || clips == null || clips.Length == 0) return;

            float dist = Vector3.Distance(transform.position, _target.position);
            bool inRange = dist <= talkRadius;

            if (inRange)
            {
                bool slotFreeOrMine = _activeSpeaker == null || _activeSpeaker == this;
                if (_activeSpeaker == null && slotFreeOrMine && !_src.isPlaying && Time.time >= _nextAllowed)
                    _activeSpeaker = this;

                if (ShouldStartLine(inRange, _activeSpeaker == this, _src.isPlaying, Time.time, _nextAllowed))
                    SpeakNext();
            }
            else if (_activeSpeaker == this)
            {
                Release();
            }
        }

        private void SpeakNext()
        {
            var clip = clips[_idx % clips.Length];
            string sub = (subtitles != null && subtitles.Length > 0)
                ? subtitles[_idx % subtitles.Length]
                : null;
            _idx++;

            if (clip != null)
            {
                _src.clip = clip;
                _src.Play();
                _nextAllowed = Time.time + clip.length + gapBetweenLines;
            }
            else
            {
                _nextAllowed = Time.time + gapBetweenLines;
            }

            var dm = DialogueManager.Instance;
            if (dm != null && !dm.IsDialogueActive && !string.IsNullOrEmpty(sub))
            {
                dm.EmitLine($"{speakerName}: {sub}");
                _showingSubtitle = true;
            }
        }

        private void Release()
        {
            if (_activeSpeaker == this) _activeSpeaker = null;
            if (_showingSubtitle)
            {
                var dm = DialogueManager.Instance;
                // EndDialogue fires OnDialogueEnd → DialogueUI hides the panel.
                if (dm != null && !dm.IsDialogueActive) dm.EndDialogue();
                _showingSubtitle = false;
            }
        }
    }
}
