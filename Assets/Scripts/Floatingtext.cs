using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))] // kailangan ng TextMeshPro component para gumana to
public class FloatingText : MonoBehaviour
{
    private const float FloatSpeed = 1.2f; // bilis ng pag-angat ng text (units per second)
    private const float Lifetime = 1.2f; // ilang seconds bago mawala yung text

    private TextMeshPro _text; // yung TextMeshPro component para ma-manipulate yung text at kulay
    private float _elapsed; // ilang seconds na ang lumipas mula nang mag-start
    private Color _startColor; // original na kulay ng text (para i-fade out)

    private void Awake()
    {
        _text = GetComponent<TextMeshPro>(); // kunin yung TextMeshPro component

        Renderer r = GetComponent<Renderer>(); // kunin yung renderer (para i-set yung sorting layer)
        if (r != null) // kung may renderer
        {
            r.sortingLayerName = "UI"; // i-set yung sorting layer sa "UI" para nasa ibabaw ng lahat
            r.sortingOrder = 100; // i-set yung order sa 100 (para sure na nasa harap)
        }
    }

    /// <summary>Sets the displayed message and color, then starts the float animation.</summary>
    public void Initialize(string message, Color color)
    {
        _text.text = message; // i-set yung message (hal. "25g" o "50 damage")
        _text.color = color; // i-set yung kulay (base sa kung positive o negative)
        _startColor = color; // i-save yung original na kulay (para mag-fade from original to transparent)
        _elapsed = 0f; // i-reset yung timer
    }

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime; // dagdagan yung elapsed time (unscaled para kahit naka-pause, gumagalaw pa rin)
        transform.position += Vector3.up * FloatSpeed * Time.unscaledDeltaTime; // i-angat yung text paakyat (unscaled para tuloy-tuloy kahit naka-pause)

        float alpha = Mathf.Lerp(1f, 0f, _elapsed / Lifetime); // compute yung alpha (opacity) - magsisimula sa 1, magiging 0 pag malapit na sa lifetime
        _text.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha); // i-set yung bagong kulay na may fading alpha

        if (_elapsed >= Lifetime) // kung lumipas na yung lifetime
            gameObject.SetActive(false); // i-disable yung GameObject (ibalik sa pool)
    }
}