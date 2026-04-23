using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ToyProjectile : MonoBehaviour
{
    [HideInInspector] public Grid grid;

    bool _landed;

    void OnCollisionEnter(Collision collision)
    {
        if (_landed) return;
        _landed = true;

        Tile tile = collision.collider.GetComponentInParent<Tile>();

        if (tile != null && tile.isBuildable && !tile.isOccupied)
        {
            Vector3Int cell = grid.WorldToCell(transform.position);
            Vector3 cellCenter = grid.GetCellCenterWorld(cell);
            transform.position = new Vector3(cellCenter.x, collision.contacts[0].point.y, cellCenter.z);

            tile.isOccupied = true;

            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Collider>().enabled = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
