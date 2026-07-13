using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterhumans.Kafka
{
    /// <summary>
    /// E-sprint (E1.5): carries Hero_Corgi (+ its KafkaDirectController-driven camera) across
    /// the Botanika→City scene load (GDD §8: "Кафка → persistent объект с DontDestroyOnLoad").
    /// DontDestroyOnLoad is play-mode-only — guarded so BotanikaBuilder can still add this
    /// component at edit time without an edit-mode error (see Awake).
    ///
    /// On every scene load it repositions to SpawnPoint_FromBotanika (if the new scene has
    /// one) and tells KafkaDirectController to re-acquire Camera.main — the OLD scene's camera
    /// is destroyed on unload, and the controller's cached reference would otherwise stay null
    /// forever (its own re-init guard only ever runs once).
    /// </summary>
    [RequireComponent(typeof(KafkaDirectController))]
    public class PersistentPlayer : MonoBehaviour
    {
        private static PersistentPlayer _instance;
        private KafkaDirectController _controller;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _controller = GetComponent<KafkaDirectController>();

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var marker = GameObject.Find("SpawnPoint_FromBotanika");
            if (marker != null)
            {
                var cc = GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false; // avoid the CC fighting a hard teleport
                transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
                if (cc != null) cc.enabled = true;
                Debug.Log($"[PersistentPlayer] repositioned to SpawnPoint_FromBotanika in '{scene.name}'");
            }

            if (_controller != null) _controller.ReacquireCamera();
        }
    }
}
