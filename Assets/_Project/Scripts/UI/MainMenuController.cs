using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Afterhumans.Scenes;
using Afterhumans.Managers;

namespace Afterhumans.UI
{
    /// <summary>
    /// Main menu for Episode 0. Three buttons: Start, Continue (if save exists), Quit.
    /// Minimalist, Dune-inspired palette (orange/purple sunset).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;

        [Header("Scene Names")]
        [SerializeField] private string firstScene = "Scene_Botanika";

        private void Start()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStart);
            }

            if (continueButton != null)
            {
                bool hasSave = GameStateManager.Instance != null && GameStateManager.Instance.HasSave;
                continueButton.gameObject.SetActive(hasSave);
                continueButton.onClick.AddListener(OnContinue);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuit);
            }
        }

        private void OnStart()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ResetGame();
            }
            LoadGameScene(firstScene);
        }

        private void OnContinue()
        {
            if (GameStateManager.Instance != null && !string.IsNullOrEmpty(GameStateManager.Instance.data.currentScene))
            {
                LoadGameScene(GameStateManager.Instance.data.currentScene);
            }
            else
            {
                LoadGameScene(firstScene);
            }
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadGameScene(string sceneName)
        {
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
