using UnityEngine;

public class CameraAspectFitter : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;
    private const float DefaultOrthographicSize = 6f;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        AdjustOrthographicSize();
    }

    /// <summary>Adjusts orthographic size to ensure the full map width is always visible.</summary>
    private void AdjustOrthographicSize()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect < TargetAspect)
            _camera.orthographicSize = DefaultOrthographicSize * (TargetAspect / currentAspect);
        else
            _camera.orthographicSize = DefaultOrthographicSize;
    }
}
