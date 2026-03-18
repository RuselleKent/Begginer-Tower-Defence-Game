using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    public string levelName;
    public int wavesToWin;
    public int startingResources;
    public int startingLives;

    [Tooltip("Tutorial steps shown before the countdown. Leave empty for no tutorial.")]
    public TutorialStep[] tutorialSteps;
}
