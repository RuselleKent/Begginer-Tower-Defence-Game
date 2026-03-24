using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data; // Serialized field para ma-assign sa inspector yung enemy data (stats ng kalaban)
    public EnemyData Data => data; // Public property para ma-access ng ibang scripts yung enemy data (read-only)

    public static event Action<EnemyData> OnEnemyReachedEnd; // Static event na nagfa-fire pag may kalaban na nakarating sa dulo ng path
    public static event Action<Enemy> OnEnemyDestroyed; // Static event na nagfa-fire pag may kalaban na nasira/namatay

    private Path _currentPath; // Reference sa Path component (yung sinusundan na daan)
    private Vector3 _targetPosition; // Position ng next waypoint na pupuntahan
    private int _currentWaypoint; // Index ng current waypoint (kung saan papunta)
    private float _lives; // Current health points ng kalaban
    private float _maxLives; // Maximum health points (base lives * health multiplier)
    private float _currentSpeed; // Current movement speed (base speed * speed multiplier)
    private float _currentReward; // Current reward na ibibigay pag namatay (base reward * reward multiplier)
    private bool _hasBeenCounted; // Flag para maiwasan multiple counting pag na-reach end or namatay

    public int WaypointIndex => _currentWaypoint; // Public property para makuha current waypoint index

    /// <summary>Squared distance to the next waypoint — avoids Sqrt, safe for relative comparisons.</summary>
    public float SqrDistanceToNextWaypoint => (transform.position - _targetPosition).sqrMagnitude; // Distance squared papuntang next waypoint (mas mabilis kesa mag-Sqrt)

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer; // SpriteRenderer para sa visual ng enemy (pang-flip ng direction)

    [Header("Health Bar")]
    [SerializeField] private GameObject healthBarRoot; // Parent GameObject ng health bar (pwedeng i-enable/disable)
    [SerializeField] private Transform healthBar; // Transform ng health bar mismo (para i-scale based sa remaining health)
    private Vector3 _healthBarOriginalScale; // Original scale ng health bar (para i-base sa original size)
    private bool _damageTaken; // Flag kung naka-take na ng damage (para i-show yung health bar)

    [Header("Armor Bar")]
    [SerializeField] private GameObject armorBarRoot; // Parent GameObject ng armor bar (pwedeng i-enable/disable)
    [SerializeField] private Transform armorBar; // Transform ng armor bar (para i-scale based sa remaining armor)
    private Vector3 _armorBarOriginalScale; // Original scale ng armor bar
    private float _armor; // Current armor points ng kalaban
    private float _maxArmor; // Maximum armor points (base armor * armor multiplier)

    [Header("Armor Visuals")]
    [Tooltip("Assign only on enemies that have armor break animations. Leave empty for normal enemies.")]
    [SerializeField] private Animator armorAnimator; // Animator para sa armor break animation (optional)

    private static readonly int ArmorBrokenParam = Animator.StringToHash("ArmorBroken"); // Hash value ng "ArmorBroken" parameter (mas mabilis kesa string)

    private void Awake()
    {
        if (data == null) // Check kung may assigned na EnemyData
        {
            Debug.LogError($"Enemy '{gameObject.name}': EnemyData is not assigned!"); // Mag-error kung wala
            enabled = false; // I-disable yung script para hindi mag-run
            return;
        }

        _currentPath = FindFirstObjectByType<Path>(); // Hanapin yung Path component sa scene (first instance)

        if (_currentPath == null) // Check kung may nahanap na Path
        {
            Debug.LogError("Enemy: No Path component found in the scene!"); // Mag-error kung wala
            enabled = false; // I-disable yung script
            return;
        }

        if (healthBar != null) // Check kung may assigned health bar
            _healthBarOriginalScale = healthBar.localScale; // I-store yung original scale ng health bar
        else
            Debug.LogWarning($"Enemy '{gameObject.name}': Health bar Transform not assigned."); // Warning kung walang health bar

        if (armorBar != null) // Check kung may assigned armor bar
            _armorBarOriginalScale = armorBar.localScale; // I-store yung original scale ng armor bar
    }

    private void OnEnable()
    {
        // Waypoint and target are reset inside Initialize() for pooled enemies.
        // This guard handles the rare case of re-enabling before Initialize is called.
        if (_currentPath != null && _currentWaypoint == 0) // Kung may path at nasa start pa yung waypoint
            _targetPosition = _currentPath.GetPosition(0); // I-set yung target position sa first waypoint
    }

    private void Update()
    {
        if (_hasBeenCounted || _currentPath == null) // Kung na-count na or walang path, wag mag-move
            return;

        Vector3 moveDirection = (_targetPosition - transform.position).normalized; // Compute direction papuntang target (normalized para constant speed)
        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _currentSpeed * Time.deltaTime); // I-move yung enemy papuntang target (frame-rate independent)
        UpdateFacingDirection(moveDirection); // I-flip yung sprite based sa direction

        if ((transform.position - _targetPosition).sqrMagnitude < 0.01f) // Kung malapit na sa target (within 0.01 units)
        {
            if (_currentWaypoint < _currentPath.Waypoints.Length - 1) // Kung hindi pa last waypoint
            {
                _currentWaypoint++; // Increase waypoint index
                _targetPosition = _currentPath.GetPosition(_currentWaypoint); // I-set yung next target position
            }
            else // Kung last waypoint na
            {
                _hasBeenCounted = true; // Mark as counted para hindi ma-trigger ulit
                OnEnemyReachedEnd?.Invoke(data); // I-trigger yung event na may kalaban na nakarating sa dulo
                gameObject.SetActive(false); // I-disable yung GameObject (return to pool or destroy)
            }
        }
    }

    /// <summary>
    /// Applies damage to the enemy. Armor absorbs damage first.
    /// Any overflow after armor breaks carries into health.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_hasBeenCounted) // Kung na-count na (dead or reached end), wag mag-take ng damage
            return;

        if (_armor > 0f) // Kung may armor pa
        {
            _armor -= damage; // Bawasan yung armor ng damage amount

            if (_armor <= 0f) // Kung naubos yung armor
            {
                float overflow = -_armor; // Compute yung excess damage na lumampas sa armor
                _armor = 0f; // I-set sa 0 yung armor

                if (armorBarRoot != null) // Kung may armor bar root
                    armorBarRoot.SetActive(false); // I-hide yung armor bar (since wala nang armor)

                UpdateArmorBar(); // I-update yung armor bar visual (magiging zero)

                if (armorAnimator != null) // Kung may animator para sa armor
                    armorAnimator.SetTrigger(ArmorBrokenParam); // I-play yung armor break animation

                if (overflow > 0f) // Kung may excess damage
                    ApplyHealthDamage(overflow); // I-apply yung excess sa health
            }
            else // Kung may natirang armor
            {
                UpdateArmorBar(); // I-update lang yung armor bar visual
            }

            return; // Tapos na (damage handled by armor)
        }

        ApplyHealthDamage(damage); // Walang armor, direct sa health yung damage
    }

    private void ApplyHealthDamage(float damage)
    {
        if (!_damageTaken) // Kung first time mag-take ng damage
        {
            _damageTaken = true; // Mark na may damage na
            if (healthBarRoot != null) // Kung may health bar root
                healthBarRoot.SetActive(true); // I-show yung health bar (mag-aappear lang pag may damage)
        }

        _lives -= damage; // Bawasan yung health
        _lives = Mathf.Max(_lives, 0f); // I-clamp para hindi negative
        UpdateHealthBar(); // I-update yung health bar visual

        if (_lives <= 0f) // Kung wala nang health
        {
            _hasBeenCounted = true; // Mark as counted
            OnEnemyDestroyed?.Invoke(this); // I-trigger yung event na may kalaban na namatay
            gameObject.SetActive(false); // I-disable yung GameObject (return to pool or destroy)
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null) // Kung walang health bar reference, wag mag-update
            return;

        float percent = _maxLives > 0f ? _lives / _maxLives : 0f; // Compute percentage ng remaining health
        Vector3 scale = _healthBarOriginalScale; // Kunin yung original scale
        scale.x = _healthBarOriginalScale.x * percent; // I-scale yung X axis based sa percentage
        healthBar.localScale = scale; // I-apply yung bagong scale
    }

    private void UpdateArmorBar()
    {
        if (armorBar == null || _maxArmor <= 0f) // Kung walang armor bar or walang max armor, wag mag-update
            return;

        float percent = _armor / _maxArmor; // Compute percentage ng remaining armor
        Vector3 scale = _armorBarOriginalScale; // Kunin yung original scale
        scale.x = _armorBarOriginalScale.x * percent; // I-scale yung X axis based sa percentage
        armorBar.localScale = scale; // I-apply yung bagong scale
    }

    /// <summary>Flips the sprite horizontally based on horizontal movement direction.</summary>
    private void UpdateFacingDirection(Vector3 moveDirection)
    {
        if (spriteRenderer == null) // Kung walang sprite renderer, wag mag-flip
            return;

        if (moveDirection.x < 0f) // Kung pa-kaliwa yung direction
            spriteRenderer.flipX = true; // I-flip horizontally (nakaharap sa kaliwa)
        else if (moveDirection.x > 0f) // Kung pa-kanan yung direction
            spriteRenderer.flipX = false; // Wag i-flip (nakaharap sa kanan)
        // Note: Kung walang horizontal movement (x = 0), maintain yung current facing direction
    }

    /// <summary>Initializes the enemy stats, health, armor, and resets path progress.</summary>
    public void Initialize(float healthMultiplier, float speedMultiplier, float rewardMultiplier, float armorMultiplier = 1f)
    {
        if (data == null) // Check kung may data
        {
            Debug.LogError("Enemy: EnemyData is null! Cannot initialize."); // Mag-error kung wala
            return;
        }

        _hasBeenCounted = false; // I-reset yung counted flag (para sa pooling)
        _damageTaken = false; // I-reset yung damage taken flag

        // Reset path progress so pooled enemies always start from waypoint 0.
        _currentWaypoint = 0; // I-reset sa first waypoint
        if (_currentPath != null) // Kung may path
            _targetPosition = _currentPath.GetPosition(0); // I-set yung target sa first waypoint

        _maxLives = data.lives * healthMultiplier; // Compute max lives based sa base lives at multiplier
        _lives = _maxLives; // I-set yung current lives sa max
        _currentSpeed = data.speed * speedMultiplier; // Compute speed based sa base speed at multiplier
        _currentReward = data.resourceReward * rewardMultiplier; // Compute reward based sa base reward at multiplier

        _maxArmor = data.armor * armorMultiplier; // Compute max armor based sa base armor at multiplier
        _armor = _maxArmor; // I-set yung current armor sa max

        if (healthBarRoot != null) // Kung may health bar root
            healthBarRoot.SetActive(false); // I-hide muna (mag-aappear lang pag may damage)

        if (armorBarRoot != null) // Kung may armor bar root
            armorBarRoot.SetActive(_maxArmor > 0f); // I-show lang kung may armor (kung zero, nakatago)

        UpdateHealthBar(); // I-update health bar visual (magiging full)
        UpdateArmorBar(); // I-update armor bar visual (magiging full kung may armor)
    }

    public float GetCurrentReward() => _currentReward; // Public method para makuha yung reward na ibibigay pag namatay ang kalaban
}