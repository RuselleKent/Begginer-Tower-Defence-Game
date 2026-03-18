using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [SerializeField] private TowerData data;
    [SerializeField] private CircleCollider2D rangeCollider;

    [Header("Projectile Spawn")]
    [Tooltip("Drag the SpawnPoint child object here to control where projectiles come from")]
    [SerializeField] private Transform spawnPoint;

    private readonly List<Enemy> _enemiesInRange = new List<Enemy>();
    private ObjectPooler _projectilePool;
    private float _shootTimer;

    private void OnEnable()
    {
        Enemy.OnEnemyDestroyed += HandleEnemyDestroyed;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDestroyed -= HandleEnemyDestroyed;
    }

    private void Start()
    {
        if (data == null)
        {
            Debug.LogError($"Tower '{gameObject.name}': TowerData is null!");
            enabled = false;
            return;
        }

        if (rangeCollider == null)
            rangeCollider = GetComponent<CircleCollider2D>();

        if (rangeCollider != null)
        {
            rangeCollider.radius = data.range;
            rangeCollider.isTrigger = true;
        }

        _projectilePool = GetComponent<ObjectPooler>();
        _shootTimer = data.shootInterval;
    }

    private void Update()
    {
        if (data == null || _enemiesInRange.Count == 0)
            return;

        _shootTimer -= Time.deltaTime;

        if (_shootTimer <= 0f)
        {
            _shootTimer = data.shootInterval;
            Shoot();
        }
    }

    private Vector3 GetSpawnPosition()
    {
        return spawnPoint != null ? spawnPoint.position : transform.position;
    }

    /// <summary>
    /// Returns the enemy furthest along the path — highest waypoint index,
    /// then shortest sqr distance to the next waypoint.
    /// Uses sqrMagnitude to avoid a Sqrt call per enemy per shot.
    /// </summary>
    private Enemy GetPriorityTarget()
    {
        Enemy target = null;
        int highestWaypoint = -1;
        float shortestSqrDistance = float.MaxValue;

        foreach (Enemy enemy in _enemiesInRange)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            bool isFurtherWaypoint = enemy.WaypointIndex > highestWaypoint;
            bool isSameWaypointButCloser = enemy.WaypointIndex == highestWaypoint
                                           && enemy.SqrDistanceToNextWaypoint < shortestSqrDistance;

            if (isFurtherWaypoint || isSameWaypointButCloser)
            {
                highestWaypoint = enemy.WaypointIndex;
                shortestSqrDistance = enemy.SqrDistanceToNextWaypoint;
                target = enemy;
            }
        }

        return target;
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
            projectile.SetActive(true);
        }
    }

    /// <summary>Removes null or inactive enemies without a lambda allocation.</summary>
    private void CleanEnemiesInRange()
    {
        for (int i = _enemiesInRange.Count - 1; i >= 0; i--)
        {
            if (_enemiesInRange[i] == null || !_enemiesInRange[i].gameObject.activeInHierarchy)
                _enemiesInRange.RemoveAt(i);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(GameConstants.TAG_ENEMY))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null && !_enemiesInRange.Contains(enemy))
                _enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag(GameConstants.TAG_ENEMY))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
                _enemiesInRange.Remove(enemy);
        }
    }

    private void HandleEnemyDestroyed(Enemy enemy)
    {
        _enemiesInRange.Remove(enemy);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetSpawnPosition(), 0.12f);
        Gizmos.DrawLine(transform.position, GetSpawnPosition());
    }

    private void OnDrawGizmosSelected()
    {
        if (data != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.range);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetSpawnPosition(), 0.15f);
    }
}
