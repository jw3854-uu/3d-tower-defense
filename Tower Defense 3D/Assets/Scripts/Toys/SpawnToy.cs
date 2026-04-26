using UnityEngine;

public class SpawnToy : MonoBehaviour
{
    public GameObject toyPrefab;
    public enum ToyType { Waiting, Holding };
    public float pickupRadius = 2f;

    public bool CheckDistance(Vector3 playerPos)
    {
        return Vector3.Distance(playerPos, transform.position) <= pickupRadius;
    }

    public GameObject SpawnToyAt(Vector3 localOffset, Transform parent = null)
    {
        GameObject toy = Instantiate(toyPrefab);
        if (parent != null)
            toy.transform.SetParent(parent, worldPositionStays: false);
        toy.transform.localPosition = localOffset;
        toy.transform.localRotation = Quaternion.identity;
        Debug.Log($"Spawning toy at local offset {localOffset} under {parent?.name ?? "scene root"}");
        return toy;
    }
}
