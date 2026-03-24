using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectFitter : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;

    private Camera _camera;
    private float _baseOrthographicSize;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        // Capture whatever size is set in the Inspector as the baseline
        _baseOrthographicSize = _camera.orthographicSize;
    }

    private void Start()
    {
        Apply();
    }

    private void Update()
    {
        // Re-apply whenever the browser window is resized
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            Apply();
    }

    /// <summary>Adjusts orthographic size so the full map always fills the screen without cropping.</summary>
    private void Apply()
    {
        _lastScreenWidth  = Screen.width;
        _lastScreenHeight = Screen.height;

        float currentAspect = (float)Screen.width / Screen.height;
        float scaleHeight   = currentAspect / TargetAspect;

        // If screen is taller than 16:9, zoom out so nothing is cropped vertically
        _camera.orthographicSize = scaleHeight < 1f
            ? _baseOrthographicSize / scaleHeight
            : _baseOrthographicSize;
    }
}
