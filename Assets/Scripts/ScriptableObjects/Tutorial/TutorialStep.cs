using UnityEngine;

[CreateAssetMenu(fileName = "TutorialStep", menuName = "Scriptable Objects/Tutorial/TutorialStep")]
public class TutorialStep : ScriptableObject
{
    [TextArea(3, 6)]
    public string message;

    [Tooltip("Optional sprite shown alongside the message (e.g. an arrow or icon).")]
    public Sprite illustration;
}
