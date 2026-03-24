using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [SerializeField] private GameObject prefab; // yung prefab na gagawing pool (kalaban o projectile)
    [SerializeField] private int poolSize = 5; // ilang instance ang gagawin sa pool
    private List<GameObject> _pool; // listahan ng mga naka-pool na objects
    private bool _initialized; // flag kung na-initialize na yung pool (para hindi ma-initialize ulit)

    private void Start()
    {
        // Only auto-initialize when set up manually in the Inspector.
        // Skip if Spawner already called Initialize() at runtime.
        if (!_initialized && prefab != null) // kung hindi pa initialized at may prefab
            Initialize(prefab, poolSize); // i-initialize yung pool
    }

    /// <summary>Initializes the pool with the given prefab and size. Called by Spawner at runtime.</summary>
    public void Initialize(GameObject enemyPrefab, int size)
    {
        if (_initialized) // kung na-initialize na
            return; // wag na ulit

        _initialized = true; // markahan na initialized na
        prefab = enemyPrefab; // i-save yung prefab
        poolSize = size; // i-save yung pool size

        _pool = new List<GameObject>(); // gumawa ng bagong listahan
        for (int i = 0; i < poolSize; i++) // mag-loop base sa pool size
        {
            CreateNewObject(); // gumawa ng bagong object at idagdag sa pool
        }
    }

    private GameObject CreateNewObject()
    {
        if (prefab == null) // kung walang prefab
        {
            Debug.LogError($"ObjectPooler on '{gameObject.name}': Cannot create object, prefab is null!"); // mag-error
            return null; // walang maibalik
        }

        GameObject obj = Instantiate(prefab); // gumawa ng bagong instance ng prefab
        obj.transform.SetParent(null); // i-set yung parent sa null (hindi anak ng pooler object)
        obj.SetActive(false); // i-disable muna (inactive)
        _pool.Add(obj); // idagdag sa listahan
        return obj; // ibalik yung ginawang object
    }

    /// <summary>Returns an inactive pooled object, or creates a new one if none are available.</summary>
    public GameObject GetPooledObject()
    {
        if (_pool == null) // kung walang pool
        {
            Debug.LogError($"ObjectPooler on '{gameObject.name}': Pool is not initialized!"); // mag-error
            return null; // walang maibalik
        }

        foreach (GameObject obj in _pool) // dumaan sa bawat object sa pool
        {
            if (obj != null && !obj.activeSelf) // kung may object at hindi active (nakatago)
                return obj; // ibalik yun (reuse)
        }

        return CreateNewObject(); // kung lahat active, gumawa ng bago (expand yung pool)
    }
}