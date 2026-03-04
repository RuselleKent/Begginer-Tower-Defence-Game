using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "Scriptable Objects/TowerData")]
public class TowerData : ScriptableObject
{
    public float range;
    public float shootInterval;
    public float projectileSpeed;
    public float projectileDuration;
    public float projectileSize;
    public float damage;

    public int cost;
    public Sprite sprite;

    public GameObject prefab;
    
    [Header("Upgrade System")]
    public TowerData nextUpgrade;
    public int upgradeCost;
    public int refundValue;
}
