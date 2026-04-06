using System.Collections;
using TMPro;
using UnityEngine;
using Afterhumans.Dialogue;
using Afterhumans.Player;

namespace Afterhumans.Scenes
{
    /// <summary>
    /// BOT-S01/S02: Scripted wake-up cinematic for Botanika scene.
    ///
    /// Replaces Timeline+Cinemachine approach (not feasible in batch mode)
    /// with a coroutine-driven camera pan that achieves the same 30-second
    /// test beats from STORY §3.1:
    ///
    /// 0:00-0:03  Camera low, looking at sun rays through ceiling (stillness)
    /// 0:03-0:05  Slow pan down toward Kafka on floor
    /// 0:05-0:07  Hold on Kafka (emotional anchor)
    /// 0:07-0:10  Pan up to note on coffee table
    /// 0:10-0:12  Note prompt appears ([E] прочитать)
    /// 0:12-0:15  Wide shot pan over Botanika (first-look) — BOT-S02
    /// 0:15-0:18  Camera settles to player FPS position
    /// 0:18+       Player controls enabled, tutorial overlay 5s
    ///
    /// Disables PlayerInteraction + SimpleFirstPersonController during
    /// cinematic. Re-enables after last beat. Self-destructs after.
    ///
    /// Skills: scroll-experience (opening beat pacing), game-design
    /// (30-second test), 3d-web-experience (camera choreography).
    /// </summary>
    public class BotanikaIntroDirector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Camera playerCamera;

        [Header("Timing")]
        [SerializeField] private float totalDuration = 18f;

        [Header("Tutorial")]
        [SerializeField] private float tutorialShowDuration = 5f;

        public bool IsPlaying { get; private set; }

        private MonoBehaviour _fpsController;
        private MonoBehaviour _playerInteraction;
        private bool _fpsWasEnabled;
        private bool _interactWasEnabled;
        private Vector3 _finalCamPos;
        private Quaternion _finalCamRot;

