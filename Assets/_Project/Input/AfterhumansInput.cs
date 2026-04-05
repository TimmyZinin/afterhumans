// ---------------------------------------------------------------------
// BOT-F04: Hand-written C# wrapper equivalent to Unity's "Generate C# Class"
// output for AfterhumansInput.inputactions. Avoids batchmode asset post-
// processing bugs where generateWrapperCode flag isn't honored.
// ---------------------------------------------------------------------
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterhumans.InputSystems
{
    /// <summary>
    /// Static helper wrapping a single runtime instance of AfterhumansInput
    /// action asset. Use via <c>AfterhumansInputWrapper.Instance.Gameplay</c>.
    /// Lazy-loads the asset from Resources on first access.
    ///
    /// Skill `game-development` §3: actions, not raw keys. Keeps legacy Input
    /// fallback working (ProjectSettings activeInputHandler = Both) while new
    /// systems migrate gradually. Designed to co-exist with Input.GetKeyDown
    /// calls in SimpleFirstPersonController during transition.
    /// </summary>
    public class AfterhumansInputWrapper
    {
        private static AfterhumansInputWrapper _instance;
        public static AfterhumansInputWrapper Instance
        {
            get
            {
                if (_instance == null) _instance = new AfterhumansInputWrapper();
                return _instance;
            }
        }

        public InputActionAsset Asset { get; private set; }
        public InputActionMap Gameplay { get; private set; }
        public InputActionMap Dialogue { get; private set; }

        // Individual actions cached for fast access
        public InputAction Move { get; private set; }
        public InputAction Look { get; private set; }
        public InputAction Interact { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Pause { get; private set; }
        public InputAction DialogueContinue { get; private set; }
        public InputAction DialogueSkipChoice { get; private set; }

        private AfterhumansInputWrapper()
        {
            Asset = Resources.Load<InputActionAsset>("AfterhumansInput");
            if (Asset == null)
            {
                // Fallback: load from assets via AssetDatabase would require editor.
                // In runtime without Resources: skip binding, all actions null.
                // Consumer code must null-check before Enable().
                Debug.LogWarning("[AfterhumansInput] InputActionAsset not found in Resources/. Place at Resources/AfterhumansInput.asset or migrate code to manual binding.");
                return;
            }

            Gameplay = Asset.FindActionMap("Gameplay", throwIfNotFound: false);
            Dialogue = Asset.FindActionMap("Dialogue", throwIfNotFound: false);

            if (Gameplay != null)
            {
                Move = Gameplay.FindAction("Move");
                Look = Gameplay.FindAction("Look");
                Interact = Gameplay.FindAction("Interact");
                Sprint = Gameplay.FindAction("Sprint");
                Pause = Gameplay.FindAction("Pause");
            }

            if (Dialogue != null)
            {
                DialogueContinue = Dialogue.FindAction("Continue");
                DialogueSkipChoice = Dialogue.FindAction("SkipChoice");
            }
        }

        public void EnableGameplay()
        {
            Gameplay?.Enable();
        }

        public void EnableDialogue()
        {
            Dialogue?.Enable();
        }

        public void DisableAll()
        {
            Gameplay?.Disable();
            Dialogue?.Disable();
        }
    }
}
