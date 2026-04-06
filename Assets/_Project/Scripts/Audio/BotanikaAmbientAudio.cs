using UnityEngine;

namespace Afterhumans.Audio
{
    /// <summary>
    /// BOT-S06: Scene ambient audio controller for Botanika.
    ///
    /// Plays a looping ambient drone (procedural MVP placeholder) through
    /// a 2D AudioSource. Real CC0 lofi music will replace the procedural
    /// drone when audio assets arrive (docs/DENIS_BRIEF.md).
    ///
    /// Also spawns a 3D AudioSource on the coffee machine area for
    /// spatial coffee drip SFX hint.
    ///
    /// Self-contained: creates its own clips via ProceduralAudioGenerator.
    /// Attached to a scene root by BotanikaNpcPopulator.
    /// </summary>
    public class BotanikaAmbientAudio : MonoBehaviour
    {
        [SerializeField] private float musicVolume = 0.12f;

        private AudioSource _musicSource;

        private void Start()
        {
            // Music loop (2D, quiet background)
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.clip = ProceduralAudioGenerator.CreateAmbientDrone(10f);
            _musicSource.loop = true;
            _musicSource.volume = musicVolume;
            _musicSource.spatialBlend = 0f;
            _musicSource.playOnAwake = false;
            _musicSource.Play();
        }
    }
}
