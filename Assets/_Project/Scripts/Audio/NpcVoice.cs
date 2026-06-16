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
            var want = Quaternion.LookRotation(delta, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, faceSpeed * Time.deltaTime);
        }

        private void SpeakNext()
        {
            AudioClip clip = (clips != null && clips.Length > 0) ? clips[_idx % clips.Length] : null;
            string sub = (subtitles != null && subtitles.Length > 0) ? subtitles[_idx % subtitles.Length] : "";
            _idx++;

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
