using Unity.Collections;
using Unity.Netcode;

public class PlayerSessionData : NetworkBehaviour
{
    public static PlayerSessionData Instance { get; private set; }

    public const ulong Unclaimed = ulong.MaxValue;

    // Which clientId currently owns each character slot. Set by Character Select,
    // read again whenever a level spawns Player A/B to know who gets ownership.
    public NetworkVariable<ulong> playerASlotOwner = new NetworkVariable<ulong>(
        Unclaimed, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> playerBSlotOwner = new NetworkVariable<ulong>(
        Unclaimed, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Which cutscene clip to play next. Set by the server (e.g. results screen) before
    // it loads the Cutscene scene; each machine resolves this name against its own local
    // VideoClip list once the scene loads — the clip itself never travels over the network.
    public NetworkVariable<FixedString32Bytes> pendingCutsceneName = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called server-side once Character Select has already decided who gets which slot.
    // No conflict checking here — that logic belongs to whatever calls this.
    public void SetSlotOwner(bool isSlotA, ulong clientId)
    {
        if (isSlotA) playerASlotOwner.Value = clientId;
        else playerBSlotOwner.Value = clientId;
    }
}
