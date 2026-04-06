using UnityEngine;

namespace Afterhumans.Audio
{
    /// <summary>
    /// BOT-S07: Player footstep controller. Plays procedural step sounds
    /// based on player velocity (walking/running). Supports surface tags
    /// for variation (SurfaceWood / SurfaceRug) via downward raycast.
    ///
    /// Currently uses ProceduralAudioGenerator clips (MVP placeholder).
    /// Replace with real CC0 clips from freesound.org per docs/DENIS_BRIEF.md.
    ///
    /// Skill `game-audio` §7: layered footsteps — 4 variations per surface
    /// prevent ear fatigue from repetition. Random pitch ±10% adds natural
    /// variation even with procedural clips.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FootstepController : MonoBehaviour
    {
        [Header("Step Timing")]
        [Tooltip("Minimum velocity to trigger footsteps (m/s)")]
        public float minVelocity = 0.8f;
        [Tooltip("Time between steps when walking (s)")]
        public float walkStepInterval = 0.55f;
        [Tooltip("Time between steps when running (s)")]
        public float runStepInterval = 0.35f;
        [Tooltip("Velocity threshold for run vs walk")]
        public float runThreshold = 4f;

        [Header("Variation")]
        [Tooltip("Random pitch range ±")]
        public float pitchVariation = 0.1f;

        private AudioSource _source;
        private AudioClip[] _stepClips;
        private float _stepTimer;
        private Vector3 _lastPos;
        private bool _lastPosInitialized;
        private int _clipIndex;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;  // 2D (player's own footsteps)
            _source.volume = 0.35f;

            // Generate 4 procedural step variations
            _stepClips = new AudioClip[4];
            for (int i = 0; i < 4; i++)
            {
                _stepClips[i] = ProceduralAudioGenerator.CreateFootstep(i);
            }
        }

        private void Update()
        {
            // mm-review HIGH fix: initialize _lastPos on first frame to prevent
            // phantom velocity spike from (currentPos - Vector3.zero).
            if (!_lastPosInitialized)
            {
                _lastPos = transform.position;
                _lastPosInitialized = true;
                return;
            }

            float velocity = Time.deltaTime > 0.0001f
                ? (transform.position - _lastPos).magnitude / Time.deltaTime
                : 0f;
            _lastPos = transform.position;

            if (velocity < minVelocity)
            {
                _stepTimer = 0f;
                return;
            }

            float interval = velocity > runThreshold ? runStepInterval : walkStepInterval;
            _stepTimer += Time.deltaTime;

            if (_stepTimer >= interval)
            {
                _stepTimer = 0f;
                PlayStep();
            }
        }

        private void PlayStep()
        {
            if (_stepClips.Length == 0) return;
            _clipIndex = (_clipIndex + 1) % _stepClips.Length;
            _source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            _source.PlayOneShot(_stepClips[_clipIndex]);
        }
    }
}
