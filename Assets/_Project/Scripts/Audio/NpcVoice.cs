using UnityEngine;

namespace Afterhumans.Audio
{
    /// <summary>
    /// BOT-N10 (rev2): Proximity voice. Dog (player) approaches → the NPC speaks its
    /// own pre-recorded Russian line on a 3D AudioSource AND shows a subtitle in the
    /// self-contained NpcDialogueHud. No key press, NO Ink (the Ink path froze the
    /// game on E — removed). Tim's requirement: «собака подходит → NPC с ней говорит».
    ///
    /// Only ONE NPC speaks at a time (static _active lock). Cycles its lines while the
    /// dog stays close, releases + hides the subtitle when the dog leaves. Freeze-proof:
    /// no recursion, no Ink, no blocking loops.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class NpcVoice : MonoBehaviour
    {
        public AudioClip[] clips;
        [TextArea] public string[] subtitles;
        public string speakerName = "NPC";
        public float talkRadius = 3.4f;
        public float gapBetweenLines = 0.6f;
        public string targetTag = "Player";
        public string targetName = "Hero_Corgi";
        public bool faceTargetWhileTalking = true;
        public float faceSpeed = 90f;   // deg/sec, yaw only

        // E-sprint (12 июл, P0-2: "NPC говорят затылком к собаке" — live playtest of #5d,
        // Sasha screenshot). Root cause per-NPC, MEASURED not guessed: BotanikaCameraProbe.
        // DiagnoseNpcFacing shot each wired NPC's head from 0/90/180/270° around its own
        // transform.forward; the angle where the CAMERA (placed at that angle, looking back)
        // saw the actual FACE tells you how far the true "front" is rotated from
        // transform.forward. Team-lead's hypothesis was "180° for the new Tripo rigs" — the
        // measurement showed the 4 new Tripo deci-rigs (kirill/mila/nikolai/stas) are ALL at
        // 270° (not 180°), while Sasha's old sasha_anim.fbx rig is at 180° as suspected. Set
        // per-NPC by BotanikaBuilder.WireBotanikaNpcs from the measured values — do NOT set
        // this from a guess; re-run DiagnoseNpcFacing after any rig swap.
        public float faceYawOffsetDeg = 0f;

        // E-sprint (BOT-E-progression): once gateActive() returns true, SpeakNext pulls from
        // this separate line set instead of the normal cycle — used for Nikolai's finale
        // ("iди, там снаружи, возможно, твой хозяин") after the player has met all 4 other
        // NPCs. Kept as a SEPARATE array (not merged into clips/subtitles) so the normal
        // cycle is untouched and the gate lines never play early.
        [Header("Gate lines (optional)")]
        public AudioClip[] gateClips;
        [TextArea] public string[] gateSubtitles;
        public System.Func<bool> gateActive;
        /// <summary>True once at least one gate line has actually been spoken aloud.</summary>
        public bool GateLineSpoken { get; private set; }
        private int _gateIdx;

        private static NpcVoice _active;
        private AudioSource _src;
        private Transform _target;
        private int _idx;
        private float _nextAllowed;
        private bool _showing;
        private bool _faceNow;

        private void Awake()
        {
            _src = GetComponent<AudioSource>();
            _src.playOnAwake = false;
            _src.spatialBlend = 1f;
            _src.rolloffMode = AudioRolloffMode.Linear;
            if (_src.minDistance < 1.5f) _src.minDistance = 1.5f;
            if (_src.maxDistance < 16f) _src.maxDistance = 16f;
        }

        private void OnDisable() { if (_active == this) Release(); }

        private void FindTarget()
        {
            // Unity fake-null: a destroyed Transform compares == null, so a recreated dog
            // is re-acquired automatically; this guard just avoids re-searching every frame.
            if (_target != null && _target.gameObject != null) return;
            _target = null;
            GameObject go = null;
            // BIND TO THE REAL DOG, not the stale FPS "Player". The scene still contains a
            // leftover first-person "Player" GameObject at z=-12 (only disabled, never untagged),
            // so FindGameObjectWithTag("Player") could return THAT stationary object — then every
            // NPC measured the dog as 9-15 m away and NOBODY ever spoke (Tim: «подхожу, NPC молчит»).
            // Resolve by name (Hero_Corgi) and the KafkaDirectController component FIRST; fall back
            // to the tag only if those fail.
            if (!string.IsNullOrEmpty(targetName)) go = GameObject.Find(targetName);
            if (go == null)
            {
                var kdc = Object.FindFirstObjectByType<Afterhumans.Kafka.KafkaDirectController>();
                if (kdc != null) go = kdc.gameObject;
            }
            if (go == null && !string.IsNullOrEmpty(targetTag))
            {
                try { go = GameObject.FindGameObjectWithTag(targetTag); } catch { }
            }
            if (go != null) _target = go.transform;
        }

        private void Update()
        {
            FindTarget();
            _faceNow = false;
            // Proceed if we have a voice clip OR at least a subtitle — so the dialogue window
            // ALWAYS appears even if a clip failed to load in WebGL (Tim: окно должно появляться).
            bool hasContent = (clips != null && clips.Length > 0) || (subtitles != null && subtitles.Length > 0);
            if (_target == null || !hasContent) return;

            float d = Vector3.Distance(transform.position, _target.position);
            if (d <= talkRadius)
            {
                if (_active == null && !_src.isPlaying && Time.time >= _nextAllowed) _active = this;
                if (_active == this && !_src.isPlaying && Time.time >= _nextAllowed) SpeakNext();
                if (_active == this && faceTargetWhileTalking) _faceNow = true;  // applied in LateUpdate
            }
            else if (_active == this)
            {
                Release();
            }
        }

        // Face the dog in LateUpdate so it wins AFTER NpcIdleBob/NpcWalk rotate in Update —
        // otherwise the talking NPC jitters between facing the dog and its idle/walk pose.
        private void LateUpdate()
        {
            if (!_faceNow || _target == null) return;
            var delta = _target.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0004f) return;
            // faceYawOffsetDeg compensates a per-rig mismatch between transform.forward and
            // the mesh's actual visual front (see field doc above): rotate the LookRotation
            // result by -offset so the MEASURED true-face direction ends up pointing at the
            // target, not raw transform.forward.
            var want = Quaternion.Euler(0f, -faceYawOffsetDeg, 0f) * Quaternion.LookRotation(delta, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, faceSpeed * Time.deltaTime);
        }

