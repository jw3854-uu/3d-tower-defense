using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    public ToyTypeCatalog toyCatalog;

    [Header("Recording Bar")]
    [Tooltip("Prefab with a world-space Canvas + Slider inside. Leave empty to use a built-in fallback bar.")]
    [SerializeField] GameObject recordingBarPrefab;
    [SerializeField] float maxRecordingTime = 10f;
    [SerializeField] Vector3 recordingBarOffset = new Vector3(0f, 1.8f, 0f);

    float _recordingElapsed;
    GameObject _recordingBarInstance;
    Slider _recordingSlider;
    Image _fallbackFill;

    bool _isCloseToBin;
    bool _isCloseToBelt;
    bool _voiceSessionActive;
    [SerializeField] private Vector3 toyOffset;
    GameObject _currentToy;

    CharacterController _cc;
    float _verticalVelocity;
    Quaternion _baseRotation;

    void Awake(){
        _cc = GetComponent<CharacterController>();
        _baseRotation = transform.rotation;

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

    // Take care of voice input + progress bar
    void OnRecordingStarted(){
        _voiceSessionActive = true;
        _recordingElapsed = 0f;
        SpawnRecordingBar();
    }

    void SpawnRecordingBar()
    {
        if (_recordingBarInstance != null) Destroy(_recordingBarInstance);
        _recordingSlider = null;
        _fallbackFill = null;

        if (recordingBarPrefab != null)
        {
            _recordingBarInstance = Instantiate(recordingBarPrefab);
            _recordingSlider = _recordingBarInstance.GetComponentInChildren<Slider>(true);
            if (_recordingSlider != null)
            {
                _recordingSlider.minValue = 0f;
                _recordingSlider.maxValue = 1f;
                _recordingSlider.value = 0f;
                _recordingSlider.interactable = false;
            }
        }
        else
        {
            _recordingBarInstance = BuildFallbackBar();
        }
    }

    GameObject BuildFallbackBar()
    {
        var canvasGO = new GameObject("RecordingBarCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(1f, 0.1f);

        var bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f);
        StretchRect(bg.GetComponent<RectTransform>());

        var fill = new GameObject("Fill");
        fill.transform.SetParent(canvasGO.transform, false);
        _fallbackFill = fill.AddComponent<Image>();
        _fallbackFill.color = Color.yellow;
        _fallbackFill.type = Image.Type.Filled;
        _fallbackFill.fillMethod = Image.FillMethod.Horizontal;
        _fallbackFill.fillAmount = 0f;
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        return canvasGO;
    }

    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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

        if (voiceInputManager != null && voiceInputManager.IsRecording)
        {
            _recordingElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_recordingElapsed / maxRecordingTime);
            if (_recordingSlider != null) _recordingSlider.value = t;
            else if (_fallbackFill != null) _fallbackFill.fillAmount = t;
        }
        else if (_recordingElapsed > 0f)
        {
            Destroy(_recordingBarInstance);
            _recordingBarInstance = null;
            _recordingSlider = null;
            _fallbackFill = null;
        }
    }


    void OnVoiceTranscribed(string text){
        _voiceSessionActive = false;
        _isCloseToBin = false;

        GameObject matchedPrefab = toyCatalog?.CheckVoiceInput(text);
        if (matchedPrefab == null)
        {
            Debug.Log($"[PlayerAManager] No toy type matched transcription: \"{text}\"");
            return;
        }

        int cost = matchedPrefab.GetComponent<Toy>()?.Price ?? 0;
        if (GameManager.Instance != null && !GameManager.Instance.SpendMoney(cost))
        {
            Debug.Log($"[PlayerAManager] Cannot afford toy (costs {cost}). Pickup blocked.");
            return;
        }

        toySpawner.toyPrefab = matchedPrefab;
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
        UpdateRecordingBar();

        if (horizontalMove.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(horizontalMove.normalized) * _baseRotation;

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
