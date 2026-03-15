using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RangeIndicator : MonoBehaviour
{
    private const int Segments = 64;

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.loop = true;
        _lineRenderer.positionCount = Segments;
        _lineRenderer.useWorldSpace = false;
    }

    /// <summary>Positions the indicator at the given world position and draws a circle of the given radius.</summary>
    public void Show(Vector3 worldPosition, float radius)
    {
        transform.position = worldPosition;
        DrawCircle(radius);
        gameObject.SetActive(true);
    }

    /// <summary>Hides the range indicator.</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void DrawCircle(float radius)
    {
        float angleStep = 360f / Segments;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}
