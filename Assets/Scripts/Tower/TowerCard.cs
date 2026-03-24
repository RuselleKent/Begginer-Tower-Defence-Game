using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(CanvasGroup))] // kailangan ng CanvasGroup component para sa fade effect at interactability
public class TowerCard : MonoBehaviour
{
    [SerializeField] private Image towerImage; // image na nagpapakita ng sprite ng tower
    [SerializeField] private TMP_Text costText; // text na nagpapakita kung magkano ang tower

    private TowerData _towerData; // data ng tower (sprite, cost, stats)
    private CanvasGroup _canvasGroup; // pang-control ng alpha at interactability ng card

    public static event Action<TowerData> OnTowerSelected; // event na nagfi-fire kapag napili yung tower card

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>(); // kunin yung CanvasGroup component
    }

    private void OnEnable()
    {
        GameManager.OnResourcesChanged += OnResourcesChanged; // mag-subscribe sa event pag nagbago yung resources (gold)
    }

    private void OnDisable()
    {
        GameManager.OnResourcesChanged -= OnResourcesChanged; // mag-unsubscribe para iwas memory leak
    }

    /// <summary>Populates the card with tower data and updates affordability visuals.</summary>
    public void Initialize(TowerData data)
    {
        _towerData = data; // i-save yung tower data
        towerImage.sprite = data.sprite; // i-set yung sprite ng tower image
        costText.text = $"{data.cost}g"; // i-set yung text ng cost (may "g" sa dulo)
        UpdateAffordability(GameManager.Instance != null ? GameManager.Instance.Resources : 0); // i-update yung visual base sa current gold (kung may GameManager)
    }

    /// <summary>Always fires the selected event — affordability is handled by UIController.</summary>
    public void PlaceTower()
    {
        OnTowerSelected?.Invoke(_towerData); // i-trigger yung event na may napiling tower (yung UIController ang bahala kung afford o hindi)
    }

    private void OnResourcesChanged(int currentResources)
    {
        UpdateAffordability(currentResources); // tawagin yung update affordability kapag nagbago yung resources
    }

    private void UpdateAffordability(int currentResources)
    {
        if (_towerData == null || _canvasGroup == null) // kung walang data o canvas group
            return; // wag mag-update

        bool canAfford = currentResources >= _towerData.cost; // tseke kung kaya bilhin yung tower

        // Dim the card visually but keep it always clickable
        _canvasGroup.alpha = canAfford ? 1f : 0.4f; // kung afford, 100% kita, kung hindi, 40% lang (faded)
        _canvasGroup.interactable = true; // laging pwedeng i-click (hindi naka-disable)
        _canvasGroup.blocksRaycasts = true; // laging na-d-detect yung click (para laging pwedeng pindutin)
    }
}