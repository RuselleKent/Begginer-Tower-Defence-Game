using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainButtonGroup;
    [SerializeField] private GameObject levelSelectPanel;

    /// <summary>Shows the level selection panel and hides the main buttons.</summary>
    public void OpenLevelSelect()
    {
        mainButtonGroup.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    /// <summary>Hides the level selection panel and restores the main buttons.</summary>
    public void CloseLevelSelect()
    {
        levelSelectPanel.SetActive(false);
        mainButtonGroup.SetActive(true);
    }

    /// <summary>Loads the level at the given index from LevelManager's allLevels array.</summary>
    public void SelectLevel(int index)
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("MainMenuController: LevelManager.Instance is null!");
            return;
        }

        if (LevelManager.Instance.allLevels == null || index >= LevelManager.Instance.allLevels.Length)
        {
            Debug.LogError($"MainMenuController: Level index {index} is out of range!");
            return;
        }

        LevelManager.Instance.LoadLevel(LevelManager.Instance.allLevels[index]);
    }

    public void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
