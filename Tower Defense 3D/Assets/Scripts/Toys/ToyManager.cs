using UnityEngine;

public class ToyManager : MonoBehaviour
{
    private enum ToyState {WithPlayerA, OnBelt, WithPlayerB, Shot};
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    public void ToOnBelt(Transform beltStart)
    {
        transform.position = beltStart.position;
        transform.rotation = beltStart.rotation;
        GetComponent<Rigidbody>().isKinematic = false;
        // rb.linearVelocity = launchVelocity;
    }
}
