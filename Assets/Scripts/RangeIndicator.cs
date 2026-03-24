using UnityEngine;

[RequireComponent(typeof(LineRenderer))] // kailangan ng LineRenderer component para gumana to
public class RangeIndicator : MonoBehaviour
{
    private const int Segments = 64; // ilang segments gagamitin para sa bilog (mas mataas = mas smooth)

    private LineRenderer _lineRenderer; // yung LineRenderer na gagamitin para mag-drawing ng circle

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>(); // kunin yung LineRenderer component
        _lineRenderer.loop = true; // i-loop para magsara yung circle (connected yung dulo sa simula)
        _lineRenderer.positionCount = Segments; // i-set kung ilang points ang iguguhit (segments)
        _lineRenderer.useWorldSpace = false; // gamitin yung local space para relative sa position ng GameObject
    }

    /// <summary>Positions the indicator at the given world position and draws a circle of the given radius.</summary>
    public void Show(Vector3 worldPosition, float radius)
    {
        transform.position = worldPosition; // i-position yung range indicator sa world position
        DrawCircle(radius); // i-drawing yung circle na may specific radius
        gameObject.SetActive(true); // ipakita yung range indicator
    }

    /// <summary>Hides the range indicator.</summary>
    public void Hide()
    {
        gameObject.SetActive(false); // itago yung range indicator
    }

    private void DrawCircle(float radius)
    {
        float angleStep = 360f / Segments; // compute kung ilang degrees kada segment
        for (int i = 0; i < Segments; i++) // dumaan sa bawat segment
        {
            float angle = i * angleStep * Mathf.Deg2Rad; // i-convert yung angle sa radians (para sa Mathf.Cos at Sin)
            float x = Mathf.Cos(angle) * radius; // compute x position sa circle (cos * radius)
            float y = Mathf.Sin(angle) * radius; // compute y position sa circle (sin * radius)
            _lineRenderer.SetPosition(i, new Vector3(x, y, 0f)); // i-set yung position ng point sa line renderer
        }
    }
}