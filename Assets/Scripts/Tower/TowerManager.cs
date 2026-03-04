using UnityEngine;
using System;

public class TowerManager : MonoBehaviour
{
    public static event Action<TowerManager> OnTowerClicked;

    private TowerData _currentData;
    private Platform _platform;

    public TowerData CurrentData => _currentData;
    public Platform Platform => _platform;
    public int RefundValue => _currentData != null ? _currentData.refundValue : 0;

    private void Start()
    {
        int towerLayer = LayerMask.NameToLayer(GameConstants.LAYER_TOWERS);
        if (towerLayer != -1)
            gameObject.layer = towerLayer;
        else
            Debug.LogWarning($"TowerManager: Layer '{GameConstants.LAYER_TOWERS}' not found!");
    }

    /// <summary>Initializes the tower with its data and the platform it's placed on.</summary>
    public void Initialize(TowerData data, Platform platform)
    {
        if (data == null)
        {
            Debug.LogError("TowerManager: Cannot initialize with null TowerData!");
            return;
        }

        if (platform == null)
        {
            Debug.LogError("TowerManager: Cannot initialize with null Platform!");
            return;
        }

        _currentData = data;
        _platform = platform;
    }

    /// <summary>Refunds the tower, restores resources and reveals the platform.</summary>
    public void Refund()
    {
        if (_platform != null)
            _platform.ShowSprite();

        if (GameManager.Instance != null)
            GameManager.Instance.AddResources(RefundValue);

        Destroy(gameObject);
    }

    /// <summary>Handles click on this tower to show the tower actions panel.</summary>
    public void HandleClick()
    {
        if (Platform.towerPanelOpen)
            return;

        if (UIController.IsCountdownActive)
            return;

        OnTowerClicked?.Invoke(this);
    }
}
