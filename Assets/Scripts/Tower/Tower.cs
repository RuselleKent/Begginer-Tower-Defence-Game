using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [SerializeField] private TowerData data; // data ng tower (range, damage, shoot interval, etc.) - i-drag sa inspector
    [SerializeField] private CircleCollider2D rangeCollider; // collider para ma-detect kung may kalaban sa range

    [Header("Projectile Spawn")]
    [Tooltip("Drag the SpawnPoint child object here to control where projectiles come from")]
    [SerializeField] private Transform spawnPoint; // kung saan lumalabas yung projectile (para hindi sa gitna ng tower)

    private readonly List<Enemy> _enemiesInRange = new List<Enemy>(); // listahan ng mga kalaban na nasa range
    private ObjectPooler _projectilePool; // object pooler para sa projectiles (para hindi lagi nag-i-instantiate)
    private float _shootTimer; // timer para sa pagitan ng putok

    private void OnEnable()
    {
        Enemy.OnEnemyDestroyed += HandleEnemyDestroyed; // mag-subscribe sa event para malaman kung may namatay na kalaban
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDestroyed -= HandleEnemyDestroyed; // mag-unsubscribe para iwas memory leak
    }

    private void Start()
    {
        if (data == null) // kung walang tower data
        {
            Debug.LogError($"Tower '{gameObject.name}': TowerData is null!"); // mag-error
            enabled = false; // i-disable yung script
            return; // tapos na
        }

        if (rangeCollider == null) // kung walang range collider na na-assign
            rangeCollider = GetComponent<CircleCollider2D>(); // kunin yung CircleCollider2D sa tower

        if (rangeCollider != null) // kung may collider
        {
            rangeCollider.radius = data.range; // i-set yung radius base sa data
            rangeCollider.isTrigger = true; // gawing trigger para hindi mag-collide physically
        }

        _projectilePool = GetComponent<ObjectPooler>(); // kunin yung object pooler component
        _shootTimer = data.shootInterval; // i-set yung timer sa shoot interval (para sa unang putok)
    }

    private void Update()
    {
        if (data == null || _enemiesInRange.Count == 0) // kung walang data o walang kalaban sa range
            return; // wag mag-shoot

        _shootTimer -= Time.deltaTime; // bawas ng timer bawat frame

        if (_shootTimer <= 0f) // kung oras na para pumutok
        {
            _shootTimer = data.shootInterval; // i-reset yung timer
            Shoot(); // pumutok
        }
    }

    private Vector3 GetSpawnPosition()
    {
        return spawnPoint != null ? spawnPoint.position : transform.position; // kung may spawn point, yun ang gamitin, kung wala, yung position ng tower
    }

    /// <summary>
    /// Returns the enemy furthest along the path — highest waypoint index,
    /// then shortest sqr distance to the next waypoint.
    /// Uses sqrMagnitude to avoid a Sqrt call per enemy per shot.
    /// </summary>
    private Enemy GetPriorityTarget()
    {
        Enemy target = null; // yung target na pipiliin
        int highestWaypoint = -1; // pinakamataas na waypoint index
        float shortestSqrDistance = float.MaxValue; // pinakamaikling distance squared papuntang next waypoint

        foreach (Enemy enemy in _enemiesInRange) // dumaan sa bawat kalaban sa range
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) // kung patay na o inactive
                continue; // skip

            bool isFurtherWaypoint = enemy.WaypointIndex > highestWaypoint; // tseke kung mas malayo na yung kalaban (mas mataas na waypoint)
            bool isSameWaypointButCloser = enemy.WaypointIndex == highestWaypoint // kung same waypoint
                                           && enemy.SqrDistanceToNextWaypoint < shortestSqrDistance; // pero mas malapit sa next waypoint

            if (isFurtherWaypoint || isSameWaypointButCloser) // kung mas malayo o same pero mas malapit
            {
                highestWaypoint = enemy.WaypointIndex; // i-update yung highest waypoint
                shortestSqrDistance = enemy.SqrDistanceToNextWaypoint; // i-update yung shortest distance
                target = enemy; // i-set yung target
            }
        }

        return target; // ibalik yung napiling target (o null kung wala)
    }

    private void Shoot()
{
    if (_projectilePool == null || _enemiesInRange.Count == 0)
        return;

    CleanEnemiesInRange();

    Enemy priorityTarget = GetPriorityTarget();
    if (priorityTarget == null)
        return;

    GameObject projectile = _projectilePool.GetPooledObject();
    if (projectile == null)
        return;

    Vector3 spawnPos = GetSpawnPosition();
    projectile.transform.position = spawnPos;

    Vector2 direction = (priorityTarget.transform.position - spawnPos).normalized;

    Projectile proj = projectile.GetComponent<Projectile>();
    if (proj != null)
    {
        proj.Shoot(data, direction);
        // Play the shoot sound for this tower type.
        AudioManager.Instance?.PlayTowerShoot(data.displayName);
        projectile.SetActive(true);
    }
}


    /// <summary>Removes null or inactive enemies without a lambda allocation.</summary>
    private void CleanEnemiesInRange()
    {
        for (int i = _enemiesInRange.Count - 1; i >= 0; i--) // dumaan sa listahan mula sa dulo pababa (para safe mag-remove)
        {
            if (_enemiesInRange[i] == null || !_enemiesInRange[i].gameObject.activeInHierarchy) // kung patay na o inactive
                _enemiesInRange.RemoveAt(i); // alisin sa listahan
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(GameConstants.TAG_ENEMY)) // kung kalaban yung pumasok sa range
        {
            Enemy enemy = collision.GetComponent<Enemy>(); // kunin yung Enemy component
            if (enemy != null && !_enemiesInRange.Contains(enemy)) // kung may enemy at wala pa sa listahan
                _enemiesInRange.Add(enemy); // idagdag sa listahan
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(GameConstants.TAG_ENEMY)) // kung kalaban yung lumabas sa range
        {
            Enemy enemy = collision.GetComponent<Enemy>(); // kunin yung Enemy component
            if (enemy != null) // kung may enemy
                _enemiesInRange.Remove(enemy); // alisin sa listahan
        }
    }

    private void HandleEnemyDestroyed(Enemy enemy)
    {
        _enemiesInRange.Remove(enemy); // tanggalin yung enemy sa listahan kapag namatay (para hindi na i-target)
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow; // kulay dilaw
        Gizmos.DrawWireSphere(GetSpawnPosition(), 0.12f); // mag-drawing ng wire sphere sa spawn point (para makita sa editor)
        Gizmos.DrawLine(transform.position, GetSpawnPosition()); // mag-drawing ng linya mula tower papuntang spawn point
    }

    private void OnDrawGizmosSelected()
    {
        if (data != null) // kung may data
        {
            Gizmos.color = Color.red; // kulay pula
            Gizmos.DrawWireSphere(transform.position, data.range); // mag-drawing ng wire sphere para sa range (kapag napili yung tower)
        }

        Gizmos.color = Color.green; // kulay berde
        Gizmos.DrawSphere(GetSpawnPosition(), 0.15f); // mag-drawing ng solid sphere sa spawn point (kapag napili yung tower)
    }
}