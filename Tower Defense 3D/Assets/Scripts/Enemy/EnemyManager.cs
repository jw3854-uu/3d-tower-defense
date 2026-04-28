using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;

    float _timer;

    void Update()
    {
        if (enemyPrefab == null) return;

        _timer += Time.deltaTime;
        if (_timer >= spawnInterval)
        {
            _timer = 0f;
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (EnemyPath.Instance == null || EnemyPath.Instance.Waypoints.Count == 0)
        {
            Debug.LogWarning("EnemyManager: EnemyPath not ready.");
            return;
        }
        Instantiate(enemyPrefab, EnemyPath.Instance.Waypoints[0], Quaternion.identity);
    }
}
