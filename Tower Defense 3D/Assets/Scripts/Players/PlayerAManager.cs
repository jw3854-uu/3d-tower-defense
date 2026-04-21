using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerAManager : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public LayerMask FloorLayerMask;

    public enum PlayerState {Waiting, Holding, Placing};
    public PlayerState currentState;

    public VoiceInputManager voiceInputManager;

    CharacterController _cc;
    float _verticalVelocity;

    void Awake(){
        _cc = GetComponent<CharacterController>();

        if (voiceInputManager != null)
            voiceInputManager.OnTranscribed += OnVoiceTranscribed;

        // FOR TESTING
        currentState = PlayerState.Holding;
    } 

    void OnDestroy(){
        if (voiceInputManager != null)
            voiceInputManager.OnTranscribed -= OnVoiceTranscribed;
    }

    void OnVoiceTranscribed(){
        currentState = PlayerState.Placing;
    }

    bool IsWalkableA(Vector3 position)
    {
        // Raycast downward from slightly above the target position
        Ray ray = new Ray(position + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 3f, FloorLayerMask))
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            // Debug.Log($"Hit tile at {hit.collider}, isWalkableA={tile?.isWalkableA}");
            return tile != null && tile.isWalkableA;
        }
        return false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float inputX = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float inputZ = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);

        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight   = Vector3.ProjectOnPlane(Camera.main.transform.right,   Vector3.up).normalized;
        Vector3 horizontalMove = (camForward * inputZ + camRight * inputX) * moveSpeed;

        if (!IsWalkableA(transform.position + horizontalMove * Time.deltaTime))
            horizontalMove = Vector3.zero;

        if (_cc.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = horizontalMove;
        move.y = _verticalVelocity;
        _cc.Move(move * Time.deltaTime);

        if (currentState == PlayerState.Waiting){
            // Checking for picking up distance
        }else if (currentState == PlayerState.Holding){
            voiceInputManager.EnableVoiceInput();
        }else if (currentState == PlayerState.Placing){
            // Checking for placing distance and trigger 传送
        }
    }
}
