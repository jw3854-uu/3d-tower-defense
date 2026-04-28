using System.Collections.Generic;
using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    public static EnemyPath Instance { get; private set; }
    public List<Vector3> Waypoints { get; private set; } = new();

    public Grid grid;

    void Awake()
    {
        Instance = this;
        BuildPath();
    }

    void BuildPath()
    {
        if (grid == null)
        {
            Debug.LogError("EnemyPath: Grid reference is not assigned!");
            return;
        }

        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        Dictionary<Vector3Int, Tile> tileMap = new();
        Tile startTile = null;

        foreach (Tile t in allTiles)
        {
            if (!t.isEnemyPath) continue;
            Vector3Int cell = grid.WorldToCell(t.transform.position);
            tileMap[cell] = t;
            if (t.isPathStart) startTile = t;
        }

        if (startTile == null)
        {
            Debug.LogError("EnemyPath: No tile with isPathStart found!");
            return;
        }else{
            Debug.Log($"EnemyPath: found start tile at {startTile.transform.position}");
        }

        // BFS from start tile, only stepping onto isEnemyPath neighbors
        // grid.WorldToCell maps world-X → cell-X, world-Z → cell-Y
        Vector3Int[] dirs = {
            new( 1,  0, 0),
            new(-1,  0, 0),
            new( 0,  1, 0),
            new( 0, -1, 0)
        };

        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();

        Vector3Int startCell = grid.WorldToCell(startTile.transform.position);
        queue.Enqueue(startCell);
        visited.Add(startCell);

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            Waypoints.Add(tileMap[current].transform.position + Vector3.up * 0.5f);

            foreach (var dir in dirs)
            {
                Vector3Int neighbor = current + dir;
                if (!visited.Contains(neighbor) && tileMap.ContainsKey(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        Debug.Log($"EnemyPath: built {Waypoints.Count} waypoints.");
    }

}
