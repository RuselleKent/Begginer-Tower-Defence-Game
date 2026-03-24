using UnityEngine;

[RequireComponent(typeof(Camera))] // kailangan ng Camera component para gumana to
public class CameraAspectFitter : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f; // target aspect ratio (16:9) na dapat i-maintain

    private Camera _camera; // yung camera na ia-adjust
    private float _baseOrthographicSize; // original size ng camera (galing sa inspector)
    private int _lastScreenWidth; // huling screen width na na-process (para malaman kung nagbago)
    private int _lastScreenHeight; // huling screen height na na-process

    private void Awake()
    {
        _camera = GetComponent<Camera>(); // kunin yung camera component
        // Capture whatever size is set in the Inspector as the baseline
        _baseOrthographicSize = _camera.orthographicSize; // i-save yung original orthographic size na naka-set sa inspector
    }

    private void Start()
    {
        Apply(); // i-apply yung adjustment sa start
    }

    private void Update()
    {
        // Re-apply whenever the browser window is resized
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight) // kung nagbago yung screen size (na-resize yung window)
            Apply(); // i-apply ulit yung adjustment
    }

    /// <summary>Adjusts orthographic size so the full map always fills the screen without cropping.</summary>
    private void Apply()
    {
        _lastScreenWidth  = Screen.width; // i-save yung current width
        _lastScreenHeight = Screen.height; // i-save yung current height

        float currentAspect = (float)Screen.width / Screen.height; // compute yung current aspect ratio ng screen
        float scaleHeight   = currentAspect / TargetAspect; // compute kung gaano kaiba yung aspect ratio sa target

        // If screen is taller than 16:9, zoom out so nothing is cropped vertically
        _camera.orthographicSize = scaleHeight < 1f // kung mas matangkad yung screen kesa sa 16:9 (mas maliit yung scaleHeight)
            ? _baseOrthographicSize / scaleHeight // i-zoom out (palakihin yung orthographic size) para hindi ma-crop
            : _baseOrthographicSize; // kung mas lapad or same lang, gamitin yung original size
    }
}