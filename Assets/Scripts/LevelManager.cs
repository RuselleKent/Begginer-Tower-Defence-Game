using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public LevelData[] allLevels;
    public LevelData CurrentLevel { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>Sets the current level and loads its scene by name.</summary>
    public void LoadLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("LevelManager: Cannot load a null LevelData!");
            return;
        }

        if (string.IsNullOrEmpty(levelData.levelName))
        {
            Debug.LogError($"LevelManager: LevelData '{levelData.name}' has no levelName set! Assign the scene name in the Inspector.");
            return;
        }

        CurrentLevel = levelData;
        SceneManager.LoadScene(levelData.levelName);
    }
}
