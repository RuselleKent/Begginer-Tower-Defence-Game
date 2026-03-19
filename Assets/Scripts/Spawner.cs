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
    public static event Action OnBossWarning;
    public static event Action<int> OnNextWaveIn;

    [SerializeField] private WaveData[] waves;
    private int _currentWaveIndex;
    private int _waveCounter;
    private WaveData CurrentWave => waves != null && waves.Length > 0 ? waves[_currentWaveIndex] : null;

    private Dictionary<GameObject, ObjectPooler> _pools = new Dictionary<GameObject, ObjectPooler>();

    private float _spawnTimer;
    private int _spawnCounter;
    private int _finalSpawnCounter;
    private int _enemiesRemoved;

    private const float TimeBetweenWaves = 1f;
    private float _waveCooldown;
    private bool _isBetweenWaves;
    private bool _isEndlessMode;
    private bool _hasStarted;
    private bool _bossWarningFired;
    private int _lastTimerSecond = -1;

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

            if (wave.finalSpawns != null)
            {
                foreach (EnemySpawnEntry entry in wave.finalSpawns)
                {
                    if (entry == null || entry.enemyPrefab == null)
                        continue;
                    RegisterPool(entry.enemyPrefab, Mathf.Max(entry.poolSize, 1));
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

            int secondsLeft = Mathf.CeilToInt(_waveCooldown);
            if (secondsLeft != _lastTimerSecond)
            {
                _lastTimerSecond = secondsLeft;

                bool isLastWave = LevelManager.Instance != null &&
                                  LevelManager.Instance.CurrentLevel != null &&
                                  _waveCounter + 1 >= LevelManager.Instance.CurrentLevel.wavesToWin &&
                                  !_isEndlessMode;

                if (!isLastWave)
                    OnNextWaveIn?.Invoke(Mathf.Max(0, secondsLeft));
            }

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
                _bossWarningFired = false;
                _lastTimerSecond = -1;
            }
        }
        else
        {
            _spawnTimer -= Time.deltaTime;

            bool randomsDone = _spawnCounter >= CurrentWave.EnemiesPerWave;
            bool finalsDone = CurrentWave.finalSpawns == null || _finalSpawnCounter >= CurrentWave.finalSpawns.Length;

            if (!_bossWarningFired && randomsDone && !finalsDone)
            {
                _bossWarningFired = true;
                OnBossWarning?.Invoke();
            }

            if (_spawnTimer <= 0)
            {
                if (!randomsDone)
                {
                    _spawnTimer = CurrentWave.spawnInterval;
                    SpawnWeightedEnemy();
                    _spawnCounter++;
                }
                else if (!finalsDone)
                {
                    _spawnTimer = CurrentWave.spawnInterval;
                    SpawnFinalEnemy(_finalSpawnCounter);
                    _finalSpawnCounter++;
                }
            }

            if (randomsDone && finalsDone && _enemiesRemoved >= CurrentWave.TotalEnemies)
            {
                _isBetweenWaves = true;
                _waveCooldown = TimeBetweenWaves;
                _lastTimerSecond = -1;
            }
        }
    }

    private EnemySpawnEntry PickWeightedEntry(WaveData wave)
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
                return entry;
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

        EnemySpawnEntry entry = PickWeightedEntry(CurrentWave);
        if (entry == null)
        {
            Debug.LogError($"Spawner: Could not pick a valid entry from wave '{CurrentWave.name}'!");
            return;
        }

        SpawnFromPool(entry);
    }

    private void SpawnFinalEnemy(int index)
    {
        if (CurrentWave.finalSpawns == null || index >= CurrentWave.finalSpawns.Length)
            return;

        EnemySpawnEntry entry = CurrentWave.finalSpawns[index];
        if (entry == null || entry.enemyPrefab == null)
        {
            Debug.LogError($"Spawner: Final spawn at index {index} in wave '{CurrentWave.name}' is null!");
            return;
        }

        SpawnFromPool(entry);
    }

    private void SpawnFromPool(EnemySpawnEntry entry)
    {
        if (!_pools.TryGetValue(entry.enemyPrefab, out ObjectPooler pool))
        {
            Debug.LogError($"Spawner: No pool found for prefab '{entry.enemyPrefab.name}'!");
            return;
        }

        GameObject spawnedObject = pool.GetPooledObject();
        if (spawnedObject == null)
        {
            Debug.LogError($"Spawner: Pool for '{entry.enemyPrefab.name}' returned null!");
            return;
        }

        spawnedObject.transform.position = transform.position;

        Enemy enemy = spawnedObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Pass the immunity duration from the spawn entry
            enemy.Initialize(entry.healthMultiplier, entry.speedMultiplier, entry.rewardMultiplier, entry.armorMultiplier);
            spawnedObject.SetActive(true);
        }
        else
        {
            Debug.LogError($"Spawner: Prefab '{entry.enemyPrefab.name}' is missing an Enemy component!");
        }
    }

    private void HandleEnemyReachedEnd(EnemyData data) => _enemiesRemoved++;
    private void HandleEnemyDestroyed(Enemy enemy) => _enemiesRemoved++;

    /// <summary>Enables endless mode, preventing mission complete from firing after the last wave.</summary>
    public void EnableEndlessMode()
    {
        _isEndlessMode = true;
    }
}
