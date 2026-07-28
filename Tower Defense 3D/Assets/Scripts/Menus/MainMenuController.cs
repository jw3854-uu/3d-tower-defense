using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;

    // Testing
    [Header("LAN Connection")]
    [SerializeField] UnityTransport transport;
    [Tooltip("Client types the host's LAN IP here (e.g. 192.168.1.42) before clicking Join.")]
    [SerializeField] TMP_InputField hostIpInputField;

    [Header("Join Code (Relay 占位，先不生效)")]
    [SerializeField] TMP_InputField joinCodeInputField;

    [Header("Status")]
    [SerializeField] TextMeshProUGUI statusText;

    [Header("Next Scene")]
    [SerializeField] string characterSelectSceneName = "Character Select";

    [Header("Session Data")]
    [SerializeField] NetworkObject playerSessionDataPrefab;

    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnHostClicked()
    {
        // Listen on all network interfaces, not just localhost, so another machine on the LAN can reach it.
        transport.ConnectionData.ServerListenAddress = "0.0.0.0";

        statusText.text = "创建房间中…";
        NetworkManager.Singleton.StartHost();

        var instance = Instantiate(playerSessionDataPrefab);
        instance.Spawn();
    }

    void OnJoinClicked()
    {
        // TODO: 以后接 Relay，这里改成先 JoinAllocationAsync(joinCodeInputField.text)
        transport.ConnectionData.Address = hostIpInputField.text;

        statusText.text = "连接中…";
        NetworkManager.Singleton.StartClient();
    }

    void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (NetworkManager.Singleton.ConnectedClientsIds.Count < 2) return;

        // 两人都连上了，server 端发起同步场景切换
        NetworkManager.Singleton.SceneManager.LoadScene(characterSelectSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
