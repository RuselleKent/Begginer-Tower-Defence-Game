using UnityEngine;

public class Projectile : MonoBehaviour
{
    private TowerData _data;
    private Vector2 _direction;
    private float _lifetime;

    void Update()
    {
        if (_data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _lifetime -= Time.deltaTime;
        
        if (_lifetime <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position += (Vector3)_direction * _data.projectileSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(GameConstants.TAG_ENEMY))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(_data.damage);
            }
            gameObject.SetActive(false);
        }
    }

    public void Shoot(TowerData data, Vector2 direction)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _data = data;
        _direction = direction;
        _lifetime = _data.projectileDuration;
        
        transform.localScale = Vector3.one * _data.projectileSize;
    }
}
