using UnityEngine;
using System;

public class TowerManager : MonoBehaviour
{
    public static event Action<TowerManager> OnTowerClicked; // event na nagfi-fire kapag may na-click na tower (para magpakita ng panel)

    private TowerData _currentData; // data ng tower (damage, range, cost, etc.)
    private Platform _platform; // yung platform kung saan nakalagay yung tower

    public TowerData CurrentData => _currentData; // para makuha yung tower data mula sa ibang scripts
    public Platform Platform => _platform; // para makuha yung platform mula sa ibang scripts
    public int RefundValue => _currentData != null ? _currentData.refundValue : 0; // kung magkano ang makukuha kapag binenta (kung may data, kunin yung refund value)

    private void Start()
    {
        int towerLayer = LayerMask.NameToLayer(GameConstants.LAYER_TOWERS); // kunin yung layer number ng "Towers" layer
        if (towerLayer != -1) // kung may nahanap na layer (hindi -1)
            gameObject.layer = towerLayer; // i-set yung layer ng tower object para maayos yung collision/raycast
        else // kung walang nahanap
            Debug.LogWarning($"TowerManager: Layer '{GameConstants.LAYER_TOWERS}' not found!"); // mag-warning
    }

    /// <summary>Initializes the tower with its data and the platform it's placed on.</summary>
    public void Initialize(TowerData data, Platform platform)
    {
        if (data == null) // kung walang data
        {
            Debug.LogError("TowerManager: Cannot initialize with null TowerData!"); // mag-error
            return; // wag mag-initialize
        }

        if (platform == null) // kung walang platform
        {
            Debug.LogError("TowerManager: Cannot initialize with null Platform!"); // mag-error
            return; // wag mag-initialize
        }

        _currentData = data; // i-save yung data
        _platform = platform; // i-save yung platform
    }

    /// <summary>Refunds the tower, restores resources and reveals the platform.</summary>
    public void Refund()
    {
        if (_platform != null) // kung may platform
            _platform.ShowSprite(); // ipakita ulit yung sprite ng platform (para makapaglagay ulit ng tower)

        if (GameManager.Instance != null) // kung may GameManager
            GameManager.Instance.AddResources(RefundValue); // idagdag yung refund value sa resources (gold)

        Destroy(gameObject); // sirain yung tower game object
    }

    /// <summary>Handles click on this tower to show the tower actions panel.</summary>
    public void HandleClick()
    {
        if (Platform.towerPanelOpen) // kung may open na tower panel
            return; // wag mag-process (para hindi mag-open ng dalawa)

        if (UIController.IsCountdownActive) // kung may countdown (nagsisimula pa lang)
            return; // wag mag-process

        OnTowerClicked?.Invoke(this); // i-trigger yung event na may na-click na tower (para magpakita ng action panel)
    }
}