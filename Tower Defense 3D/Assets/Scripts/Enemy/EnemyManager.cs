using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[System.Serializable]
public struct WaveEnemyCount
{
    public ToyScriptableObject enemyType;
    public int count;
}

[System.Serializable]
public struct WaveDefinition
{
    [Tooltip("Seconds between each enemy spawn")]
    public float spawnInterval;

    [Tooltip("Seconds to wait after this wave finishes before starting the next")]
    public float postWaveDelay;

    [Tooltip("Which enemy types spawn this wave, and how many of each")]
    public WaveEnemyCount[] enemyCounts;
}

public class EnemyManager : MonoBehaviour
{
    [Header("Waves")]
    [SerializeField] WaveDefinition[] waves;

    // Called by LevelManager once it's server and the scene load has fully completed —
    // not from Start(), so we can't race Netcode's own scene-object registration the
    // same way LevelManager's player spawn once did.
    public void BeginWaves()
    {
        if (!NetworkManager.Singleton.IsServer) return;
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
        if (wave.enemyCounts != null)
        {
            foreach (var entry in wave.enemyCounts)
            {
                if (entry.enemyType == null || entry.enemyType.EnemyPrefab == null) continue;
                for (int i = 0; i < entry.count; i++)
                    pool.Add(entry.enemyType.EnemyPrefab);
            }
        }

        // Fisher-Yates shuffle so types are interspersed
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        foreach (var prefab in pool)
        {
            var enemy = Instantiate(prefab, EnemyPath.Instance.Waypoints[0], prefab.transform.rotation);
            enemy.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }
}
