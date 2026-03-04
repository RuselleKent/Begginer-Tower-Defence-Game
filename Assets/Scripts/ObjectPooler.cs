using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 5;
    private List<GameObject> _pool;

    void Start()
    {
        if (prefab == null)
        {
            Debug.LogError($"ObjectPooler on '{gameObject.name}': Prefab is not assigned! Assign it in the Inspector.");
            enabled = false;
            return;
        }

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
            {
                return obj;
            }
        }

        return CreateNewObject();
    }
}
