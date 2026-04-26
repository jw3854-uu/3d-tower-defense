using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToyBelt : MonoBehaviour
{
    [Header("Belt Setup")]
    public Transform startPoint;
    public Transform endPoint;
    public float dropdownRadius = 2f;

    [Header("Movement")]
    public float speed = 2f;

    // Fires with the toy GameObject when it reaches the end of the belt
    public event Action<GameObject> OnToyArrived;

    public bool CheckDistance(Vector3 playerPos)
    {
        // Debug.Log($"Checking distance from player at {playerPos} to belt at {startPoint.position}: {Vector3.Distance(playerPos, startPoint.position)} ");
        return Vector3.Distance(playerPos, startPoint.position) <= dropdownRadius;
    }

    public void PlaceToy(GameObject toy)
    {
        if (toy == null || startPoint == null || endPoint == null) return;

        toy.transform.SetParent(null);

        Transform bottom = toy.transform.Find("Bottom Center");
        // Capture local-space position of bottom before any rotation change
        Vector3 localBottom = toy.transform.InverseTransformPoint(bottom.position);

        // Align toy to belt surface normal
        toy.transform.rotation = Quaternion.FromToRotation(Vector3.up, startPoint.up);

        // After rotation, compute pivot position so bottom lands exactly on startPoint
        // pivot = startPoint - (rotation * localBottom)
        Vector3 pivotOffset = -(toy.transform.rotation * localBottom);
        toy.transform.position = startPoint.position + pivotOffset;

        Debug.Log($"Placing toy on belt. Bottom at {bottom.position}, pivot offset {pivotOffset}");

        StartCoroutine(MoveToy(toy, pivotOffset));
    }

    IEnumerator MoveToy(GameObject toy, Vector3 pivotOffset)
    {
        float distance = Vector3.Distance(startPoint.position, endPoint.position);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (toy == null) yield break;
            elapsed += Time.deltaTime;
            // Move so the bottom traces the belt path, not the pivot
            Vector3 beltPos = Vector3.Lerp(startPoint.position, endPoint.position, elapsed / duration);
            toy.transform.position = beltPos + pivotOffset;
            yield return null;
        }

        if (toy != null)
        {
            toy.transform.position = endPoint.position + pivotOffset;
            OnToyArrived?.Invoke(toy);
        }
    }
}
