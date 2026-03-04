using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void StartNewGame()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("MainMenuController: LevelManager.Instance is null! Make sure LevelManager exists in the scene.");
            return;
        }

        if (LevelManager.Instance.allLevels == null || LevelManager.Instance.allLevels.Length == 0)
        {
            Debug.LogError("MainMenuController: No levels assigned to LevelManager!");
            return;
        }

        LevelManager.Instance.LoadLevel(LevelManager.Instance.allLevels[0]);
    }

    public void QuitGame()
    {
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
