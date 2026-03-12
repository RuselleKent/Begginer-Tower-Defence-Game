using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public static Spawner Instance { get; private set; }

    public static event Action<int> OnWaveChanged;
    public static event Action OnMissionComplete;
    public static event Action<int> OnCountdownTick;
    public static event Action OnCountdownComplete;

    [SerializeField] private WaveData[] waves;
    private int _currentWaveIndex = 0;
    private int _waveCounter = 0;
    private WaveData CurrentWave => waves != null && waves.Length > 0 ? waves[_currentWaveIndex] : null;

    private Dictionary<GameObject, ObjectPooler> _pools = new Dictionary<GameObject, ObjectPooler>();

    private float _spawnTimer;
    private int _spawnCounter;
    private int _finalSpawnCounter;
    private int _enemiesRemoved;

    private float _timeBetweenWaves = 1f;
    private float _waveCooldown;
    private bool _isBetweenWaves = false;
    private bool _isEndlessMode = false;
    private bool _hasStarted = false;

    [Header("Enemy Scaling Per Wave")]
    [SerializeField] private float healthIncreasePerWave = 0.15f;
    [SerializeField] private float speedIncreasePerWave = 0.05f;
    [SerializeField] private float rewardIncreasePerWave = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        Enemy.OnEnemyReachedEnd += HandleEnemyReachedEnd;
        Enemy.OnEnemyDestroyed += HandleEnemyDestroyed;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyReachedEnd -= HandleEnemyReachedEnd;
        Enemy.OnEnemyDestroyed -= HandleEnemyDestroyed;
    }

    private void Start()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("Spawner: No waves assigned!");
            return;
        }

        BuildPools();
        OnWaveChanged?.Invoke(_waveCounter);
    }

    private void BuildPools()
    {
        foreach (WaveData wave in waves)
        {
            if (wave == null)
                continue;

            // Pool random enemies
            if (wave.enemies != null)
            {
                foreach (EnemySpawnEntry entry in wave.enemies)
                {
                    if (entry == null || entry.enemyPrefab == null)
                    {
                        Debug.LogWarning($"Spawner: Wave '{wave.name}' has an entry with no prefab assigned.");
                        continue;
                    }

                    RegisterPool(entry.enemyPrefab, entry.poolSize);
                }
            }

            // Pool final spawn enemies
            if (wave.finalSpawns != null)
            {
                foreach (GameObject prefab in wave.finalSpawns)
                {
                    if (prefab == null)
                        continue;

                    RegisterPool(prefab, 1);
                }
            }
        }
    }

    private void RegisterPool(GameObject prefab, int size)
    {
        if (_pools.ContainsKey(prefab))
            return;

        GameObject poolObject = new GameObject($"Pool_{prefab.name}");
        poolObject.transform.SetParent(transform);

        ObjectPooler pooler = poolObject.AddComponent<ObjectPooler>();
        pooler.Initialize(prefab, size);

        _pools[prefab] = pooler;
    }

    /// <summary>Starts the game with a countdown before the first wave spawns.</summary>
    public void StartGameWithCountdown(int countdownSeconds = 3)
    {
        StartCoroutine(CountdownCoroutine(countdownSeconds));
    }

    private IEnumerator CountdownCoroutine(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            OnCountdownTick?.Invoke(i);
            yield return new WaitForSeconds(1f);
        }

        OnCountdownComplete?.Invoke();
        _hasStarted = true;
    }

    private void Update()
    {
        if (!_hasStarted || CurrentWave == null)
            return;

        if (_isBetweenWaves)
        {
            _waveCooldown -= Time.deltaTime;

            if (_waveCooldown <= 0f)
            {
                if (LevelManager.Instance != null &&
                    LevelManager.Instance.CurrentLevel != null &&
                    _waveCounter + 1 >= LevelManager.Instance.CurrentLevel.wavesToWin &&
                    !_isEndlessMode)
                {
                    OnMissionComplete?.Invoke();
                    return;
                }

                _currentWaveIndex = (_currentWaveIndex + 1) % waves.Length;
                _waveCounter++;
                OnWaveChanged?.Invoke(_waveCounter);
                _spawnCounter = 0;
                _finalSpawnCounter = 0;
                _enemiesRemoved = 0;
                _spawnTimer = 0f;
                _isBetweenWaves = false;
            }
        }
        else
        {
            _spawnTimer -= Time.deltaTime;

            bool randomsDone = _spawnCounter >= CurrentWave.EnemiesPerWave;
            bool finalsDone = CurrentWave.finalSpawns == null || _finalSpawnCounter >= CurrentWave.finalSpawns.Length;

            if (_spawnTimer <= 0)
            {
                if (!randomsDone)
                {
                    // Spawn a random weighted enemy
                    _spawnTimer = CurrentWave.spawnInterval;
                    SpawnWeightedEnemy();
                    _spawnCounter++;
                }
                else if (!finalsDone)
                {
                    // Spawn the next guaranteed final enemy
                    _spawnTimer = CurrentWave.spawnInterval;
                    SpawnFinalEnemy(_finalSpawnCounter);
                    _finalSpawnCounter++;
                }
            }

            // Wave ends only when all enemies are spawned AND removed
            if (randomsDone && finalsDone && _enemiesRemoved >= CurrentWave.TotalEnemies)
            {
                _isBetweenWaves = true;
                _waveCooldown = _timeBetweenWaves;
            }
        }
    }

    /// <summary>Picks a random enemy prefab from the current wave using spawn weights.</summary>
    private GameObject PickWeightedPrefab(WaveData wave)
    {
        int totalWeight = 0;
        foreach (EnemySpawnEntry entry in wave.enemies)
        {
            if (entry != null && entry.enemyPrefab != null)
                totalWeight += entry.spawnWeight;
        }

        if (totalWeight <= 0)
            return null;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int accumulated = 0;

        foreach (EnemySpawnEntry entry in wave.enemies)
        {
            if (entry == null || entry.enemyPrefab == null)
                continue;

            accumulated += entry.spawnWeight;
            if (roll < accumulated)
                return entry.enemyPrefab;
        }

        return null;
    }

    private void SpawnWeightedEnemy()
    {
        if (CurrentWave.enemies == null || CurrentWave.enemies.Length == 0)
        {
            Debug.LogError($"Spawner: Wave '{CurrentWave.name}' has no enemies configured!");
            return;
        }

        GameObject prefab = PickWeightedPrefab(CurrentWave);
        if (prefab == null)
        {
            Debug.LogError($"Spawner: Could not pick a valid prefab from wave '{CurrentWave.name}'!");
            return;
        }

        SpawnFromPool(prefab);
    }

    private void SpawnFinalEnemy(int index)
    {
        if (CurrentWave.finalSpawns == null || index >= CurrentWave.finalSpawns.Length)
            return;

        GameObject prefab = CurrentWave.finalSpawns[index];
        if (prefab == null)
        {
            Debug.LogError($"Spawner: Final spawn at index {index} in wave '{CurrentWave.name}' is null!");
            return;
        }

        SpawnFromPool(prefab);
    }

    private void SpawnFromPool(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out ObjectPooler pool))
        {
            Debug.LogError($"Spawner: No pool found for prefab '{prefab.name}'!");
            return;
        }

        GameObject spawnedObject = pool.GetPooledObject();
        if (spawnedObject == null)
        {
            Debug.LogError($"Spawner: Pool for '{prefab.name}' returned null!");
            return;
        }

        spawnedObject.transform.position = transform.position;

        float healthMultiplier = 1f + (_waveCounter * healthIncreasePerWave);
        float speedMultiplier = 1f + (_waveCounter * speedIncreasePerWave);
        float rewardMultiplier = 1f + (_waveCounter * rewardIncreasePerWave);

        Enemy enemy = spawnedObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(healthMultiplier, speedMultiplier, rewardMultiplier);
            spawnedObject.SetActive(true);
        }
        else
        {
            Debug.LogError($"Spawner: Prefab '{prefab.name}' is missing an Enemy component!");
        }
    }

    private void HandleEnemyReachedEnd(EnemyData data)
    {
        _enemiesRemoved++;
    }

    private void HandleEnemyDestroyed(Enemy enemy)
    {
        _enemiesRemoved++;
    }

    /// <summary>Enables endless mode, preventing mission complete from firing after the last wave.</summary>
    public void EnableEndlessMode()
    {
        _isEndlessMode = true;
    }
}
