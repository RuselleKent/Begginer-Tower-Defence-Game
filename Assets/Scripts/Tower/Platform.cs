using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class Platform : MonoBehaviour
{
    public static event Action<Platform> OnPlatformClicked; // event na nagfi-fire kapag na-click yung platform na walang tower

    public static bool towerPanelOpen { get; set; } = false; // static variable para malaman kung may open na tower panel (para hindi makapag-click sa iba)

    /// <summary>True only when a valid TowerManager child is present on this platform.</summary>
    public bool HasTower => PlacedTower != null; // tseke kung may tower na nakalagay dito

    /// <summary>Returns the TowerManager on the first child, or null if none exists.</summary>
    public TowerManager PlacedTower => transform.childCount > 0 // kung may anak (child) yung platform
        ? transform.GetChild(0).GetComponent<TowerManager>() // kunin yung TowerManager ng unang anak
        : null; // kung wala, mag-return ng null

    private Collider2D _collider; // collider ng platform (para ma-detect kung na-click)
    private SpriteRenderer _spriteRenderer; // sprite renderer (para i-hide/show yung visual ng platform)
    private Camera _mainCamera; // main camera (para mag-convert ng screen to world coordinates)

    private void Awake()
    {
        _collider = GetComponent<Collider2D>(); // kunin yung Collider2D component
        _spriteRenderer = GetComponent<SpriteRenderer>(); // kunin yung SpriteRenderer component
        _mainCamera = Camera.main; // kunin yung main camera

        if (_collider == null) // kung walang collider
            Debug.LogError($"Platform '{gameObject.name}': Missing Collider2D!"); // mag-error
    }

    private void OnEnable()
    {
        // Re-cache camera in case scene reloaded
        if (_mainCamera == null) // kung nawala yung camera reference
            _mainCamera = Camera.main; // kunin ulit yung main camera
    }

    private void Update()
    {
        if (_mainCamera == null) // kung wala pang camera
        {
            _mainCamera = Camera.main; // subukang kunin ulit
            if (_mainCamera == null) // kung wala pa rin
                return; // wag mag-process
        }

        if (UIController.IsCountdownActive || TutorialManager.IsActive) // kung may countdown o tutorial
            return; // wag magpa-click

        if (towerPanelOpen || Time.timeScale == 0f) // kung may open na tower panel o naka-pause
            return; // wag magpa-click

        // ─── PC only: Right-click to interact with a placed tower ────────────
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) // kung may mouse at nag-right click
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) // kung naka-click sa UI
                return; // wag mag-process

            Vector2 rightClickWorld = _mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()); // i-convert yung click position sa world coordinates

            if (_collider != null && _collider.OverlapPoint(rightClickWorld) && HasTower) // kung naka-click sa collider at may tower
            {
                PlacedTower?.HandleClick(); // i-handle yung click sa tower (right-click interaction)
                return; // tapos na
            }
        }

        // ─── Universal: Left-click (PC) and tap (mobile/WebGL) ───────────────
        Pointer pointer = Pointer.current; // kunin yung pointer (mouse o touch)

        if (pointer == null || !pointer.press.wasPressedThisFrame) // kung walang pointer o hindi nag-press
            return; // wag mag-process

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) // kung naka-click sa UI
            return; // wag mag-process

        Vector2 screenPos = pointer.position.ReadValue(); // kunin yung screen position ng click/tap
        Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(screenPos); // i-convert sa world coordinates

        if (_collider == null || !_collider.OverlapPoint(worldPoint)) // kung walang collider o hindi na-click yung collider
            return; // wag mag-process

        if (HasTower) // kung may tower na nakalagay
            PlacedTower?.HandleClick(); // i-click yung tower (left-click interaction)
        else // kung walang tower
            OnPlatformClicked?.Invoke(this); // i-trigger yung event na may na-click na platform na walang tower
    }

    /// <summary>Places a tower on this platform using the provided TowerData.</summary>
    public void PlaceTower(TowerData data)
    {
        if (data == null || data.prefab == null) // kung walang data o walang prefab
        {
            Debug.LogError("Platform: Cannot place tower - TowerData or prefab is null"); // mag-error
            return; // wag mag-place
        }

        GameObject towerObject = Instantiate(data.prefab, transform.position, Quaternion.identity, transform); // i-instantiate yung tower prefab sa position ng platform, gawing anak
        TowerManager manager = towerObject.GetComponent<TowerManager>(); // kunin yung TowerManager ng tower

        if (manager != null) // kung may TowerManager
            manager.Initialize(data, this); // i-initialize yung tower gamit yung data at itong platform
        else // kung walang TowerManager
            Debug.LogError("Platform: Placed tower prefab is missing TowerManager component"); // mag-error

        HideSprite(); // itago yung sprite ng platform (para hindi na makita)
    }

    /// <summary>Hides the platform sprite when a tower is placed on it.</summary>
    public void HideSprite()
    {
        if (_spriteRenderer != null) // kung may sprite renderer
            _spriteRenderer.enabled = false; // i-disable (itago)
    }

    /// <summary>Shows the platform sprite when no tower is present.</summary>
    public void ShowSprite()
    {
        if (_spriteRenderer != null) // kung may sprite renderer
            _spriteRenderer.enabled = true; // i-enable (ipakita)
    }
}