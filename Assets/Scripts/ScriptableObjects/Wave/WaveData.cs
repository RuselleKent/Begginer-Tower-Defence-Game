using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;

    [Tooltip("How many of this enemy type to pre-instantiate in the pool")]
    public int poolSize = 10;

    [Tooltip("How many of this enemy type spawn during this wave")]
    public int spawnCount = 5;

    [Range(1, 100)]
    [Tooltip("Higher weight = more likely to be picked when spawning")]
    public int spawnWeight = 1;

    [Header("Stat Multipliers")]
    [Tooltip("Multiplies the base health of this enemy for this wave")]
    public float healthMultiplier = 1f;

    [Tooltip("Multiplies the base armor of this enemy for this wave. 0 = no armor")]
    public float armorMultiplier = 1f;

    [Tooltip("Multiplies the base speed of this enemy for this wave")]
    public float speedMultiplier = 1f;

    [Tooltip("Multiplies the base resource reward of this enemy for this wave")]
    public float rewardMultiplier = 1f;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/WaveData")]
public class WaveData : ScriptableObject
{
    public EnemySpawnEntry[] enemies;

    [Tooltip("These entries always spawn at the end of the wave in order, after all random enemies")]
    public EnemySpawnEntry[] finalSpawns;

    public float spawnInterval;

    public int EnemiesPerWave
    {
        get
        {
            int total = 0;
            if (enemies == null)
                return 0;

            foreach (EnemySpawnEntry entry in enemies)
            {
                if (entry != null)
                    total += entry.spawnCount;
            }
            return total;
        }
    }

    public int TotalEnemies => EnemiesPerWave + (finalSpawns != null ? finalSpawns.Length : 0);
}
