using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;

    public static event Action<EnemyData> OnEnemyReachedEnd;
    public static event Action<Enemy> OnEnemyDestroyed;

    private Path _currentPath;

    private Vector3 _targetPosition;
    private int _currentWaypoint;
    private float _lives;
    private float _maxLives;
    private float _currentSpeed;
    private float _currentReward;

    // Exposed so Tower can determine which enemy is furthest along the path
    public int WaypointIndex => _currentWaypoint;
    public float DistanceToNextWaypoint => Vector3.Distance(transform.position, _targetPosition);

    [SerializeField] private Transform healthBar;
    private Vector3 _healthBarOriginalScale;

    private bool _hasBeenCounted = false;

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"Enemy '{gameObject.name}': EnemyData is not assigned! Assign it in the Inspector.");
            enabled = false;
            return;
        }

        // Find any Path component in the scene, regardless of object name or tag
        _currentPath = FindFirstObjectByType<Path>();

        if (_currentPath == null)
        {
            Debug.LogError("Enemy: No Path component found in the scene! Make sure a Path component exists.");
            enabled = false;
            return;
        }

        if (healthBar != null)
        {
            _healthBarOriginalScale = healthBar.localScale;
        }
        else
        {
            Debug.LogWarning($"Enemy '{gameObject.name}': Health bar Transform not assigned.");
        }
    }

    private void OnEnable()
    {
        if (_currentPath != null)
        {
            _currentWaypoint = 0;
            _targetPosition = _currentPath.GetPosition(_currentWaypoint);
        }
    }

    void Update()
    {
        if (_hasBeenCounted || _currentPath == null)
            return;

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _currentSpeed * Time.deltaTime);

        float relativeDistance = (transform.position - _targetPosition).magnitude;

        if (relativeDistance < 0.1f)
        {
            if (_currentWaypoint < _currentPath.Waypoints.Length - 1)
            {
                _currentWaypoint++;
                _targetPosition = _currentPath.GetPosition(_currentWaypoint);
            }
            else
            {
                _hasBeenCounted = true;
                OnEnemyReachedEnd?.Invoke(data);
                gameObject.SetActive(false);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (_hasBeenCounted)
            return;

        _lives -= damage;
        _lives = Mathf.Max(_lives, 0);
        UpdateHealthBar();

        if (_lives <= 0)
        {
            _hasBeenCounted = true;
            OnEnemyDestroyed?.Invoke(this);
            gameObject.SetActive(false);
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null)
            return;

        float healthPercent = _maxLives > 0 ? _lives / _maxLives : 0f;
        Vector3 scale = _healthBarOriginalScale;
        scale.x = _healthBarOriginalScale.x * healthPercent;
        healthBar.localScale = scale;
    }

    public void Initialize(float healthMultiplier, float speedMultiplier, float rewardMultiplier)
    {
        if (data == null)
        {
            Debug.LogError("Enemy: EnemyData is null! Cannot initialize.");
            return;
        }

        _hasBeenCounted = false;
        _maxLives = data.lives * healthMultiplier;
        _lives = _maxLives;
        _currentSpeed = data.speed * speedMultiplier;
        _currentReward = data.resourceReward * rewardMultiplier;
        UpdateHealthBar();
    }

    public float GetCurrentReward()
    {
        return _currentReward;
    }
}
