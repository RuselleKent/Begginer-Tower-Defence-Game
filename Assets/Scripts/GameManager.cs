using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // singleton instance para isa lang yung GameManager sa buong game

    public static event Action<int> OnLivesChanged; // event na nagfi-fire kapag nagbago yung lives (buhay) ng player
    public static event Action<int> OnResourcesChanged; // event na nagfi-fire kapag nagbago yung resources (gold)
    public static event Action<int, Vector3> OnResourcesEarned; // event na nagfi-fire kapag nakakuha ng resources (kasama yung position para sa floating text)

    private int _lives; // current lives ng player
    private int _resources; // current resources (gold) ng player
    public int Resources => _resources; // para makuha yung resources mula sa ibang scripts

    private float _gameSpeed = 1f; // current game speed (1 = normal, 2 = double speed, etc.)
    public float GameSpeed => _gameSpeed; // para makuha yung game speed mula sa ibang scripts

    private void Awake()
    {
        if (Instance != null && Instance != this) // kung may existing instance na at hindi ito yun
        {
            Destroy(gameObject); // sirain yung duplicate na GameManager
        }
        else // kung wala pang instance o ito yung una
        {
            Instance = this; // i-set yung instance sa current object
            DontDestroyOnLoad(gameObject); // huwag sirain pag nag-load ng bagong scene
        }
    }

    private void OnEnable()
    {
        Enemy.OnEnemyReachedEnd += HandleEnemyReachedEnd; // mag-subscribe sa event kapag may kalaban na nakarating sa dulo
        Enemy.OnEnemyDestroyed += HandleEnemyDestroyed; // mag-subscribe sa event kapag may kalaban na namatay
        SceneManager.sceneLoaded += OnSceneLoaded; // mag-subscribe sa event kapag may na-load na scene
    }

    private void OnDisable()
    {
        Enemy.OnEnemyReachedEnd -= HandleEnemyReachedEnd; // mag-unsubscribe para iwas memory leak
        Enemy.OnEnemyDestroyed -= HandleEnemyDestroyed; // mag-unsubscribe
        SceneManager.sceneLoaded -= OnSceneLoaded; // mag-unsubscribe
    }

    private void HandleEnemyReachedEnd(EnemyData data)
    {
        _lives = Mathf.Max(0, _lives - data.damage); // bawas ng lives base sa damage ng kalaban (hindi pwedeng bumaba sa zero)
        OnLivesChanged?.Invoke(_lives); // i-trigger yung event para ma-update yung UI
    }

    private void HandleEnemyDestroyed(Enemy enemy)
    {
        int amount = Mathf.RoundToInt(enemy.GetCurrentReward()); // kunin yung reward ng kalaban at i-round sa integer
        Vector3 position = enemy.transform.position; // kunin yung position kung saan namatay yung kalaban
        AddResources(amount); // idagdag yung reward sa resources
        OnResourcesEarned?.Invoke(amount, position); // i-trigger yung event para magpakita ng floating text
    }

    public void AddResources(int amount)
    {
        _resources += amount; // dagdagan yung resources
        OnResourcesChanged?.Invoke(_resources); // i-trigger yung event para ma-update yung UI
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = scale; // i-set yung time scale ng Unity (1 = normal, 0 = pause, 2 = double speed)
    }

    public void SetGameSpeed(float newSpeed)
    {
        _gameSpeed = newSpeed; // i-save yung bagong game speed
        SetTimeScale(_gameSpeed); // i-apply yung time scale
    }

    /// <summary>Stores the desired game speed without affecting Time.timeScale.</summary>
    public void StoreGameSpeed(float newSpeed)
    {
        _gameSpeed = newSpeed; // i-save lang yung game speed (hindi muna i-a-apply)
    }

    public void SpendResources(int amount)
    {
        if (_resources >= amount) // kung sapat yung resources
        {
            _resources -= amount; // bawas yung resources
            OnResourcesChanged?.Invoke(_resources); // i-trigger yung event para ma-update yung UI
        }
    }

    /// <summary>Resets lives, resources and game speed to the current level's starting values.</summary>
    public void ResetGameState()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.CurrentLevel == null) // kung walang LevelManager o walang current level
        {
            Debug.LogWarning("GameManager: ResetGameState called but LevelManager or CurrentLevel is null. Skipping."); // mag-warning
            return; // wag mag-reset
        }

        _lives = LevelManager.Instance.CurrentLevel.startingLives; // i-reset yung lives base sa level
        OnLivesChanged?.Invoke(_lives); // i-trigger yung event
        _resources = LevelManager.Instance.CurrentLevel.startingResources; // i-reset yung resources base sa level
        OnResourcesChanged?.Invoke(_resources); // i-trigger yung event

        SetGameSpeed(1f); // i-reset yung game speed sa normal
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null) // kung may LevelManager at may current level
            ResetGameState(); // i-reset yung game state pag nag-load ng bagong scene
    }
}