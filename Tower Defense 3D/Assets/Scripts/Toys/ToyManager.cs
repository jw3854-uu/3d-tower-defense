using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class ToyManager : NetworkBehaviour
{
    public Grid grid;
    public LayerMask floorMask;

    Vector3 _basePrefabPosition;
    Rigidbody _rb;

    void Awake()
    {
        _basePrefabPosition = transform.localPosition;
        _rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        grid = FindAnyObjectByType<Grid>();

        // Only the server simulates flight physics; clients stay kinematic and just
        // display whatever position NetworkTransform replicates to them.
        _rb.isKinematic = !IsServer;

        if (IsServer)
        {
            StartCoroutine(CheckLandingAfterDelay());
        }
    }

    IEnumerator CheckLandingAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f, floorMask))
        {
            Debug.Log($"[ToyManager] Toy destroyed: no floor detected after 3s");
            NetworkObject.Despawn();
            yield break;
        }

        Tile tile = hit.collider.GetComponentInParent<Tile>();
        Debug.Log($"[ToyManager] Checking tile: {tile?.name ?? "None"} (hit {hit.collider.name})");

        if (tile != null && tile.isBuildable && !tile.isOccupied)
        {
            Debug.Log($"[ToyManager] Toy landed on buildable tile: {tile.name}");
            Vector3Int cell = grid.WorldToCell(transform.position);
            Vector3 cellCenter = grid.GetCellCenterWorld(cell);
            transform.position = new Vector3(cellCenter.x, hit.point.y + _basePrefabPosition.y, cellCenter.z);
            transform.rotation = Quaternion.identity;

            tile.isOccupied = true;

            _rb.isKinematic = true;
            GetComponent<Collider>().enabled = false;

            GetComponent<Toy>()?.Activate();
        }
        else
        {
            Debug.Log($"[ToyManager] Toy destroyed: tile not valid at landing position");
            // TODO: destroy with explosion effect
            NetworkObject.Despawn();
        }
    }
}
