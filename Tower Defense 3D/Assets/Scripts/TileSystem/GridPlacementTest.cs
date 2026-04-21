using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class GridPlacementTest : MonoBehaviour
{

    public Grid grid;
    // TODO: Change to the toy model prefab once we have it
    public GameObject placementPrefab;
    public Vector3Int referenceCell;
    public LayerMask floorLayerMask;

    [Header("Preview Materials")]
    public Material validPreviewMaterial;
    public Material invalidPreviewMaterial;

    public event Action OnPlaced;

    Camera _cam;
    GameObject _preview;
    Renderer[] _previewRenderers;
    Vector3Int _hoveredCell;
    bool _hasHovered;
    bool _active;

    void Start()
    {
        _cam = Camera.main;
        if (placementPrefab != null)
        {
            _preview = Instantiate(placementPrefab);
            _previewRenderers = _preview.GetComponentsInChildren<Renderer>();
            SetPreviewVisible(false);
            _active = false; // Start with placement disabled until Player B initiates it
        }
    }

    void SetPreviewMaterial(Material mat)
    {
        if (mat == null) return;
        foreach (var r in _previewRenderers)
            r.material = mat;
    }

    public void EnablePlacement()
    {
        _active = true;
    }

    public void DisablePlacement()
    {
        _active = false;
        SetPreviewVisible(false);
    }

    void Update()
    {
        if (grid == null || !_active) return;

        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Placement of toys
        bool tileHit = TryRaycastTile(ray, out RaycastHit worldHitRaycast);
        Vector3 worldHit = tileHit? worldHitRaycast.point: Vector3.zero;

        if (tileHit)
        {
            _hoveredCell = grid.WorldToCell(worldHit);
            Tile tile = tileHit ? worldHitRaycast.collider.GetComponentInParent<Tile>() : null;
            Vector3 cellCenter = grid.GetCellCenterWorld(_hoveredCell);
            Vector3 center = new Vector3(cellCenter.x, worldHit.y, cellCenter.z);
            bool canPlace = tile != null && tile.isBuildable && !tile.isOccupied;

            if (_preview != null)
            {
                SetPreviewVisible(true);
                _preview.transform.position = center;
                SetPreviewMaterial(canPlace ? validPreviewMaterial : invalidPreviewMaterial);
            }

            if (!canPlace){
                Debug.Log($"Hovering over cell {_hoveredCell} | Not placeable.");
            }else{
                Debug.Log($"Hovering over cell {_hoveredCell} | isBuildable={tile.isBuildable} | isWalkableA={tile.isWalkableA} | isWalkableB={tile.isWalkableB}");
                _hasHovered = true;

                // Left-click: place object
                if (Mouse.current.leftButton.wasPressedThisFrame){
                    PlaceObject(center);
                    tile.isOccupied = true; 
                }

                // Right-click: cancel placement

                float gridDist = GridDistance(_hoveredCell, referenceCell);
                float worldDist = WorldDistance(_hoveredCell, referenceCell);
                // Debug.Log($"Hovered: {_hoveredCell} | Grid dist from ref: {gridDist:F0} | World dist: {worldDist:F2}");
            }
        }
        else if (_preview != null)
        {
            SetPreviewVisible(false);
        }
    }

    void PlaceObject(Vector3 position)
    {
        if (placementPrefab == null) return;
        Instantiate(placementPrefab, position, Quaternion.identity);
        Debug.Log($"Placed at cell {_hoveredCell}, world {position}");
        DisablePlacement();
        OnPlaced?.Invoke();
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

    bool TryRaycastTile(Ray ray, out RaycastHit hit)
    {
        return Physics.Raycast(ray, out hit, 1000f, floorLayerMask);
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
