using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] GameObject PlayerAPrefab;
    [SerializeField] GameObject PlayerBPrefab;
    [SerializeField] Transform spawnPointA;
    [SerializeField] Transform spawnPointB;
    [SerializeField] GameObject launcherPrefab;
    [SerializeField] Transform launcherSpawnPoint;
    [SerializeField] EnemyManager enemyManager;

    [Header("Player Stats")]
    [SerializeField] int startingHealth = 3;
    [SerializeField] int startingMoney = 100;

    // Server-writable, shared team stats — every client's HUD just reacts to OnValueChanged.
    NetworkVariable<int> _playerHealth = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<int> _playerMoney = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int PlayerHealth => _playerHealth.Value;
    public int PlayerMoney => _playerMoney.Value;

    [Header("HUD")]
    public TextMeshProUGUI healthUI;
    public TextMeshProUGUI moneyUI;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Button startOverButton;
    public Button closeButton;

    void Awake()
    {
        Instance = this;
        gameOverPanel.SetActive(false);
        startOverButton.onClick.AddListener(OnStartOver);
        closeButton.onClick.AddListener(OnClose);
    }

    public override void OnNetworkSpawn()
    {
        _playerHealth.OnValueChanged += (oldVal, newVal) =>
        {
            healthUI.text = $"Health: {newVal}";
            // Every machine reacts locally so the game-over panel shows on both screens,
            // even though only the server decides when health actually drops.
            if (newVal <= 0)
            {
                Debug.Log("[LevelManager] Game over!");
                gameOverPanel.SetActive(true);
                Time.timeScale = 0f;
            }
        };
        _playerMoney.OnValueChanged += (oldVal, newVal) => moneyUI.text = $"Money: {newVal}";
        healthUI.text = $"Health: {_playerHealth.Value}";
        moneyUI.text = $"Money: {_playerMoney.Value}";

        if (!IsServer) return;

        _playerHealth.Value = startingHealth;
        _playerMoney.Value = startingMoney;

        // Wait until the load event is fully finished for everyone before spawning anything.
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;

        SpawnPlayers();
        enemyManager.BeginWaves();
    }

    void SpawnPlayers()
    {
        var a = Instantiate(PlayerAPrefab, spawnPointA.position, spawnPointA.rotation);
        a.GetComponent<NetworkObject>().SpawnWithOwnership(PlayerSessionData.Instance.playerASlotOwner.Value);
        var b = Instantiate(PlayerBPrefab, spawnPointB.position, spawnPointB.rotation);
        b.GetComponent<NetworkObject>().SpawnWithOwnership(PlayerSessionData.Instance.playerBSlotOwner.Value);
        var launcher = Instantiate(launcherPrefab, launcherSpawnPoint.position, launcherSpawnPoint.rotation);
        launcher.GetComponent<NetworkObject>().Spawn();
    }

    public void EnemyReachedEnd()
    {
        _playerHealth.Value--;
    }

    void OnStartOver()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnClose()
    {
        gameOverPanel.SetActive(false);
        // timeScale stays 0 — game remains frozen in lose state
    }

    // Returns false if the player cannot afford it. Server-only — writes to _playerMoney
    // require server authority, same as health.
    public bool SpendMoney(int amount)
    {
        if (_playerMoney.Value < amount)
        {
            Debug.Log($"[LevelManager] Not enough money. Have {_playerMoney.Value}, need {amount}.");
            return false;
        }
        _playerMoney.Value -= amount;
        return true;
    }

    public void AddMoney(int amount)
    {
        _playerMoney.Value += amount;
    }
}
