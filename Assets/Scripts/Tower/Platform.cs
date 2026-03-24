using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class Platform : MonoBehaviour
{
    public static event Action<Platform> OnPlatformClicked;

    public static bool towerPanelOpen { get; set; } = false;

    /// <summary>True only when a valid TowerManager child is present on this platform.</summary>
    public bool HasTower => PlacedTower != null;

    /// <summary>Returns the TowerManager on the first child, or null if none exists.</summary>
    public TowerManager PlacedTower => transform.childCount > 0
        ? transform.GetChild(0).GetComponent<TowerManager>()
        : null;

    private Collider2D _collider;
    private SpriteRenderer _spriteRenderer;
    private Camera _mainCamera;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _mainCamera = Camera.main;

        if (_collider == null)
            Debug.LogError($"Platform '{gameObject.name}': Missing Collider2D!");
    }

    private void OnEnable()
    {
        // Re-cache camera in case scene reloaded
        if (_mainCamera == null)
            _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
                return;
        }

        if (UIController.IsCountdownActive || TutorialManager.IsActive)
            return;

        if (towerPanelOpen || Time.timeScale == 0f)
            return;

        // ─── PC only: Right-click to interact with a placed tower ────────────
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 rightClickWorld = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            if (_collider != null && _collider.OverlapPoint(rightClickWorld) && HasTower)
            {
                PlacedTower?.HandleClick();
                return;
            }
        }

        // ─── Universal: Left-click (PC) and tap (mobile/WebGL) ───────────────
        Pointer pointer = Pointer.current;

        if (pointer == null || !pointer.press.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 screenPos = pointer.position.ReadValue();
        Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(screenPos);

        if (_collider == null || !_collider.OverlapPoint(worldPoint))
            return;

        if (HasTower)
            PlacedTower?.HandleClick();
        else
            OnPlatformClicked?.Invoke(this);
    }

    /// <summary>Places a tower on this platform using the provided TowerData.</summary>
    public void PlaceTower(TowerData data)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError("Platform: Cannot place tower - TowerData or prefab is null");
            return;
        }

        GameObject towerObject = Instantiate(data.prefab, transform.position, Quaternion.identity, transform);
        TowerManager manager = towerObject.GetComponent<TowerManager>();

        if (manager != null)
            manager.Initialize(data, this);
        else
            Debug.LogError("Platform: Placed tower prefab is missing TowerManager component");

        HideSprite();
    }

    /// <summary>Hides the platform sprite when a tower is placed on it.</summary>
    public void HideSprite()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = false;
    }

    /// <summary>Shows the platform sprite when no tower is present.</summary>
    public void ShowSprite()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;
    }
}
