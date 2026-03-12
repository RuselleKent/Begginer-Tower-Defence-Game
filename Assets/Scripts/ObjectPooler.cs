using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 5;
    private List<GameObject> _pool;

    private void Start()
    {
        // Only auto-initialize when set up manually in the Inspector
        if (prefab != null)
            Initialize(prefab, poolSize);
    }

    /// <summary>Initializes the pool with the given prefab and size. Called by Spawner at runtime.</summary>
    public void Initialize(GameObject enemyPrefab, int size)
    {
        prefab = enemyPrefab;
        poolSize = size;

        _pool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewObject();
        }
    }

    private GameObject CreateNewObject()
    {
        if (prefab == null)
        {
            Debug.LogError($"ObjectPooler on '{gameObject.name}': Cannot create object, prefab is null!");
            return null;
        }

        GameObject obj = Instantiate(prefab);
        obj.transform.SetParent(null);
        obj.SetActive(false);
        _pool.Add(obj);
        return obj;
    }

    /// <summary>Returns an inactive pooled object, or creates a new one if none are available.</summary>
    public GameObject GetPooledObject()
    {
        if (_pool == null)
        {
            Debug.LogError($"ObjectPooler on '{gameObject.name}': Pool is not initialized!");
            return null;
        }

        foreach (GameObject obj in _pool)
        {
            if (obj != null && !obj.activeSelf)
                return obj;
        }

        return CreateNewObject();
    }
}