        private void SpeakNext()
        {
            // E-sprint gate: once the 4-NPC progression is complete, this NPC (Nikolai) speaks
            // its finale line set instead of the normal cycle. Checked first so the gate always
            // wins the moment it turns true, even mid-cycle.
            bool useGate = gateActive != null && gateActive() && gateClips != null && gateSubtitles != null
                           && gateSubtitles.Length > 0;

            AudioClip clip;
            string sub;
            if (useGate)
            {
                clip = (gateClips.Length > 0) ? gateClips[_gateIdx % gateClips.Length] : null;
                sub = gateSubtitles[_gateIdx % gateSubtitles.Length];
                _gateIdx++;
                GateLineSpoken = true;
                NpcProgressTracker.OnGateLineSpoken(gameObject.name);
            }
            else
            {
                clip = (clips != null && clips.Length > 0) ? clips[_idx % clips.Length] : null;
                sub = (subtitles != null && subtitles.Length > 0) ? subtitles[_idx % subtitles.Length] : "";
                _idx++;
                NpcProgressTracker.OnSpoken(gameObject.name);
            }
            // E-sprint (12 июл, P0-3 diagnostic, pairs with NpcProgressTracker's own log): which
            // GameObject actually voiced which line, and via which path (gate vs normal cycle) —
            // this is the piece that would catch "wrong NPC said the door line" (e.g. a stale
            // duplicate NPC_<id> object left over from an earlier wire pass, still holding old
            // gate data) since it names the exact instance, not just the logical id.
            Debug.Log($"[VoiceDiag] {gameObject.name} (speaker={speakerName}) useGate={useGate} sub=\"{sub}\"");

            float hold = 3.5f;
            if (clip != null)
            {
                _src.clip = clip;
                _src.Play();
                _nextAllowed = Time.time + clip.length + gapBetweenLines;
                hold = clip.length + 1.5f;
            }
            else
            {
                // subtitle-only fallback (clip missing / WebGL decode fail): still show the line,
                // pace by a readable minimum so the window doesn't flicker.
                _nextAllowed = Time.time + Mathf.Max(2.5f, gapBetweenLines);
            }

            NpcDialogueHud.Get().Show(speakerName, sub, hold);
            _showing = true;
        }

        private void Release()
        {
            if (_active == this) _active = null;
            if (_showing) { NpcDialogueHud.Get().Hide(); _showing = false; }
            // Stop this NPC's clip so its voice doesn't bleed into the next NPC the dog visits.
            if (_src != null && _src.isPlaying) _src.Stop();
        }

        public float TalkRadius => talkRadius;

        /// <summary>
        /// Explicit trigger (E-press via NpcInteractor): speak immediately, ignoring the proximity
        /// timer, and grab the single-speaker lock so the voice clip + subtitle always fire. Tim
        /// presses E to talk, so this guarantees a response even if auto-proximity hasn't kicked in.
        /// </summary>
        public void ForceSpeak()
        {
            bool hasContent = (clips != null && clips.Length > 0) || (subtitles != null && subtitles.Length > 0);
            // E-sprint diagnostic (12 июл, paired with NpcInteractor's candidate-list log):
            // confirms whether NpcInteractor picked THIS NpcVoice correctly but it silently
            // no-op'd on empty content, vs. picking the wrong NpcVoice in the first place.
            Debug.Log($"[NpcVoice.ForceSpeak] {gameObject.name} speaker={speakerName} hasContent={hasContent} clipsLen={(clips == null ? -1 : clips.Length)} subsLen={(subtitles == null ? -1 : subtitles.Length)}");
            if (!hasContent) { Debug.LogWarning($"[NpcVoice] {speakerName}: no clips/subtitles — skip ForceSpeak"); return; }
            FindTarget();
            if (_src == null) _src = GetComponent<AudioSource>();
            if (_active != null && _active != this) _active.Release();
            _active = this;
            _nextAllowed = 0f;
            SpeakNext();
        }
    }
}
