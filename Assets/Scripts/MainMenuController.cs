using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainButtonGroup; // yung grupo ng mga main buttons (Start, Quit, etc.)
    [SerializeField] private GameObject levelSelectPanel; // yung panel na nagpapakita ng listahan ng levels

    /// <summary>Shows the level selection panel and hides the main buttons.</summary>
    public void OpenLevelSelect()
    {
        mainButtonGroup.SetActive(false); // itago yung main buttons
        levelSelectPanel.SetActive(true); // ipakita yung level selection panel
    }

    /// <summary>Hides the level selection panel and restores the main buttons.</summary>
    public void CloseLevelSelect()
    {
        levelSelectPanel.SetActive(false); // itago yung level selection panel
        mainButtonGroup.SetActive(true); // ipakita ulit yung main buttons
    }

    /// <summary>Loads the level at the given index from LevelManager's allLevels array.</summary>
    public void SelectLevel(int index)
    {
        if (LevelManager.Instance == null) // kung walang LevelManager
        {
            Debug.LogError("MainMenuController: LevelManager.Instance is null!"); // mag-error
            return; // wag mag-load
        }

        if (LevelManager.Instance.allLevels == null // kung walang levels
            || index < 0 // o negative yung index
            || index >= LevelManager.Instance.allLevels.Length) // o lagpas sa dami ng levels
        {
            Debug.LogError($"MainMenuController: Level index {index} is out of range!"); // mag-error
            return; // wag mag-load
        }

        LevelManager.Instance.LoadLevel(LevelManager.Instance.allLevels[index]); // i-load yung level base sa index
    }

    /// <summary>Quits the application or exits play mode in the Editor.</summary>
    public void QuitGame()
    {
        Application.Quit(); // i-quit yung application (sa build)

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // kung nasa editor, i-stop yung play mode
#endif
    }
}