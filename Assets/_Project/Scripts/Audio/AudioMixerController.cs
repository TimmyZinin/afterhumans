using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Afterhumans.Audio
{
    /// <summary>
    /// Global audio mixer controller. Fades ambient tracks between scenes,
    /// manages master/music/sfx/vo volumes via AudioMixer exposed parameters.
    /// Persistent across scenes via DontDestroyOnLoad.
    /// </summary>
    public class AudioMixerController : MonoBehaviour
    {
        public static AudioMixerController Instance { get; private set; }

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;

        [Header("Exposed Parameters (must match AudioMixer exposed params)")]
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SfxVolume";
        [SerializeField] private string voVolumeParam = "VoVolume";

        [Header("Music Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;

        [Header("Ambient Tracks")]
        [SerializeField] private AudioClip botanikaMusic;
        [SerializeField] private AudioClip cityMusic;
        [SerializeField] private AudioClip desertMusic;
        [SerializeField] private AudioClip creditsMusic;

        [Header("Crossfade")]
        [SerializeField] private float crossfadeDuration = 3f;

        private bool _useA = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AudioClip track = scene.name switch
            {
                "Scene_Botanika" => botanikaMusic,
                "Scene_City" => cityMusic,
                "Scene_Desert" => desertMusic,
                "Scene_Credits" => creditsMusic,
                _ => null,
            };

            if (track != null)
            {
                CrossfadeTo(track);
            }
            else
            {
                // Main menu or unknown — fade out music
                CrossfadeTo(null);
            }
        }

        public void CrossfadeTo(AudioClip newClip)
        {
            if (musicSourceA == null || musicSourceB == null) return;
            StartCoroutine(CrossfadeCoroutine(newClip));
        }

        private IEnumerator CrossfadeCoroutine(AudioClip newClip)
        {
            AudioSource from = _useA ? musicSourceA : musicSourceB;
            AudioSource to = _useA ? musicSourceB : musicSourceA;
            _useA = !_useA;

            if (newClip != null)
            {
                to.clip = newClip;
                to.loop = true;
                to.volume = 0f;
                to.Play();
            }

            float t = 0f;
            float fromStart = from != null ? from.volume : 0f;
            while (t < crossfadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / crossfadeDuration);
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                if (newClip != null && to != null) to.volume = Mathf.Lerp(0f, 1f, k);
                yield return null;
            }

            if (from != null)
            {
                from.volume = 0f;
                from.Stop();
            }
            if (newClip != null && to != null) to.volume = 1f;
        }

        public void SetMasterVolume(float linear01)  => SetMixerVolume(masterVolumeParam, linear01);
        public void SetMusicVolume(float linear01)   => SetMixerVolume(musicVolumeParam, linear01);
        public void SetSfxVolume(float linear01)     => SetMixerVolume(sfxVolumeParam, linear01);
        public void SetVoVolume(float linear01)      => SetMixerVolume(voVolumeParam, linear01);

        private void SetMixerVolume(string param, float linear01)
        {
            if (mixer == null || string.IsNullOrEmpty(param)) return;
            float db = linear01 > 0.0001f ? Mathf.Log10(linear01) * 20f : -80f;
            mixer.SetFloat(param, db);
        }
    }
}
