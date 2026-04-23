using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerBManager : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public LayerMask FloorLayerMask;
    public enum PlayerState {Waiting, Holding, Placing};
    public PlayerState currentState;

    [Header("Projectile")]
    public GameObject toyPrefab;
    public Grid grid;
    public float maxLaunchSpeed = 20f;
    public float maxChargeTime = 2f;
    public float arcHeight = 5f;

    CharacterController _cc;
    float _verticalVelocity;
    float _chargeTime;
    bool _isCharging;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        currentState = PlayerState.Placing; // FOR TESTING
    }

    void HandlePlacing()
    {
        if (toyPrefab == null || grid == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        // Get aim target: raycast from camera through mouse onto ground plane
        Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (!groundPlane.Raycast(ray, out float enter)) return;
        Vector3 aimTarget = ray.GetPoint(enter);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _isCharging = true;
            _chargeTime = 0f;
        }

        if (_isCharging && mouse.leftButton.isPressed)
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, maxChargeTime);

        if (_isCharging && mouse.leftButton.wasReleasedThisFrame)
        {
            _isCharging = false;
            ShootToy(aimTarget);
            // currentState = PlayerState.Waiting;
        }
    }

    void ShootToy(Vector3 target)
    {
        Vector3 spawnPos = transform.position + Vector3.up;
        GameObject toy = Instantiate(toyPrefab, spawnPos, Quaternion.identity);

        ToyProjectile proj = toy.GetComponent<ToyProjectile>();
        if (proj != null) proj.grid = grid;

        float chargeRatio = _chargeTime / maxChargeTime;
        float speed = Mathf.Lerp(maxLaunchSpeed * 0.3f, maxLaunchSpeed, chargeRatio);

        Vector3 horizontal = (target - spawnPos);
        horizontal.y = 0;
        float dist = horizontal.magnitude;
        Vector3 dir = horizontal.normalized;

        // Compute vertical velocity needed to arc over arcHeight and reach target
        float vUp = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * arcHeight);
        float timeUp = vUp / Mathf.Abs(Physics.gravity.y);
        float timeDown = Mathf.Sqrt(2f * (arcHeight + Mathf.Max(0, spawnPos.y - target.y)) / Mathf.Abs(Physics.gravity.y));
        float totalTime = timeUp + timeDown;
        float hSpeed = dist / totalTime;

        Vector3 launchVelocity = dir * hSpeed * chargeRatio + Vector3.up * vUp;

        toy.GetComponent<Rigidbody>().linearVelocity = launchVelocity;
    }

    bool IsWalkableB(Vector3 position)
    {
        // Raycast downward from slightly above the target position
        Ray ray = new Ray(position + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 3f, FloorLayerMask))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            // Debug.Log($"Hit tile at {hit.collider}, isWalkableB={tile?.isWalkableA}");
            return tile != null && tile.isWalkableB;
        }
        return false;
    }

    void Update()
    {
        // Player Movement
        var kb = Keyboard.current;
        if (kb == null) return;

        float inputX = (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0);
        float inputZ = (kb.upArrowKey.isPressed   ? 1 : 0) - (kb.downArrowKey.isPressed  ? 1 : 0);

        // Move relative to camera orientation
        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight   = Vector3.ProjectOnPlane(Camera.main.transform.right,   Vector3.up).normalized;
        Vector3 horizontalMove = (camForward * inputZ + camRight * inputX) * moveSpeed;

        if (!IsWalkableB(transform.position + horizontalMove * Time.deltaTime))
            horizontalMove = Vector3.zero;

        if (_cc.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = horizontalMove;
        move.y = _verticalVelocity;
        _cc.Move(move * Time.deltaTime);

        if (currentState == PlayerState.Waiting){
            // Picking up object logic
        } else if (currentState == PlayerState.Holding){
            // Add magic in the toy
        } else if (currentState == PlayerState.Placing){
            HandlePlacing();
        }
    }
}
