using System.Linq;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelectController : NetworkBehaviour
{
    private NetworkVariable<bool> playerAReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> playerBReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI slotAVisual;
    [SerializeField] private TextMeshProUGUI slotBVisual;
    [SerializeField] private Button swapSlotButton;
    [SerializeField] private Button readyButton;

    [Header("Swap Request UI")]
    [SerializeField] private GameObject swapRequestPanel;
    [SerializeField] private Button acceptSwapButton;
    [SerializeField] private Button declineSwapButton;

    // Server-only bookkeeping for whichever swap request is currently in flight.
    private bool _swapRequestPending;
    private ulong _pendingResponderId;

    public override void OnNetworkSpawn()
    {
        PlayerSessionData.Instance.playerASlotOwner.OnValueChanged += OnSlotAChanged;
        PlayerSessionData.Instance.playerBSlotOwner.OnValueChanged += OnSlotBChanged;
        playerAReady.OnValueChanged += OnPlayerAReadyChanged;
        playerBReady.OnValueChanged += OnPlayerBReadyChanged;
        swapSlotButton.onClick.AddListener(() => RequestSwapServerRpc());
        acceptSwapButton.onClick.AddListener(() =>
        {
            RespondToSwapRpc(true);
            swapRequestPanel.SetActive(false);
            swapSlotButton.interactable = true;
        });
        declineSwapButton.onClick.AddListener(() =>
        {
            RespondToSwapRpc(false);
            swapRequestPanel.SetActive(false);
            swapSlotButton.interactable = true;
        });
        readyButton.onClick.AddListener(() => SetReadyRpc());

        swapRequestPanel.SetActive(false);

        OnSlotAChanged(PlayerSessionData.Unclaimed, PlayerSessionData.Instance.playerASlotOwner.Value);
        OnSlotBChanged(PlayerSessionData.Unclaimed, PlayerSessionData.Instance.playerBSlotOwner.Value);

        // Choosing role
        // Default to: player A - host, player B - client
        // Ask for swap button, the other player can admit or deny the swap request
        if (!IsServer) return;

        ulong hostId = NetworkManager.ServerClientId;
        ulong clientId = NetworkManager.ConnectedClientsIds.First(id => id != hostId);

        PlayerSessionData.Instance.SetSlotOwner(true, hostId);    // host 默认是 A
        PlayerSessionData.Instance.SetSlotOwner(false, clientId); // client 默认是 B
    }

    void OnSlotAChanged(ulong oldOwner, ulong newOwner)
    {
        // slotAVisual.SetOccupied(newOwner != PlayerSessionData.Unclaimed);
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        if (clientId == newOwner)
            slotAVisual.SetText($"Player A: Me (Not Ready)");
        else
            slotAVisual.SetText($"Player A: Other (Not Ready)");
    }

    void OnSlotBChanged(ulong oldOwner, ulong newOwner)
    {
        // slotBVisual.SetOccupied(newOwner != PlayerSessionData.Unclaimed);
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        if (clientId == newOwner)
            slotBVisual.SetText($"Player B: Me (Not Ready)");
        else
            slotBVisual.SetText($"Player B: Other (Not Ready)");
    }

    // Swapping roles
    // Step 1: requester asks the server to start a swap.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestSwapServerRpc(RpcParams rpcParams = default)
    {
        if (_swapRequestPending) return; // one at a time

        ulong requesterId = rpcParams.Receive.SenderClientId;
        var data = PlayerSessionData.Instance;
        Debug.Log($"[CharacterSelectController] Swap request from {requesterId}. A owner: {data.playerASlotOwner.Value}, B owner: {data.playerBSlotOwner.Value}");

        ulong responderId = data.playerASlotOwner.Value == requesterId
            ? data.playerBSlotOwner.Value
            : data.playerASlotOwner.Value;

        if (responderId == PlayerSessionData.Unclaimed) return;

        _swapRequestPending = true;
        _pendingResponderId = responderId;

        Debug.Log($"[CharacterSelectController] Showing swap request to {responderId}");
        ShowSwapRequestRpc(RpcTarget.Single(responderId, RpcTargetUse.Temp));
    }

    // Step 2: only the requested-of player receives this and sees the prompt.
    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowSwapRequestRpc(RpcParams rpcParams = default)
    {
        swapRequestPanel.SetActive(true);
        swapSlotButton.interactable = false;
    }

    // Step 3: responder's Accept/Decline button calls this.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RespondToSwapRpc(bool accepted, RpcParams rpcParams = default)
    {
        if (!_swapRequestPending) return;
        if (rpcParams.Receive.SenderClientId != _pendingResponderId) return; // only the asked player can answer

        _swapRequestPending = false;

        if (accepted)
        {
            var data = PlayerSessionData.Instance;
            ulong ownerA = data.playerASlotOwner.Value;
            ulong ownerB = data.playerBSlotOwner.Value;
            data.SetSlotOwner(true, ownerB);
            data.SetSlotOwner(false, ownerA);
            // Reset ready states on swap
            playerAReady.Value = false;
            playerBReady.Value = false;
            // slotAVisual/slotBVisual update automatically via OnSlotAChanged/OnSlotBChanged
            Debug.Log($"[CharacterSelectController] Swap accepted. A owner: {data.playerASlotOwner.Value}, B owner: {data.playerBSlotOwner.Value}");
        }
    }

    // Confirmation:
    // Register to Player Session Data, then load the next scene
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetReadyRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (PlayerSessionData.Instance.playerASlotOwner.Value == clientId){
            if (playerAReady.Value) playerAReady.Value = false;
            else playerAReady.Value = true;
        }else if (PlayerSessionData.Instance.playerBSlotOwner.Value == clientId){
            if (playerBReady.Value) playerBReady.Value = false;
            else playerBReady.Value = true;
        } 
        TryStartGame(); // 每次 ready 状态变化,都顺手检查一下能不能进下一步
    }

    void TryStartGame()
    {
        var data = PlayerSessionData.Instance;
        bool bothClaimed = data.playerASlotOwner.Value != PlayerSessionData.Unclaimed
                         && data.playerBSlotOwner.Value != PlayerSessionData.Unclaimed;
        bool bothReady = playerAReady.Value && playerBReady.Value;

        if (bothClaimed && bothReady){
            // 如果是第一次游戏，播放动画：
            PlayerSessionData.Instance.pendingCutsceneName.Value = "Intro.mp4";
            NetworkManager.Singleton.SceneManager.LoadScene("Cutscene", LoadSceneMode.Single);
            // 如果不是：NetworkManager.Singleton.SceneManager.LoadScene("Level1", LoadSceneMode.Single);
            Debug.Log("[CharacterSelectController] Both players claimed and ready, would load next scene now.");
        }
    }

    void OnPlayerAReadyChanged(bool oldValue, bool newValue)
    {
        // check if client id is the owner of slot A, if yes, show me ready
        // if not, show teammate ready
        // slotAVisual.SetReady(newValue);
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        if (clientId == PlayerSessionData.Instance.playerASlotOwner.Value)
            slotAVisual.SetText($"Player A: Me {(newValue ? "(Ready)" : "(Not Ready)")}");
        else
            slotAVisual.SetText($"Player A: Other {(newValue ? "(Ready)" : "(Not Ready)")}");
    }

    void OnPlayerBReadyChanged(bool oldValue, bool newValue)
    {
        // check if client id is the owner of slot B, if yes, show me ready
        // if not, show teammate ready
        // slotBVisual.SetReady(newValue);
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        if (clientId == PlayerSessionData.Instance.playerBSlotOwner.Value)
            slotBVisual.SetText($"Player B: Me {(newValue ? "(Ready)" : "(Not Ready)")}");
        else
            slotBVisual.SetText($"Player B: Other {(newValue ? "(Ready)" : "(Not Ready)")}");
    }

}
