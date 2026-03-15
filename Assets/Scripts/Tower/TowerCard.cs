using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(CanvasGroup))]
public class TowerCard : MonoBehaviour
{
    [SerializeField] private Image towerImage;
    [SerializeField] private TMP_Text costText;

    private TowerData _towerData;
    private CanvasGroup _canvasGroup;

    public static event Action<TowerData> OnTowerSelected;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        GameManager.OnResourcesChanged += OnResourcesChanged;
    }

    private void OnDisable()
    {
        GameManager.OnResourcesChanged -= OnResourcesChanged;
    }

    /// <summary>Populates the card with tower data and updates affordability visuals.</summary>
    public void Initialize(TowerData data)
    {
        _towerData = data;
        towerImage.sprite = data.sprite;
        costText.text = $"{data.cost}g";
        UpdateAffordability(GameManager.Instance != null ? GameManager.Instance.Resources : 0);
    }

    public void PlaceTower()
    {
        OnTowerSelected?.Invoke(_towerData);
    }

    private void OnResourcesChanged(int currentResources)
    {
        UpdateAffordability(currentResources);
    }

    private void UpdateAffordability(int currentResources)
    {
        if (_towerData == null || _canvasGroup == null)
            return;

        bool canAfford = currentResources >= _towerData.cost;
        _canvasGroup.alpha = canAfford ? 1f : 0.4f;
        _canvasGroup.interactable = canAfford;
        _canvasGroup.blocksRaycasts = canAfford;
    }
}
