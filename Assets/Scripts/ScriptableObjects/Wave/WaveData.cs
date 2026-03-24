using UnityEngine;

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab; // yung prefab ng kalaban na gagamitin

    [Tooltip("How many of this enemy type to pre-instantiate in the pool")]
    public int poolSize = 10; // ilang kalaban nito ang ipa-pool muna bago mag-start

    [Tooltip("How many of this enemy type spawn during this wave")]
    public int spawnCount = 5; // ilang beses lalabas yung ganitong kalaban sa wave

    [Range(1, 100)]
    [Tooltip("Higher weight = more likely to be picked when spawning")]
    public int spawnWeight = 1; // gaano kataas ang chance na mapili yung kalaban pag random spawn (mas mataas = mas malamang)

    [Header("Stat Multipliers")]
    [Tooltip("Multiplies the base health of this enemy for this wave")]
    public float healthMultiplier = 1f; // pampadami ng health ng kalaban para sa wave na to

    [Tooltip("Multiplies the base armor of this enemy for this wave. 0 = no armor")]
    public float armorMultiplier = 1f; // pampadami ng armor (0 = walang armor)

    [Tooltip("Multiplies the base speed of this enemy for this wave")]
    public float speedMultiplier = 1f; // pampabilis o pampabagal ng takbo

    [Tooltip("Multiplies the base resource reward of this enemy for this wave")]
    public float rewardMultiplier = 1f; // pampadami ng reward kapag napatay
}

[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/WaveData")]
public class WaveData : ScriptableObject
{
    public EnemySpawnEntry[] enemies; // listahan ng mga kalaban na pwedeng lumabas sa wave na to

    [Tooltip("These entries always spawn at the end of the wave in order, after all random enemies")]
    public EnemySpawnEntry[] finalSpawns; // mga kalaban na siguradong lalabas sa dulo ng wave (sunod-sunod)

    public float spawnInterval; // pagitan ng pag-spawn ng bawat kalaban (seconds)

    [Tooltip("Seconds to wait after this wave ends before the next wave begins")]
    public float timeBetweenWaves = 10f; // ilang seconds aantayin bago mag-start yung next wave

    public int EnemiesPerWave
    {
        get
        {
            int total = 0; // panimulang bilang
            if (enemies == null) // kung walang enemy entries
                return 0; // walang kalaban

            foreach (EnemySpawnEntry entry in enemies) // dumaan sa bawat enemy entry
            {
                if (entry != null) // kung may laman yung entry
                    total += entry.spawnCount; // idagdag yung spawnCount sa total
            }
            return total; // ibalik kung ilang kalaban ang lalabas
        }
    }

    public int TotalEnemies => EnemiesPerWave + (finalSpawns != null ? finalSpawns.Length : 0); // kabuuang kalaban = random spawns + final spawns (kung may final spawns)
}