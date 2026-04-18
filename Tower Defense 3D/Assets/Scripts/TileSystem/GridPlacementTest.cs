using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

// Attach to any GameObject. Assign Grid, a prefab, and optionally a second cell to measure distance from.
public class GridPlacementTest : MonoBehaviour
{
    [Header("References")]
    public Grid grid;
    public GameObject placementPrefab;

    [Header("Distance Debug")]
    [Tooltip("Measure grid distance from this cell (set via inspector or right-click context)")]
    public Vector3Int referenceCell;

    Camera _cam;
    GameObject _preview;
    Vector3Int _hoveredCell;
    bool _hasHovered;

    void Start()
    {
        _cam = Camera.main;

        if (placementPrefab != null)
        {
            _preview = Instantiate(placementPrefab);
            SetPreviewVisible(false);
        }
    }

    void Update()
    {
        if (grid == null) return;

        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Try hitting a tile collider first; fall back to the y=0 plane.
        bool hit = TryRaycastTile(ray, out Vector3 worldHit) || TryRaycastGroundPlane(ray, out worldHit);

        if (hit)
        {
            _hoveredCell = grid.WorldToCell(worldHit);
            Vector3 center = grid.GetCellCenterWorld(_hoveredCell);
            _hasHovered = true;

            if (_preview != null)
            {
                SetPreviewVisible(true);
                _preview.transform.position = center;
            }

            // Left-click: place object
            if (Mouse.current.leftButton.wasPressedThisFrame)
                PlaceObject(center);

            // Right-click: set reference cell for distance measurement
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                referenceCell = _hoveredCell;
                Debug.Log($"Reference cell set to {referenceCell}");
            }

            float gridDist = GridDistance(_hoveredCell, referenceCell);
            float worldDist = WorldDistance(_hoveredCell, referenceCell);
            Debug.Log($"Hovered: {_hoveredCell} | Grid dist from ref: {gridDist:F0} | World dist: {worldDist:F2}");
        }
        else if (_preview != null)
        {
            SetPreviewVisible(false);
        }
    }

    void PlaceObject(Vector3 position)
    {
        if (placementPrefab == null) return;
        Vector3 positionWithOffset = position + new Vector3(0, 1, 0);
        Instantiate(placementPrefab, positionWithOffset, Quaternion.identity);
        Debug.Log($"Placed at cell {_hoveredCell}, world {positionWithOffset}");
    }

    // Manhattan distance in grid space (good for pathfinding heuristics)
    public static float GridDistance(Vector3Int a, Vector3Int b)
    {
        Vector3Int d = a - b;
        return Mathf.Abs(d.x) + Mathf.Abs(d.y) + Mathf.Abs(d.z);
    }

    // Euclidean distance in world space between cell centers
    public float WorldDistance(Vector3Int a, Vector3Int b)
    {
        return Vector3.Distance(grid.GetCellCenterWorld(a), grid.GetCellCenterWorld(b));
    }

    bool TryRaycastTile(Ray ray, out Vector3 hitPoint)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            hitPoint = hit.point;
            return true;
        }
        hitPoint = default;
        return false;
    }

    // Fallback: intersect with the horizontal plane at y=0
    bool TryRaycastGroundPlane(Ray ray, out Vector3 hitPoint)
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
        {
            hitPoint = ray.GetPoint(enter);
            return true;
        }
        hitPoint = default;
        return false;
    }

    void SetPreviewVisible(bool visible)
    {
        if (_preview != null) _preview.SetActive(visible);
    }

    void OnDestroy()
    {
        if (_preview != null) Destroy(_preview);
    }
}
