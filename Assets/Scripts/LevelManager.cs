using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; } // singleton instance para isa lang ang LevelManager sa buong game

    public LevelData[] allLevels; // listahan ng lahat ng levels (galing sa inspector)
    public LevelData CurrentLevel { get; private set; } // yung kasalukuyang level na nilalaro (read-only sa ibang scripts)

    private void Awake()
    {
        if (Instance != null && Instance != this) // kung may existing instance na at hindi ito yun
        {
            Destroy(gameObject); // sirain yung duplicate na LevelManager
        }
        else // kung wala pang instance o ito yung una
        {
            Instance = this; // i-set yung instance sa current object
            DontDestroyOnLoad(gameObject); // huwag sirain pag nag-load ng bagong scene
        }
    }

    /// <summary>Sets the current level and loads its scene by name.</summary>
    public void LoadLevel(LevelData levelData)
    {
        if (levelData == null) // kung walang level data na binigay
        {
            Debug.LogError("LevelManager: Cannot load a null LevelData!"); // mag-error
            return; // wag mag-load
        }

        if (string.IsNullOrEmpty(levelData.levelName)) // kung walang level name (walang naka-assign na scene)
        {
            Debug.LogError($"LevelManager: LevelData '{levelData.name}' has no levelName set! Assign the scene name in the Inspector."); // mag-error
            return; // wag mag-load
        }

        CurrentLevel = levelData; // i-save yung current level
        SceneManager.LoadScene(levelData.levelName); // i-load yung scene base sa levelName
    }
}