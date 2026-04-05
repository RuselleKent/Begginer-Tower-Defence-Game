using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public static Spawner Instance { get; private set; } // singleton instance

    public static event Action<int> OnWaveChanged; // event pag nagbago yung wave
    public static event Action OnMissionComplete; // event pag natapos na yung mission
    public static event Action<int> OnCountdownTick; // event bawat segundo ng countdown
    public static event Action OnCountdownComplete; // event pag tapos na yung countdown
    public static event Action OnBossWarning; // event bago lumabas yung boss (final spawns)
    public static event Action<int> OnNextWaveIn; // event kung ilang seconds na lang bago mag-next wave

    [SerializeField] private WaveData[] waves; // listahan ng waves (galing sa inspector)
    private int _currentWaveIndex; // index ng current wave
    private int _waveCounter; // ilang waves na ang natapos (pang-display)
    private WaveData CurrentWave => waves != null && waves.Length > 0 ? waves[_currentWaveIndex] : null; // kunin yung current wave

    private Dictionary<GameObject, ObjectPooler> _pools = new Dictionary<GameObject, ObjectPooler>(); // dito naka-store yung mga object pool per prefab

    private float _spawnTimer; // timer para sa pagitan ng spawn
    private int _spawnCounter; // ilang random enemies na ang na-spawn sa current wave
    private int _finalSpawnCounter; // ilang final enemies (boss) na ang na-spawn
    private int _enemiesRemoved; // ilang kalaban na ang natanggal (namatay o naka-abot sa dulo)

    private float _waveCooldown; // ilang seconds pa bago mag-start yung next wave
    private bool _isBetweenWaves; // flag kung nasa pagitan ng waves
    private bool _hasStarted; // flag kung nag-start na yung game
    private bool _bossWarningFired; // flag kung na-fire na yung boss warning (para isang beses lang)
    private bool _missionComplete; // flag to ensure OnMissionComplete fires only once
    private int _lastTimerSecond = -1; // last second na na-broadcast (para hindi paulit-ulit mag-invoke)

    private void Awake()
    {
        if (Instance != null && Instance != this) // kung may existing instance
        {
            Destroy(gameObject); // sirain yung duplicate
        }
        else
        {
            Instance = this; // i-set yung instance
        }
    }

    private void OnEnable()
    {
        Enemy.OnEnemyReachedEnd += HandleEnemyReachedEnd; // subscribe sa event pag may kalaban na nakarating sa dulo
        Enemy.OnEnemyDestroyed += HandleEnemyDestroyed; // subscribe sa event pag may kalaban na namatay
    }

    private void OnDisable()
    {
        Enemy.OnEnemyReachedEnd -= HandleEnemyReachedEnd; // unsubscribe
        Enemy.OnEnemyDestroyed -= HandleEnemyDestroyed; // unsubscribe
    }

    private void Start()
    {
        if (waves == null || waves.Length == 0) // kung walang waves
        {
            Debug.LogError("Spawner: No waves assigned!"); // mag-error
            return; // wag mag-process
        }

        BuildPools(); // gawin yung mga object pools para sa lahat ng kalaban
        OnWaveChanged?.Invoke(_waveCounter); // i-trigger yung event para ma-update yung UI
    }

    private void BuildPools()
    {
        foreach (WaveData wave in waves) // dumaan sa bawat wave
        {
            if (wave == null) // kung walang wave
                continue; // skip

            if (wave.enemies != null) // kung may random enemies
            {
                foreach (EnemySpawnEntry entry in wave.enemies) // dumaan sa bawat entry
                {
                    if (entry == null || entry.enemyPrefab == null) // kung walang prefab
                    {
                        Debug.LogWarning($"Spawner: Wave '{wave.name}' has an entry with no prefab assigned."); // mag-warning
                        continue; // skip
                    }
                    RegisterPool(entry.enemyPrefab, entry.poolSize); // i-register yung pool
                }
            }

            if (wave.finalSpawns != null) // kung may final spawns (boss)
            {
                foreach (EnemySpawnEntry entry in wave.finalSpawns) // dumaan sa bawat final spawn
                {
                    if (entry == null || entry.enemyPrefab == null) // kung walang prefab
                        continue; // skip
                    RegisterPool(entry.enemyPrefab, Mathf.Max(entry.poolSize, 1)); // i-register yung pool (at least 1)
                }
            }
        }
    }

    private void RegisterPool(GameObject prefab, int size)
    {
        if (_pools.ContainsKey(prefab)) // kung may pool na para sa prefab na to
            return; // wag na gumawa ulit

        GameObject poolObject = new GameObject($"Pool_{prefab.name}"); // gumawa ng bagong GameObject para sa pool
        poolObject.transform.SetParent(transform); // gawing anak ng spawner

        ObjectPooler pooler = poolObject.AddComponent<ObjectPooler>(); // lagyan ng ObjectPooler component
        pooler.Initialize(prefab, size); // i-initialize yung pool

        _pools[prefab] = pooler; // i-save sa dictionary
    }

    /// <summary>Starts the game with a countdown before the first wave spawns.</summary>
    public void StartGameWithCountdown(int countdownSeconds = 3)
    {
        StartCoroutine(CountdownCoroutine(countdownSeconds)); // simulan yung countdown coroutine
    }

    private IEnumerator CountdownCoroutine(int seconds)
    {
        for (int i = seconds; i > 0; i--) // mag-loop pababa mula seconds hanggang 1
        {
            OnCountdownTick?.Invoke(i); // i-trigger yung event (para magpakita ng countdown sa UI)
            yield return new WaitForSeconds(1f); // maghintay ng 1 segundo
        }

        OnCountdownComplete?.Invoke(); // i-trigger yung event na tapos na ang countdown
        _hasStarted = true; // i-start na yung game
    }

    private void Update()
    {
        if (!_hasStarted || CurrentWave == null) // kung hindi pa nag-start o walang current wave
            return; // wag mag-spawn

        if (_isBetweenWaves) // kung nasa pagitan ng waves
        {
            _waveCooldown -= Time.deltaTime; // bawas ng cooldown timer

            int secondsLeft = Mathf.CeilToInt(_waveCooldown); // kung ilang segundo ang natitira (i-round up)
            if (secondsLeft != _lastTimerSecond) // kung nagbago yung segundo
            {
                _lastTimerSecond = secondsLeft; // i-save yung bagong segundo

                bool isLastWave = LevelManager.Instance != null && // tseke kung last wave na to
                                  LevelManager.Instance.CurrentLevel != null &&
                                  _waveCounter + 1 >= LevelManager.Instance.CurrentLevel.wavesToWin;

                if (!isLastWave) // kung hindi last wave
                    OnNextWaveIn?.Invoke(Mathf.Max(0, secondsLeft)); // i-trigger yung event para magpakita ng timer sa UI
            }

            if (_waveCooldown <= 0f) // kung tapos na ang cooldown
            {
                if (!_missionComplete &&
                    LevelManager.Instance != null &&
                    LevelManager.Instance.CurrentLevel != null &&
                    _waveCounter + 1 >= LevelManager.Instance.CurrentLevel.wavesToWin) // kung last wave na
                {
                    _missionComplete = true; // markahan para hindi na mag-fire ulit
                    OnMissionComplete?.Invoke(); // i-trigger yung mission complete event (isang beses lang)
                    return; // tapos na
                }

                _currentWaveIndex = (_currentWaveIndex + 1) % waves.Length; // lipat sa next wave (kung paikot)
                _waveCounter++; // dagdagan yung wave counter
                OnWaveChanged?.Invoke(_waveCounter); // i-trigger yung event para ma-update yung UI
                _spawnCounter = 0; // reset spawn counter
                _finalSpawnCounter = 0; // reset final spawn counter
                _enemiesRemoved = 0; // reset enemies removed
                _spawnTimer = 0f; // reset spawn timer
                _isBetweenWaves = false; // hindi na between waves
                _bossWarningFired = false; // reset boss warning flag
                _lastTimerSecond = -1; // reset last timer second
            }
        }
        else // kung may ongoing wave
        {
            _spawnTimer -= Time.deltaTime; // bawas ng spawn timer

            bool randomsDone = _spawnCounter >= CurrentWave.EnemiesPerWave; // tseke kung tapos na ang random spawns
            bool finalsDone = CurrentWave.finalSpawns == null || _finalSpawnCounter >= CurrentWave.finalSpawns.Length; // tseke kung tapos na ang final spawns

            if (!_bossWarningFired && randomsDone && !finalsDone) // kung hindi pa na-fire ang boss warning at random spawns tapos na pero may final spawns pa
            {
                _bossWarningFired = true; // markahan na na-fire na
                OnBossWarning?.Invoke(); // i-trigger yung boss warning event
            }

            if (_spawnTimer <= 0) // kung oras na para mag-spawn
            {
                if (!randomsDone) // kung hindi pa tapos ang random spawns
                {
                    _spawnTimer = CurrentWave.spawnInterval; // i-reset yung timer
                    SpawnWeightedEnemy(); // mag-spawn ng random enemy (based sa weight)
                    _spawnCounter++; // dagdagan yung spawn counter
                }
                else if (!finalsDone) // kung tapos na random spawns pero may final spawns pa
                {
                    _spawnTimer = CurrentWave.spawnInterval; // i-reset yung timer
                    SpawnFinalEnemy(_finalSpawnCounter); // mag-spawn ng final enemy (boss)
                    _finalSpawnCounter++; // dagdagan yung final spawn counter
                }
            }

            if (randomsDone && finalsDone && _enemiesRemoved >= CurrentWave.TotalEnemies) // kung tapos na lahat ng spawn at lahat ng kalaban ay natanggal na
            {
                _isBetweenWaves = true; // mag-transition sa between waves
                _waveCooldown = CurrentWave.timeBetweenWaves; // i-set yung cooldown timer
                _lastTimerSecond = -1; // reset last timer second
            }
        }
    }

    private EnemySpawnEntry PickWeightedEntry(WaveData wave)
    {
        int totalWeight = 0;
        foreach (EnemySpawnEntry entry in wave.enemies) // kalkulahin ang total weight
        {
            if (entry != null && entry.enemyPrefab != null)
                totalWeight += entry.spawnWeight; // dagdagan yung total weight
        }

        if (totalWeight <= 0) // kung walang weight
            return null; // walang mapipili

        int roll = UnityEngine.Random.Range(0, totalWeight); // random number mula 0 hanggang total weight
        int accumulated = 0; // accumulated weight

        foreach (EnemySpawnEntry entry in wave.enemies) // dumaan sa bawat entry
        {
            if (entry == null || entry.enemyPrefab == null) // kung walang prefab
                continue; // skip

            accumulated += entry.spawnWeight; // dagdagan yung accumulated weight
            if (roll < accumulated) // kung yung random roll ay mas mababa sa accumulated
                return entry; // ito yung napili
        }

        return null; // walang napili (dapat hindi mangyari to)
    }

    private void SpawnWeightedEnemy()
    {
        if (CurrentWave.enemies == null || CurrentWave.enemies.Length == 0) // kung walang enemies
        {
            Debug.LogError($"Spawner: Wave '{CurrentWave.name}' has no enemies configured!"); // mag-error
            return; // wag mag-spawn
        }

        EnemySpawnEntry entry = PickWeightedEntry(CurrentWave); // pumili ng random enemy base sa weight
        if (entry == null) // kung walang napili
        {
            Debug.LogError($"Spawner: Could not pick a valid entry from wave '{CurrentWave.name}'!"); // mag-error
            return; // wag mag-spawn
        }

        SpawnFromPool(entry); // i-spawn yung napiling enemy
    }

    private void SpawnFinalEnemy(int index)
    {
        if (CurrentWave.finalSpawns == null || index >= CurrentWave.finalSpawns.Length) // kung walang final spawns o lagpas sa index
            return; // wag mag-spawn

        EnemySpawnEntry entry = CurrentWave.finalSpawns[index]; // kunin yung final spawn entry
        if (entry == null || entry.enemyPrefab == null) // kung walang prefab
        {
            Debug.LogError($"Spawner: Final spawn at index {index} in wave '{CurrentWave.name}' is null!"); // mag-error
            return; // wag mag-spawn
        }

        SpawnFromPool(entry); // i-spawn yung final enemy
    }

    private void SpawnFromPool(EnemySpawnEntry entry)
    {
        if (!_pools.TryGetValue(entry.enemyPrefab, out ObjectPooler pool)) // hanapin yung pool para sa prefab na to
        {
            Debug.LogError($"Spawner: No pool found for prefab '{entry.enemyPrefab.name}'!"); // mag-error
            return; // wag mag-spawn
        }

        GameObject spawnedObject = pool.GetPooledObject(); // kumuha ng object sa pool
        if (spawnedObject == null) // kung walang nakuha
        {
            Debug.LogError($"Spawner: Pool for '{entry.enemyPrefab.name}' returned null!"); // mag-error
            return; // wag mag-spawn
        }

        spawnedObject.transform.position = transform.position; // i-position sa spawn point

        Enemy enemy = spawnedObject.GetComponent<Enemy>(); // kunin yung Enemy component
        if (enemy != null) // kung may enemy
        {
            enemy.Initialize(entry.healthMultiplier, entry.speedMultiplier, entry.rewardMultiplier, entry.armorMultiplier); // i-initialize yung enemy stats (may multipliers)
            spawnedObject.SetActive(true); // i-activate (mag-move na)
        }
        else // kung walang Enemy component
        {
            Debug.LogError($"Spawner: Prefab '{entry.enemyPrefab.name}' is missing an Enemy component!"); // mag-error
        }
    }

    private void HandleEnemyReachedEnd(EnemyData data) => _enemiesRemoved++; // dagdagan yung enemies removed pag may kalaban na nakarating sa dulo
    private void HandleEnemyDestroyed(Enemy enemy) => _enemiesRemoved++; // dagdagan yung enemies removed pag may kalaban na namatay

    /// <summary>Forces the wave timer to re-broadcast its current value on the next frame. Call this after unpausing.</summary>
    public void RefreshWaveTimer()
    {
        if (_isBetweenWaves) // kung nasa pagitan ng waves
            _lastTimerSecond = -1; // i-reset para mag-broadcast ulit sa next frame
    }
}
