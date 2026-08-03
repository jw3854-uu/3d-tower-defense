using UnityEngine;
using Unity.Netcode;

// Combines A and B's joint contribution to a throw. A and B each continuously sync
// their own livePitch NetworkVariable (see PlayerAManager/PlayerBManager); this class
// reads both directly every frame to drive its own elevation angle in real time —
// no report/commit RPC needed, the values are already synced.
public class LaunchManager : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("A/B's live data lives on their own managers (correct NetworkVariable ownership), read from here.")]
    [SerializeField] PlayerAManager playerA;
    [SerializeField] PlayerBManager playerB;

    [Header("Elevation (driven live by A/B pitch difference)")]
    [Tooltip("livePitch arrives as raw Hz from PitchTracker — normalized here, not at the source, so both players share one tunable vocal range.")]
    [SerializeField] float pitchMinHz = 100f;
    [SerializeField] float pitchMaxHz = 400f;
    [SerializeField] float maxElevationDegrees = 60f;
    Quaternion _initialRotation;

    [Header("Launch")]
    [SerializeField] float launchSpeed = 15f; // TODO: decide what (if anything) should vary this

    GameObject _loadedToyPrefab; // set by RequestLoadToyRpc once A picks a toy and the server confirms the spend
    GameObject toyInstance;

    // Each player's raw Hz is normalized to a 0..1 fraction of the shared vocal range
    // before diffing, so the result is naturally bounded to [-1, 1] with no extra clamp.
    public float CurrentPitchDiff => NormalizedPitch(playerA.livePitch.Value) - NormalizedPitch(playerB.livePitch.Value);
    public bool BothRecording => playerA.isRecordingHeight.Value && playerB.isRecordingHeight.Value;

    float NormalizedPitch(float hz) => Mathf.InverseLerp(pitchMinHz, pitchMaxHz, hz);

    void Awake()
    {
        _initialRotation = transform.rotation;
        playerA = FindFirstObjectByType<PlayerAManager>();
        playerB = FindFirstObjectByType<PlayerBManager>();
    }

    void Update()
    {
        // Runs identically on every machine — CurrentPitchDiff is already synced,
        // so this needs no networking of its own, just a local read every frame.
        float elevation = CurrentPitchDiff * maxElevationDegrees;
        transform.rotation = Quaternion.AngleAxis(playerB.aimYaw.Value, Vector3.up) * _initialRotation * Quaternion.Euler(-elevation, 0f, 0f);
    }

    // Called by PlayerAManager once local voice recognition matches a toy.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestLoadToyRpc(string typeName, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (PlayerSessionData.Instance.playerASlotOwner.Value != senderId) return; // only A can load a toy

        var toyData = ToyCatalog.Instance?.FindByTypeName(typeName);
        if (toyData == null)
        {
            Debug.Log($"[LaunchManager] No catalog entry for \"{typeName}\".");
            return;
        }
        if (!LevelManager.Instance.SpendMoney(toyData.Cost))
        {
            Debug.Log($"[LaunchManager] Cannot afford {typeName} (costs {toyData.Cost}).");
            return;
        }

        _loadedToyPrefab = toyData.TowerPrefab.prefab;
        Debug.Log($"[LaunchManager] Loaded {typeName}, ready to launch.");
        toyInstance = Instantiate(_loadedToyPrefab, transform.position, transform.rotation);
        toyInstance.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
    }

    // Called by PlayerBManager when B presses Space to throw.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestLaunchRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (PlayerSessionData.Instance.playerBSlotOwner.Value != senderId) return; // only B can pull the trigger

        Launch();
        playerA.isRecordingHeight.Value = false;
        playerB.isRecordingHeight.Value = false;
    }

    void Launch()
    {
        if (_loadedToyPrefab == null)
        {
            Debug.Log($"[LaunchManager] Would launch at speed {launchSpeed:F1} along current elevation, but no toy is loaded yet.");
            return;
        }

        Rigidbody rb = toyInstance?.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = transform.forward * launchSpeed; // transform already reflects live elevation + B's yaw
        }

        Debug.Log($"[LaunchManager] Launched {_loadedToyPrefab.name} at speed {launchSpeed:F1}");
        _loadedToyPrefab = null;
    }
}
