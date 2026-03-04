using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    public string levelName; // match a scene name
    public int wavesToWin;
    public int startingResources;
    public int startingLives;

    //public AudioClip backgroundMusic;
}
