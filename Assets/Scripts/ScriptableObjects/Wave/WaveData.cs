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
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/WaveData")]
public class WaveData : ScriptableObject
{
    public EnemySpawnEntry[] enemies;

    [Tooltip("These enemies always spawn at the end of the wave, in order, after all random enemies are spawned")]
    public GameObject[] finalSpawns;

    public float spawnInterval;

    // Total random enemies derived from spawn counts
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

    // Grand total including final spawns
    public int TotalEnemies => EnemiesPerWave + (finalSpawns != null ? finalSpawns.Length : 0);
}
