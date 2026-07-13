using UnityEngine;
using UnityEngine.SceneManagement;
using Afterhumans.Audio;

namespace Afterhumans.Scenes
{
    /// <summary>
    /// E-sprint (E1.4/E1.5): the door from Botanika to the City. Deliberately NOT named
    /// DoorToCity/SceneExitTrigger — those types are actively destroyed on every build by
    /// BotanikaBuilder.StripInkDialogueInfra() (the Sprint D WebGL-freeze fix strips the old
    /// Ink door path). This is a fresh, Ink-free component: opens visually (two leaves swing
    /// + glow light) once NpcProgressTracker.DoorUnlocked flips true, then fades and loads the
    /// target scene when the player walks through. Falls back to a locked voice line otherwise.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CityDoorGate : MonoBehaviour
    {
        [Header("Visual (wired by BotanikaBuilder.EnsureCityDoor)")]
        public Transform leafL;
        public Transform leafR;
        public float openAngleL = -100f;
        public float openAngleR = 100f;
        public float swingSpeedDeg = 70f;
        public Light glowLight;
        public GameObject glowPanel;
        public float glowIntensity = 2.5f;

        [Header("Transition")]
        public string targetScene = "Scene_City";
        [TextArea] public string lockedMessage = "Не сейчас. Ты ещё не узнал, что снаружи.";
        public float lockedCooldown = 3f;

        private bool _open;
        private bool _fired;
        private float _lockedUntil;
        private Quaternion _closedL, _closedR, _targetL, _targetR;

        private void Awake()
        {
            if (leafL != null) { _closedL = leafL.localRotation; _targetL = Quaternion.Euler(0f, openAngleL, 0f); }
            if (leafR != null) { _closedR = leafR.localRotation; _targetR = Quaternion.Euler(0f, openAngleR, 0f); }
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            if (!_open && NpcProgressTracker.DoorUnlocked)
            {
                _open = true;
                if (glowLight != null) glowLight.enabled = true;
                if (glowPanel != null) glowPanel.SetActive(true);
                Debug.Log("[CityDoorGate] unlocked — opening");
            }

            if (!_open) return;

            if (leafL != null) leafL.localRotation = Quaternion.RotateTowards(leafL.localRotation, _targetL, swingSpeedDeg * Time.deltaTime);
            if (leafR != null) leafR.localRotation = Quaternion.RotateTowards(leafR.localRotation, _targetR, swingSpeedDeg * Time.deltaTime);
            if (glowLight != null && glowLight.intensity < glowIntensity)
                glowLight.intensity = Mathf.MoveTowards(glowLight.intensity, glowIntensity, 4f * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other) => TryEnter(other);
        private void OnTriggerStay(Collider other) { if (!_fired && Time.time >= _lockedUntil) TryEnter(other); }

        private void TryEnter(Collider other)
        {
            if (_fired) return;
            if (!other.CompareTag("Player")) return;

            if (!_open)
            {
                if (Time.time < _lockedUntil) return;
                _lockedUntil = Time.time + lockedCooldown;
                // E-sprint (12 июл, P0-3): Tim's #5d playtest — visited all 4 NPCs, door stayed
                // locked, no indication why. Diagnostic log pairs with NpcProgressTracker's own
                // (IL-3: log first, don't guess-fix) — a live console capture on the NEXT locked
                // attempt shows exactly what Met/AllFourMet/DoorUnlocked look like at that moment.
                Debug.Log($"[ProgressDiag] Door LOCKED attempt — DoorUnlocked={NpcProgressTracker.DoorUnlocked} AllFourMet={NpcProgressTracker.AllFourMet}");
                // Concrete progress instead of a vague "не сейчас": name who's still unmet, or —
                // if all 4 are met but Nikolai hasn't spoken his gate line yet — point at him
                // specifically, so a "4/4 but door still locked" state is legible to the player
                // even if OnGateLineSpoken's own trigger condition turns out to need a real fix.
                string msg = NpcProgressTracker.AllFourMet
                    ? "Кажется, стоит ещё раз поговорить с Николаем."
                    : NpcProgressTracker.UnmetSummary();
                if (string.IsNullOrEmpty(msg)) msg = lockedMessage;
                NpcDialogueHud.Get().Show("", msg, 3f);
                return;
            }

            _fired = true;
            Debug.Log("[CityDoorGate] player entered — loading " + targetScene);
            var transition = SceneTransition.Instance;
            if (transition != null) transition.LoadScene(targetScene);
            else SceneManager.LoadScene(targetScene);
        }
    }
}
