using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [SerializeField] private TowerData data;
    [SerializeField] private CircleCollider2D rangeCollider;

    [Header("Projectile Spawn")]
    [Tooltip("Drag the SpawnPoint child object here to control where projectiles come from")]
    [SerializeField] private Transform spawnPoint;

    private List<Enemy> _enemiesInRange = new List<Enemy>();
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
        if (data == null)
            return;

        _shootTimer -= Time.deltaTime;

        if (_shootTimer <= 0)
        {
            _shootTimer = data.shootInterval;
            Shoot();
        }
    }

    private Vector3 GetSpawnPosition()
    {
        // Use the assigned spawn point if available, otherwise use tower center
        return spawnPoint != null ? spawnPoint.position : transform.position;
    }

    private void Shoot()
    {
        if (_projectilePool == null || _enemiesInRange == null || _enemiesInRange.Count == 0)
            return;

        _enemiesInRange.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);

        if (_enemiesInRange.Count > 0)
        {
            GameObject projectile = _projectilePool.GetPooledObject();
            if (projectile != null)
            {
                Vector3 spawnPos = GetSpawnPosition();
                projectile.transform.position = spawnPos;

                Vector2 direction = (_enemiesInRange[0].transform.position - spawnPos).normalized;

                Projectile proj = projectile.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Shoot(data, direction);
                    projectile.SetActive(true);
                }
            }
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
        if (_enemiesInRange != null)
            _enemiesInRange.Remove(enemy);
    }

    private void OnDrawGizmos()
    {
        // Always visible yellow sphere showing spawn point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetSpawnPosition(), 0.12f);
        Gizmos.DrawLine(transform.position, GetSpawnPosition());
    }

    private void OnDrawGizmosSelected()
    {
        // Range circle when selected
        if (data != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.range);
        }

        // Solid spawn point when selected
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetSpawnPosition(), 0.15f);
    }
}
