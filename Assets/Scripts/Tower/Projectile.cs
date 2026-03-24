using UnityEngine;

public class Projectile : MonoBehaviour
{
    private TowerData _data; // data ng tower na nagpaputok (damage, speed, etc.)
    private Vector2 _direction; // direksyon kung saan lilipad yung projectile
    private float _lifetime; // ilang seconds pa bago mawala yung projectile (kung hindi tumama)
    private bool _hasHit; // flag kung tumama na para hindi na tumama ulit

    private void OnEnable()
    {
        _hasHit = false; // i-reset yung hit flag tuwing nagagamit ulit (para sa pooling)
    }

    private void Update()
    {
        if (_data == null) // kung walang data (ibig sabihin hindi pa na-shoot)
        {
            gameObject.SetActive(false); // patayin yung projectile
            return; // tapos na
        }

        _lifetime -= Time.deltaTime; // bawas ng oras bawat frame

        if (_lifetime <= 0) // kung naubos na yung oras (walang tumama)
        {
            gameObject.SetActive(false); // patayin yung projectile
            return; // tapos na
        }

        transform.position += (Vector3)_direction * _data.projectileSpeed * Time.deltaTime; // ilipat yung projectile base sa direksyon at speed
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasHit) // kung tumama na dati
            return; // wag na ulit mag-process

        if (collision.CompareTag(GameConstants.TAG_ENEMY)) // kung kalaban yung nasagi
        {
            Enemy enemy = collision.GetComponent<Enemy>(); // kunin yung Enemy component
            if (enemy != null) // kung may enemy
            {
                _hasHit = true; // markahan na tumama na
                enemy.TakeDamage(_data.damage); // i-damage yung kalaban
            }
            gameObject.SetActive(false); // patayin yung projectile (kahit tumama o hindi)
        }
    }

    /// <summary>Sets the projectile's data and direction, then arms it for flight.</summary>
    public void Shoot(TowerData data, Vector2 direction)
    {
        if (data == null) // kung walang data
        {
            gameObject.SetActive(false); // patayin yung projectile
            return; // tapos na
        }

        _data = data; // i-save yung tower data
        _direction = direction; // i-save yung direksyon
        _lifetime = _data.projectileDuration; // i-set yung lifetime galing sa tower data

        transform.localScale = Vector3.one * _data.projectileSize; // i-set yung laki ng projectile
    }
}