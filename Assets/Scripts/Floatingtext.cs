using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    private const float FloatSpeed = 1.2f;
    private const float Lifetime = 1.2f;

    private TextMeshPro _text;
    private float _elapsed;
    private Color _startColor;

    private void Awake()
    {
        _text = GetComponent<TextMeshPro>();

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            r.sortingLayerName = "UI";
            r.sortingOrder = 100;
        }
    }

    /// <summary>Sets the displayed message and color, then starts the float animation.</summary>
    public void Initialize(string message, Color color)
    {
        _text.text = message;
        _text.color = color;
        _startColor = color;
        _elapsed = 0f;
    }

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;
        transform.position += Vector3.up * FloatSpeed * Time.unscaledDeltaTime;

        float alpha = Mathf.Lerp(1f, 0f, _elapsed / Lifetime);
        _text.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);

        if (_elapsed >= Lifetime)
            Destroy(gameObject);
    }
}
