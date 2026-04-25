using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerAManager : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public LayerMask FloorLayerMask;

    public enum PlayerState {Waiting, Holding};
    public PlayerState currentState;

    public VoiceInputManager voiceInputManager;
    public SpawnToy toySpawner;
    public ToyBelt toyBelt;

    bool _isCloseToBin;
    bool _isCloseToBelt;
    bool _voiceSessionActive;
    [SerializeField] private Vector3 toyOffset;
    GameObject _currentToy;

    CharacterController _cc;
    float _verticalVelocity;

    void Awake(){
        _cc = GetComponent<CharacterController>();

        if (voiceInputManager != null)
        {
            voiceInputManager.OnRecordingStarted += OnRecordingStarted;
            voiceInputManager.OnTranscribed += OnVoiceTranscribed;
        }

        voiceInputManager.DisableVoiceInput();

        currentState = PlayerState.Waiting;
    } 

    void OnDestroy(){
        if (voiceInputManager != null)
        {
            voiceInputManager.OnRecordingStarted -= OnRecordingStarted;
            voiceInputManager.OnTranscribed -= OnVoiceTranscribed;
        }
    }

    void OnRecordingStarted(){
        _voiceSessionActive = true;
        // Debug.Log("[Player A Manager]: Recording started.");
    }

    void OnVoiceTranscribed(string text){
        _voiceSessionActive = false;
        _isCloseToBin = false;
        // TODO: check for multiple toy commands
        // if (text.ToLower().Contains("toy"))
        // {
        //     // Debug.Log("[Player A Manager]: Voice command successful, spawning toy.");
        //     _currentToy = toySpawner.SpawnToyAt(toyOffset, transform);
        //     currentState = PlayerState.Holding;
        // }

        _currentToy = toySpawner.SpawnToyAt(toyOffset, transform);
        currentState = PlayerState.Holding;
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

        if (_voiceSessionActive || !IsWalkableA(transform.position + horizontalMove * Time.deltaTime))
            horizontalMove = Vector3.zero;

        if (_cc.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = horizontalMove;
        move.y = _verticalVelocity;
        _cc.Move(move * Time.deltaTime);

        // Picking Up Object Logic
        if (currentState == PlayerState.Waiting && !_voiceSessionActive){
            _isCloseToBin = toySpawner.CheckDistance(transform.position);

            if (_isCloseToBin){
                // Debug.Log("Player A is close to the toy bin. Press R to record voice input.");
                voiceInputManager.EnableVoiceInput();
            } else {
                // Debug.Log("Player A is not close to the toy bin.");
                voiceInputManager.DisableVoiceInput();
            }
        // Dropping Object Logic
        }else if (currentState == PlayerState.Holding){
            _isCloseToBelt = toyBelt.CheckDistance(transform.position);

             if (_isCloseToBelt){
                // Debug.Log("Player A is close to the toy belt. Press Space to place toy.");
                if (kb.spaceKey.wasPressedThisFrame)
                {
                    toyBelt.PlaceToy(_currentToy);
                    _currentToy = null;
                    currentState = PlayerState.Waiting;
                }
            } else {
                // Debug.Log("Player A is not close to the toy belt.");
            }
            // Trigger toy placement in ToyBelt
        }
    }
}
