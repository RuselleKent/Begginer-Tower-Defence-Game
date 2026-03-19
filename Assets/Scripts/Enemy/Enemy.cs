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
    private bool _hasBeenCounted;

    public int WaypointIndex => _currentWaypoint;

    /// <summary>Squared distance to the next waypoint — avoids Sqrt, safe for relative comparisons.</summary>
    public float SqrDistanceToNextWaypoint => (transform.position - _targetPosition).sqrMagnitude;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Health Bar")]
    [SerializeField] private GameObject healthBarRoot;
    [SerializeField] private Transform healthBar;
    private Vector3 _healthBarOriginalScale;
    private bool _damageTaken;

    [Header("Armor Bar")]
    [SerializeField] private GameObject armorBarRoot;
    [SerializeField] private Transform armorBar;
    private Vector3 _armorBarOriginalScale;
    private float _armor;
    private float _maxArmor;

    [Header("Armor Visuals")]
    [Tooltip("Assign only on enemies that have armor break animations. Leave empty for normal enemies.")]
    [SerializeField] private Animator armorAnimator;

    private static readonly int ArmorBrokenParam = Animator.StringToHash("ArmorBroken");

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"Enemy '{gameObject.name}': EnemyData is not assigned!");
            enabled = false;
            return;
        }

        _currentPath = FindFirstObjectByType<Path>();

        if (_currentPath == null)
        {
            Debug.LogError("Enemy: No Path component found in the scene!");
            enabled = false;
            return;
        }

        if (healthBar != null)
            _healthBarOriginalScale = healthBar.localScale;
        else
            Debug.LogWarning($"Enemy '{gameObject.name}': Health bar Transform not assigned.");

        if (armorBar != null)
            _armorBarOriginalScale = armorBar.localScale;
    }

    private void OnEnable()
    {
        if (_currentPath != null)
        {
            _currentWaypoint = 0;
            _targetPosition = _currentPath.GetPosition(_currentWaypoint);
        }
    }

    private void Update()
    {
        if (_hasBeenCounted || _currentPath == null)
            return;

        Vector3 moveDirection = (_targetPosition - transform.position).normalized;
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _currentSpeed * Time.deltaTime);
        UpdateFacingDirection(moveDirection);

        if ((transform.position - _targetPosition).sqrMagnitude < 0.01f)
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

    /// <summary>
    /// Applies damage to the enemy. Armor absorbs damage first.
    /// Any overflow after armor breaks carries into health.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_hasBeenCounted)
            return;

        if (_armor > 0f)
        {
            _armor -= damage;

            if (_armor <= 0f)
            {
                float overflow = -_armor;
                _armor = 0f;

                if (armorBarRoot != null)
                    armorBarRoot.SetActive(false);

                UpdateArmorBar();

                if (armorAnimator != null)
                    armorAnimator.SetTrigger(ArmorBrokenParam);

                if (overflow > 0f)
                    ApplyHealthDamage(overflow);
            }
            else
            {
                UpdateArmorBar();
            }

            return;
        }

        ApplyHealthDamage(damage);
    }

    private void ApplyHealthDamage(float damage)
    {
        if (!_damageTaken)
        {
            _damageTaken = true;
            if (healthBarRoot != null)
                healthBarRoot.SetActive(true);
        }

        _lives -= damage;
        _lives = Mathf.Max(_lives, 0f);
        UpdateHealthBar();

        if (_lives <= 0f)
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

        float percent = _maxLives > 0f ? _lives / _maxLives : 0f;
        Vector3 scale = _healthBarOriginalScale;
        scale.x = _healthBarOriginalScale.x * percent;
        healthBar.localScale = scale;
    }

    private void UpdateArmorBar()
    {
        if (armorBar == null || _maxArmor <= 0f)
            return;

        float percent = _armor / _maxArmor;
        Vector3 scale = _armorBarOriginalScale;
        scale.x = _armorBarOriginalScale.x * percent;
        armorBar.localScale = scale;
    }

    /// <summary>Flips the sprite horizontally based on horizontal movement direction.</summary>
    private void UpdateFacingDirection(Vector3 moveDirection)
    {
        if (spriteRenderer == null)
            return;

        if (moveDirection.x < 0f)
            spriteRenderer.flipX = true;
        else if (moveDirection.x > 0f)
            spriteRenderer.flipX = false;
    }

    /// <summary>Initializes the enemy stats, health, and armor.</summary>
    public void Initialize(float healthMultiplier, float speedMultiplier, float rewardMultiplier, float armorMultiplier = 1f)
    {
        if (data == null)
        {
            Debug.LogError("Enemy: EnemyData is null! Cannot initialize.");
            return;
        }

        _hasBeenCounted = false;
        _damageTaken = false;

        _maxLives = data.lives * healthMultiplier;
        _lives = _maxLives;
        _currentSpeed = data.speed * speedMultiplier;
        _currentReward = data.resourceReward * rewardMultiplier;

        _maxArmor = data.armor * armorMultiplier;
        _armor = _maxArmor;

        if (healthBarRoot != null)
            healthBarRoot.SetActive(false);

        if (armorBarRoot != null)
            armorBarRoot.SetActive(_maxArmor > 0f);

        UpdateHealthBar();
        UpdateArmorBar();
    }

    public float GetCurrentReward() => _currentReward;
}
