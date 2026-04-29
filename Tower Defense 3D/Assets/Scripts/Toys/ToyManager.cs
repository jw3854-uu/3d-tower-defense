using System.Collections;
using UnityEngine;

public class ToyManager : MonoBehaviour
{
    public Grid grid;
    public LayerMask floorMask;

    Vector3 _basePrefabPosition;

    void Awake()
    {
        _basePrefabPosition = transform.localPosition;
    }

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
            transform.position = new Vector3(cellCenter.x, hit.point.y + _basePrefabPosition.y, cellCenter.z);
            transform.rotation = Quaternion.identity;

            Toy toy = GetComponent<Toy>();
            // int cost = toy != null ? toy.Price : 0;
            // if (GameManager.Instance != null && !GameManager.Instance.SpendMoney(cost))
            // {
            //     Debug.Log($"[ToyManager] Cannot afford toy (costs {cost}). Destroying.");
            //     Destroy(gameObject);
            //     yield break;
            // }

            tile.isOccupied = true;

            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Collider>().enabled = false;

            toy?.Activate();
        }
        else
        {
            Debug.Log($"[ToyManager] Toy destroyed: tile not valid at landing position");
            // TODO: destroy with explosion effect
            Destroy(gameObject);
        }
    }
}
