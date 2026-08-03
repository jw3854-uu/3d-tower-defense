using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class LevelMenuManager : NetworkBehaviour
{
    [SerializeField] Button level1Button;

    public override void OnNetworkSpawn()
    {
        level1Button.onClick.AddListener(() => LoadLevelRpc("Level1 New"));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void LoadLevelRpc(string sceneName, RpcParams rpcParams = default)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
