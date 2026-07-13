using UnityEngine;

namespace Afterhumans.Art
{
    /// <summary>
    /// E-sprint (12 июл, приказ Тима после покадровой проверки — 5-кадровая серия с шагом 1с
    /// поймала то, что 2-кадровое сравнение пропустило): NpcArmStir/NpcFidget's per-frame
    /// shoulder oscillation is RETIRED for Кирилл/Стас — the ±55°/±56° swing on top of
    /// restPitchDeg still swept the arm back UP through horizontal (T-pose) at its own
    /// extremum every cycle («вдоль тела → горизонталь (T) → обратно»). Order: «плечи не
    /// трогает НИЧЕГО процедурное».
    ///
    /// This component ONLY fixes the STATIC bind pose ONCE in Awake() — the same restPitchDeg
    /// correction NpcFidget/NpcArmStir already proved (via judged screenshots) reads as a
    /// natural arms-at-the-side rest on this shared kirill_animated_raw rig. No LateUpdate, no
    /// oscillation, nothing moves after Awake(). Without this, simply removing the animation
    /// would regress the arms to the raw imported T-pose bind — the ORIGINAL bug this whole
    /// sprint started from — so this is a pose fix, not procedural animation.
    ///
    /// Kirill's old NpcArmStir only ever corrected R_Upperarm (his left arm was NEVER
    /// touched — likely why the T-pose RCA log noted "Кирилл = рука вбок"). This component
    /// corrects BOTH arms on both NPCs, reusing Stas's screenshot-verified 78° value
    /// symmetrically since both share identical rig geometry.
    /// </summary>
    public class NpcRestPose : MonoBehaviour
    {
        public float leftPitchDeg = 78f;
        public float rightPitchDeg = 78f;

        private Transform FindBone(string exact)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == exact) return t;
            return null;
        }

        private void Awake()
        {
            var lUpper = FindBone("L_Upperarm");
            var rUpper = FindBone("R_Upperarm");
            if (lUpper != null) lUpper.localRotation = lUpper.localRotation * Quaternion.Euler(0f, 0f, leftPitchDeg);
            if (rUpper != null) rUpper.localRotation = rUpper.localRotation * Quaternion.Euler(0f, 0f, -rightPitchDeg);
            if (lUpper == null && rUpper == null)
                Debug.LogWarning("[NpcRestPose] " + name + ": no L_Upperarm/R_Upperarm bone found — arms stay at raw bind pose.");
        }
    }
}
