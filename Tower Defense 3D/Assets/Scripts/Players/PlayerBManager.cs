using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerBManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] LaunchManager launchManager;

    [Header("Horizontal Aim")]
    [SerializeField] float maxYawDegrees = 90f;
    [SerializeField] float aimSpeed = 45f;
    float _currentYaw;
    float placeholderPitch = 0f; // TO SEE HOW A PERFORMS

    // Owner-writable so B can write it directly, without a server round trip — relies on
    // OnNetworkSpawn() below actually giving B real ownership of this NetworkObject.
    public NetworkVariable<float> aimYaw = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Height Contribution (joint with A)")]
    [Tooltip("Placeholder for real pitch tracking — currently just measures how long Enter was held.")]
    [SerializeField] float maxHoldTime = 3f;
    bool _isRecordingHeight;
    float _heightHoldElapsed;

    // Live values while Enter is held, read directly by both machines' UI to draw the
    // real-time pitch-difference meter.
    public NetworkVariable<float> livePitch = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isRecordingHeight = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        launchManager = FindFirstObjectByType<LaunchManager>();
    }

    void Update()
    {
        // Only process input for the local player
        if (PlayerSessionData.Instance.playerBSlotOwner.Value != NetworkManager.Singleton.LocalClientId) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        HandleAim(kb);
        HandleHeightRecording(kb);

        if (kb.spaceKey.wasPressedThisFrame)
            launchManager.RequestLaunchRpc();
    }

    // Hold A/D to steer the launcher left/right.
    void HandleAim(Keyboard kb)
    {
        float yawInput = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        _currentYaw = Mathf.Clamp(_currentYaw + yawInput * aimSpeed * Time.deltaTime, -maxYawDegrees, maxYawDegrees);
        aimYaw.Value = _currentYaw; // owned by B, so this write actually takes effect
    }

    // Hold Enter to contribute to the joint throw-height decision with A.
    // Updates livePitch every frame while held so both screens can show a real-time diff meter.
    void HandleHeightRecording(Keyboard kb)
    {
        if (kb.enterKey.wasPressedThisFrame)
        {
            _isRecordingHeight = true;
            isRecordingHeight.Value = true;
        }

        if (_isRecordingHeight && kb.enterKey.isPressed)
        {
            livePitch.Value = placeholderPitch; // TODO: replace with real pitch data once we have it
        }

        if (_isRecordingHeight && kb.enterKey.wasReleasedThisFrame)
        {
            _isRecordingHeight = false;
            isRecordingHeight.Value = false;
            // livePitch.Value stays at its last held value — LaunchManager reads it directly whenever it needs the settled result
        }
    }
}
