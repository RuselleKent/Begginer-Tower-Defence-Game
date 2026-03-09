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

    private float _spawnTimer;
    private float _spawnCounter;
    private int _enemiesRemoved;

    [SerializeField] private ObjectPooler orcPool;
    [SerializeField] private ObjectPooler dragonPool;
    [SerializeField] private ObjectPooler kaijuPool;
    [SerializeField] private ObjectPooler bossPool;

    private Dictionary<EnemyType, ObjectPooler> _poolDictionary;

    private float _timeBetweenWaves = 1f;
    private float _waveCooldown;
    private bool _isBetweenWaves = false;
    private bool _isEndlessMode = false;
    private bool _hasStarted = false;

    [Header("Enemy Scaling")]
    [SerializeField] private float healthIncreasePerWave = 0.15f;
    [SerializeField] private float speedIncreasePerWave = 0.05f;
    [SerializeField] private float rewardIncreasePerWave = 0.1f;

    private void Awake()
    {
        _poolDictionary = new Dictionary<EnemyType, ObjectPooler>();

        if (orcPool != null)
            _poolDictionary.Add(EnemyType.Orc, orcPool);
        if (dragonPool != null)
            _poolDictionary.Add(EnemyType.Dragon, dragonPool);
        if (kaijuPool != null)
            _poolDictionary.Add(EnemyType.Kaiju, kaijuPool);
        if (bossPool != null)
            _poolDictionary.Add(EnemyType.Boss, bossPool);

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

        ValidatePoolSetup();

        OnWaveChanged?.Invoke(_waveCounter);
    }

    private void ValidatePoolSetup()
    {
        if (orcPool == null)
            Debug.LogWarning("Spawner: Orc Pool is not assigned!");
        if (dragonPool == null)
            Debug.LogWarning("Spawner: Dragon Pool is not assigned!");
        if (kaijuPool == null)
            Debug.LogWarning("Spawner: Kaiju Pool is not assigned!");

        // Only warn about boss pool if a boss wave is actually configured
        bool hasBossWave = false;
        foreach (WaveData wave in waves)
        {
            if (wave != null && wave.enemyType == EnemyType.Boss)
            {
                hasBossWave = true;
                break;
            }
        }

        if (hasBossWave && bossPool == null)
            Debug.LogWarning("Spawner: Boss Pool is not assigned but a Boss wave exists!");
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
                _enemiesRemoved = 0;
                _spawnTimer = 0f;
                _isBetweenWaves = false;
            }
        }
        else
        {
            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer <= 0 && _spawnCounter < CurrentWave.enemiesPerWave)
            {
                _spawnTimer = CurrentWave.spawnInterval;
                SpawnEnemy();
                _spawnCounter++;
            }
            else if (_spawnCounter >= CurrentWave.enemiesPerWave &&
                     _enemiesRemoved >= CurrentWave.enemiesPerWave)
            {
                _isBetweenWaves = true;
                _waveCooldown = _timeBetweenWaves;
            }
        }
    }

    private void SpawnEnemy()
    {
        if (CurrentWave == null)
        {
            Debug.LogError("Spawner: CurrentWave is null!");
            return;
        }

        if (!_poolDictionary.TryGetValue(CurrentWave.enemyType, out var pool))
        {
            Debug.LogError($"Spawner: No pool found for enemy type {CurrentWave.enemyType}");
            return;
        }

        if (pool == null)
        {
            Debug.LogError($"Spawner: Pool for {CurrentWave.enemyType} is null!");
            return;
        }

        GameObject spawnedObject = pool.GetPooledObject();

        if (spawnedObject == null)
        {
            Debug.LogError($"Spawner: Pool for {CurrentWave.enemyType} returned null! Check pool size or prefab.");
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
            Debug.LogError("Spawner: Pooled object doesn't have Enemy component!");
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
