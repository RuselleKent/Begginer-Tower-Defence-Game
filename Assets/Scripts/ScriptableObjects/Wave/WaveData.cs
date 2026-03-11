using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Scriptable Objects/WaveData")]
public class WaveData : ScriptableObject
{
    public ObjectPooler enemyPool;
    public float spawnInterval;
    public int enemiesPerWave;
}
