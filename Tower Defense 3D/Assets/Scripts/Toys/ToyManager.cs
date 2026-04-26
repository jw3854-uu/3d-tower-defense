using System.Collections;
using UnityEngine;

public class ToyManager : MonoBehaviour
{
    public Grid grid;
    public LayerMask floorMask;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        grid = FindAnyObjectByType<Grid>();
    }

    public void Arm()
    {
        Debug.Log($"[ToyManager] Toy armed: {gameObject.name}");
        StartCoroutine(CheckLandingAfterDelay());
    }

    IEnumerator CheckLandingAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f, floorMask))
        {
            Debug.Log($"[ToyManager] Toy destroyed: no floor detected after 3s");
            Destroy(gameObject);
            yield break;
        }

        Tile tile = hit.collider.GetComponentInParent<Tile>();
        Debug.Log($"[ToyManager] Checking tile: {tile?.name ?? "None"} (hit {hit.collider.name})");

        if (tile != null && tile.isBuildable && !tile.isOccupied)
        {
            Debug.Log($"[ToyManager] Toy landed on buildable tile: {tile.name}");
            Vector3Int cell = grid.WorldToCell(transform.position);
            Vector3 cellCenter = grid.GetCellCenterWorld(cell);
            transform.position = new Vector3(cellCenter.x, hit.point.y, cellCenter.z);
            transform.rotation = Quaternion.identity;

            tile.isOccupied = true;

            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Collider>().enabled = false;
        }
        else
        {
            Debug.Log($"[ToyManager] Toy destroyed: tile not valid at landing position");
            // TODO: destroy with explosion effect
            Destroy(gameObject);
        }
    }
}
