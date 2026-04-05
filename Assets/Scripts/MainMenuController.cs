using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainButtonGroup;
    [SerializeField] private GameObject levelSelectPanel;

    /// <summary>Shows the level selection panel and hides the main buttons.</summary>
    public void OpenLevelSelect()
    {
        AudioManager.Instance?.PlayButtonClick();
        if (mainButtonGroup != null) mainButtonGroup.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
    }

    /// <summary>Hides the level selection panel and restores the main buttons.</summary>
    public void CloseLevelSelect()
    {
        AudioManager.Instance?.PlayButtonClick();
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (mainButtonGroup != null) mainButtonGroup.SetActive(true);
    }

    /// <summary>Opens the settings panel via singleton — safe across scene reloads.</summary>
    public void OpenSettings()
    {
        AudioManager.Instance?.PlayButtonClick();
        SettingsPanel.Instance?.OpenPanel();
    }

    /// <summary>Loads the level at the given index from LevelManager's allLevels array.</summary>
    public void SelectLevel(int index)
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("MainMenuController: LevelManager.Instance is null!");
            return;
        }

        if (LevelManager.Instance.allLevels == null
            || index < 0
            || index >= LevelManager.Instance.allLevels.Length)
        {
            Debug.LogError($"MainMenuController: Level index {index} is out of range!");
            return;
        }

        AudioManager.Instance?.PlayButtonClick();
        LevelManager.Instance.LoadLevel(LevelManager.Instance.allLevels[index]);
    }

    /// <summary>Quits the application or exits play mode in the Editor.</summary>
    public void QuitGame()
    {
        AudioManager.Instance?.PlayButtonClick();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