        private void Awake()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerCamera = player.GetComponentInChildren<Camera>();
                }
            }
            if (playerCamera == null) playerCamera = Camera.main;
        }

        private void Start()
        {
            if (playerCamera == null || playerTransform == null)
            {
                Debug.LogWarning("[BotanikaIntroDirector] Missing player/camera — skipping cinematic");
                enabled = false;
                return;
            }

            // QA Bug 3 fix: explicitly set final camera orientation facing into
            // the room (toward +Z, slightly down) instead of inheriting whatever
            // rotation the camera had at scene load — which often looks at skybox.
            // Camera settles at eye height above wherever the player stands.
            // Player may be at any Y (depends on BotanikaDresser spawn),
            // so we use player pos + eye offset.
            _finalCamPos = playerTransform.position + new Vector3(0f, 1.65f, 0f);
            _finalCamRot = Quaternion.Euler(15f, 0f, 0f);  // 15° down — see room clearly

            // Disable player controls during cinematic
            // Use generic GetComponent — string reflection fails with namespaced types
            _fpsController = playerTransform.GetComponent<SimpleFirstPersonController>();
            _playerInteraction = playerTransform.GetComponent<PlayerInteraction>();

            Debug.Log($"[IntroDirector] FPS controller found: {_fpsController != null}, PlayerInteraction found: {_playerInteraction != null}");
            Debug.Log($"[IntroDirector] Player pos: {playerTransform.position}, Camera pos: {playerCamera.transform.position}");

            _fpsWasEnabled = _fpsController != null && _fpsController.enabled;
            _interactWasEnabled = _playerInteraction != null && _playerInteraction.enabled;
            Debug.Log($"[IntroDirector] FPS was enabled: {_fpsWasEnabled}, Interact was enabled: {_interactWasEnabled}");
            if (_fpsController != null) _fpsController.enabled = false;
            if (_playerInteraction != null) _playerInteraction.enabled = false;

            // Lock cursor during cinematic
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            StartCoroutine(PlayCinematic());
        }

        private IEnumerator PlayCinematic()
        {
            IsPlaying = true;
            var cam = playerCamera.transform;

            // Beat 1: Camera looking up at glass ceiling / sun rays (0-3s)
            Vector3 startPos = _finalCamPos + new Vector3(0f, -0.5f, 0.3f);
            Quaternion lookUp = Quaternion.Euler(-30f, 0f, 0f);
            cam.position = startPos;
            cam.rotation = lookUp;

            yield return SmoothMove(cam, startPos, startPos + Vector3.up * 0.3f,
                lookUp, lookUp, 3f);

            // Beat 2: Pan down toward Kafka (3-5s)
            var kafka = GameObject.Find("Kafka");
            Vector3 kafkaLookTarget = kafka != null
                ? kafka.transform.position
                : _finalCamPos + new Vector3(1f, -0.8f, 0.5f);
            Quaternion lookAtKafka = Quaternion.LookRotation(
                kafkaLookTarget - cam.position, Vector3.up);

            yield return SmoothMove(cam, cam.position, cam.position,
                cam.rotation, lookAtKafka, 2f);

            // Beat 3: Hold on Kafka (5-7s)
            yield return new WaitForSeconds(2f);

            // Beat 4: Pan up to note on coffee table (7-10s)
            var note = GameObject.Find("Note");
            Vector3 noteLook = note != null
                ? note.transform.position
                : new Vector3(0.25f, 0.48f, 1.8f);
            Quaternion lookAtNote = Quaternion.LookRotation(
                noteLook - cam.position, Vector3.up);

            yield return SmoothMove(cam, cam.position, cam.position + Vector3.up * 0.4f,
                cam.rotation, lookAtNote, 3f);

            // Beat 5: Wide shot pan — first look at Botanika (10-15s)
            Vector3 widePos = new Vector3(0f, 3.0f, -3f);
            Quaternion wideLook = Quaternion.Euler(25f, 0f, 0f);

            yield return SmoothMove(cam, cam.position, widePos,
                cam.rotation, wideLook, 3f);
            yield return new WaitForSeconds(2f);

            // Beat 6: Settle to player FPS position (15-18s)
            yield return SmoothMove(cam, cam.position, _finalCamPos,
                cam.rotation, _finalCamRot, 3f);

            // mm-review fix: restore original enabled state, not unconditional true.
            // If another system (e.g. dialogue) disabled them during cinematic,
            // we don't blindly re-enable — respect the other system's lock.
            // Reset player body to face +Z (into room, toward Sasha)
            playerTransform.rotation = Quaternion.identity;

            Debug.Log($"[IntroDirector] Cinematic done. Re-enabling controls.");
            if (_fpsController != null)
            {
                _fpsController.enabled = _fpsWasEnabled;
                var fps = _fpsController as SimpleFirstPersonController;
                if (fps != null) fps.SetPitch(15f);
                Debug.Log($"[IntroDirector] FPS controller enabled={_fpsController.enabled}");
            }
            else
            {
                Debug.LogError("[IntroDirector] FPS CONTROLLER IS NULL — player will be frozen!");
            }
            if (_playerInteraction != null) _playerInteraction.enabled = _interactWasEnabled;

            Debug.Log($"[IntroDirector] Player final pos: {playerTransform.position}, Camera pos: {playerCamera.transform.position}, Camera rot: {playerCamera.transform.rotation.eulerAngles}");
            IsPlaying = false;

            // Show tutorial overlay
            yield return ShowTutorial();

            // Self-destruct
            Destroy(gameObject, 1f);
        }

        private IEnumerator SmoothMove(Transform t, Vector3 fromPos, Vector3 toPos,
            Quaternion fromRot, Quaternion toRot, float duration)
        {
            // mm-review fix: guard against zero duration → division by zero
            if (duration <= 0f)
            {
                t.position = toPos;
                t.rotation = toRot;
                yield break;
            }
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                t.position = Vector3.Lerp(fromPos, toPos, progress);
                t.rotation = Quaternion.Slerp(fromRot, toRot, progress);
                yield return null;
            }
            t.position = toPos;
            t.rotation = toRot;
        }

        private IEnumerator ShowTutorial()
        {
            // Create temporary tutorial overlay
            var go = new GameObject("TutorialOverlay");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;
            go.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var textGo = new GameObject("TutorialText");
            textGo.transform.SetParent(go.transform, false);
            var rect = textGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.25f, 0.15f);
            rect.anchorMax = new Vector2(0.75f, 0.30f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "WASD — ходить\nМышь — смотреть\nE — говорить\nShift — быстрее";
            tmp.fontSize = 24;
            tmp.color = new Color(0.9f, 0.85f, 0.7f, 0.85f);
            tmp.alignment = TextAlignmentOptions.Center;

            var group = textGo.AddComponent<CanvasGroup>();

            // Fade in
            float t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; group.alpha = t / 0.5f; yield return null; }
            group.alpha = 1f;

            yield return new WaitForSeconds(tutorialShowDuration);

            // Fade out
            t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; group.alpha = 1f - t / 0.5f; yield return null; }

            Destroy(go);
        }
    }
}
