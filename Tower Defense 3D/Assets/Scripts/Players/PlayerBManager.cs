using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerBManager : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public LayerMask FloorLayerMask;
    public GridPlacementTest gridPlacement;
    public enum PlayerState {Waiting, Holding, Placing};
    public PlayerState currentState;

    CharacterController _cc;
    float _verticalVelocity;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (gridPlacement != null)
            gridPlacement.OnPlaced += OnToyPlaced;

        // FOR TESTING
        currentState = PlayerState.Placing;
    }

    void OnDestroy()
    {
        if (gridPlacement != null)
            gridPlacement.OnPlaced -= OnToyPlaced;
    }

    void OnToyPlaced()
    {
        currentState = PlayerState.Waiting;
        // Debug.Log("Player B has released the object and is now WAITING.");
    }

    void HandleToyPlacement()
    {
        if (gridPlacement == null) return;
        gridPlacement.EnablePlacement();
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
        }else if (currentState == PlayerState.Holding){
            // Add magic in the toy
        }
        if (currentState == PlayerState.Placing){
            // Handle toy placement
            HandleToyPlacement();
        }
    }
}
