using System;
using System.IO;
using UnityEngine;

namespace Afterhumans.Managers
{
    /// <summary>
    /// Global game state singleton: current scene, cursor input result, save/load.
    /// Persistent across scenes, minimal save to persistentDataPath/afterhumans_save.json.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Serializable]
        public class SaveData
        {
            public string currentScene = "Scene_Botanika";
            public int cursorInput = 0; // 0=empty, 1=help, 2=enough, 3=unknown, 4=i, 5=continue
            public string timestamp;
        }

        public SaveData data = new SaveData();

        private string SavePath => Path.Combine(Application.persistentDataPath, "afterhumans_save.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();
        }

        public void SaveGame()
        {
            try
            {
                data.timestamp = DateTime.UtcNow.ToString("O");
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[GameStateManager] Saved to {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameStateManager] Save failed: {e.Message}");
            }
        }

        public void LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                data = new SaveData();
                return;
            }
            try
            {
                string json = File.ReadAllText(SavePath);
                data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                Debug.Log($"[GameStateManager] Loaded from {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameStateManager] Load failed: {e.Message}");
                data = new SaveData();
            }
        }

        public void ResetGame()
        {
            data = new SaveData();
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }

        public bool HasSave => File.Exists(SavePath);
    }
}
