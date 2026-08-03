using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerAManager : NetworkBehaviour 
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] LayerMask FloorLayerMask;
    CharacterController _cc;
    float _verticalVelocity;
    Quaternion _baseRotation;

    // public enum PlayerState {Step1, Step2};
    // public PlayerState currentState;

    // [Header("Vosk Detection")]
    // [SerializeField] 
    // public MicCaptureManager micCaptureManager;
    // public SpawnToy toySpawner;
    // public ToyBelt toyBelt;

    [Header("References")]
    [SerializeField] LaunchManager launchManager;

    [Header("Launcher Angle Contribution (joint with B)")]
    // [SerializeField] float maxHeightHoldTime = 3f;
    bool _isRecordingHeight;
    float _heightHoldElapsed;
    [SerializeField] PitchTracker pitchTracker;

    // Live values while Enter is held, read directly by both machines' UI to draw the
    // real-time pitch-difference meter. 
    public NetworkVariable<float> livePitch = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isRecordingHeight = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Step 1 Recording Bar")]
    [Tooltip("Prefab with a world-space Canvas + Slider inside.")]
    [SerializeField] GameObject recordingBarPrefab;
    [SerializeField] float maxRecordingTime = 10f;
    [SerializeField] Vector3 recordingBarOffset = new Vector3(0f, 1.8f, 0f);

    float _recordingElapsed;
    GameObject _recordingBarInstance;
    Slider _recordingSlider;

    // bool _isCloseToBin;
    // bool _isCloseToBelt;
    bool _voiceSessionActive;
    // [SerializeField] private Vector3 toyOffset;
    // GameObject _currentToy;

    // priate NetworkVariable<bool> _isRecording = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn(){
        _cc = GetComponent<CharacterController>();
        _baseRotation = transform.rotation;
        pitchTracker = GetComponent<PitchTracker>();

        if (VoskRecognitionManager.Instance != null)
        {
            VoskRecognitionManager.Instance.OnTranscribed += OnVoiceRecognized;
        }
        // currentState = PlayerState.Waiting;
        launchManager = FindFirstObjectByType<LaunchManager>();
    }

    void OnDestroy(){
        if (VoskRecognitionManager.Instance != null)
        {
            VoskRecognitionManager.Instance.OnTranscribed -= OnVoiceRecognized;
        }
    }

    // Take care of voice input progress bar
    void BeginListening(){
        VoskRecognitionManager.Instance.BeginListening();
        _voiceSessionActive = true;
        _recordingElapsed = 0f;
        SpawnRecordingBar();
    }

    void SpawnRecordingBar()
    {
        if (_recordingBarInstance != null) Destroy(_recordingBarInstance);
        _recordingSlider = null;

        if (recordingBarPrefab == null) return;

        _recordingBarInstance = Instantiate(recordingBarPrefab);
        _recordingSlider = _recordingBarInstance.GetComponentInChildren<Slider>(true);
        if (_recordingSlider == null)
        {
            Debug.LogError("[PlayerAManager] RecordingBarPrefab has no Slider component in children.");
            return;
        }
        _recordingSlider.minValue = 0f;
        _recordingSlider.maxValue = 1f;
        _recordingSlider.value = 0f;
        _recordingSlider.interactable = false;
    }

    void UpdateRecordingBar()
    {
        if (_recordingBarInstance == null) return;

        _recordingBarInstance.transform.position = transform.position + recordingBarOffset;
        var cam = Camera.main;
        if (cam != null)
        {
            _recordingBarInstance.transform.LookAt(cam.transform);
            _recordingBarInstance.transform.Rotate(0f, 180f, 0f);
        }

        if (VoskRecognitionManager.Instance != null && _voiceSessionActive)
        {
            // Debug.Log($"[PlayerAManager] Recording... {_recordingElapsed:F1}s");
            _recordingElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_recordingElapsed / maxRecordingTime);
            if (_recordingSlider != null) _recordingSlider.value = t;
        }
        else if (_recordingElapsed > 0f)
        {
            Destroy(_recordingBarInstance);
            _recordingBarInstance = null;
            _recordingSlider = null;
        }
    }

    void EndListening(){
        VoskRecognitionManager.Instance.EndListeningAndRecognize();
        _voiceSessionActive = false;
    }

    void OnVoiceRecognized(string text){
        ToyScriptableObject matchedToy = ToyCatalog.Instance?.CheckVoiceInput(text);
        if (matchedToy == null)
        {
            Debug.Log($"[PlayerAManager] No toy type matched transcription: \"{text}\"");
            return;
        }
        Debug.Log($"[PlayerAManager] Matched toy type: {matchedToy.TypeName}"); 
        launchManager.RequestLoadToyRpc(matchedToy.TypeName);
    }

    // Moving logic
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
        // Only process input for the local player 
        if (PlayerSessionData.Instance.playerASlotOwner.Value != NetworkManager.Singleton.LocalClientId) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        // Cap deltaTime for movement/physics math — the very first frame after a scene load
        // (or any mid-game hitch) can report a huge Time.deltaTime, which would otherwise
        // turn one frame of gravity into a giant instantaneous drop and shove the
        // CharacterController sideways when it slams into the floor/collides on landing.
        float dt = Mathf.Min(Time.deltaTime, 0.05f);

        // Moving logic
        float inputX = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float inputZ = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);

        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
        Vector3 camRight   = Vector3.ProjectOnPlane(Camera.main.transform.right,   Vector3.up).normalized;
        Vector3 horizontalMove = (camForward * inputZ + camRight * inputX) * moveSpeed;

        if (_voiceSessionActive || !IsWalkableA(transform.position + horizontalMove * dt))
            horizontalMove = Vector3.zero;

        if (_cc.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += gravity * dt;

        Vector3 move = horizontalMove;
        move.y = _verticalVelocity;
        _cc.Move(move * dt);
        UpdateRecordingBar();

        if (horizontalMove.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(horizontalMove.normalized) * _baseRotation;

        // Vosk logic (temp)
        if (kb.spaceKey.wasPressedThisFrame && !_voiceSessionActive)
            BeginListening();
        if (kb.spaceKey.wasReleasedThisFrame && _voiceSessionActive)
            EndListening();

        HandleHeightRecording(kb);
    }

    // Hold Enter to contribute to the joint throw-height decision with B.
    // Updates livePitch every frame while held so both screens can show a real-time diff meter.
    void HandleHeightRecording(Keyboard kb)
    {
        if (kb.enterKey.wasPressedThisFrame)
        {
            _isRecordingHeight = true;
            isRecordingHeight.Value = true;
            pitchTracker.StartTracking();
        }

        if (_isRecordingHeight && kb.enterKey.isPressed)
        {
            // Pitch == 0 means no clear pitch this frame (e.g. a breath) — hold the
            // last value instead of snapping livePitch to 0.
            float hz = pitchTracker.Pitch;
            if (hz > 0f)
                livePitch.Value = hz;
        }

        if (_isRecordingHeight && kb.enterKey.wasReleasedThisFrame)
        {
            _isRecordingHeight = false;
            isRecordingHeight.Value = false;
            pitchTracker.StopTracking();
            // livePitch.Value stays at its last held value — LaunchManager reads it directly whenever it needs the settled result
        }
    }
}
