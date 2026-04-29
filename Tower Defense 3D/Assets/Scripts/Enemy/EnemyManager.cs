using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct WaveDefinition
{
    [Tooltip("Seconds between each enemy spawn")]
    public float spawnInterval;

    [Tooltip("Seconds to wait after this wave finishes before starting the next")]
    public float postWaveDelay;

    [Tooltip("How many Normal enemies to spawn")]
    public int normalCount;

    [Tooltip("How many Fast enemies to spawn")]
    public int fastCount;

    [Tooltip("How many Armor (High HP) enemies to spawn")]
    public int armorCount;
}

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] GameObject normalPrefab;
    [SerializeField] GameObject fastPrefab;
    [SerializeField] GameObject armorPrefab;

    [Header("Waves")]
    [SerializeField] WaveDefinition[] waves;

    void Start()
    {
        if (waves == null || waves.Length == 0) return;
        StartCoroutine(RunAllWaves());
    }

    IEnumerator RunAllWaves()
    {
        foreach (var wave in waves)
        {
            yield return StartCoroutine(RunWave(wave));
            yield return new WaitForSeconds(wave.postWaveDelay);
        }
    }

    IEnumerator RunWave(WaveDefinition wave)
    {
        if (EnemyPath.Instance == null || EnemyPath.Instance.Waypoints.Count == 0)
        {
            Debug.LogWarning("[EnemyManager] EnemyPath not ready.");
            yield break;
        }

        var pool = new List<GameObject>();
        for (int i = 0; i < wave.normalCount; i++) pool.Add(normalPrefab);
        for (int i = 0; i < wave.fastCount;   i++) pool.Add(fastPrefab);
        for (int i = 0; i < wave.armorCount;  i++) pool.Add(armorPrefab);

        // Fisher-Yates shuffle so types are interspersed
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        foreach (var prefab in pool)
        {
            if (prefab != null)
                Instantiate(prefab, EnemyPath.Instance.Waypoints[0], prefab.transform.rotation);
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }
}
