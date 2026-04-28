using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Stats")]
    [SerializeField] int startingHealth = 3;
    [SerializeField] int startingMoney = 100;

    public int PlayerHealth { get; private set; }
    public int PlayerMoney { get; private set; }

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
        PlayerHealth = startingHealth;
        PlayerMoney = startingMoney;
        healthUI.text = $"Health: {PlayerHealth}";
        moneyUI.text = $"Money: {PlayerMoney}";

        gameOverPanel.SetActive(false);
        startOverButton.onClick.AddListener(OnStartOver);
        closeButton.onClick.AddListener(OnClose);
    }

    public void EnemyReachedEnd()
    {
        PlayerHealth--;
        healthUI.text = $"Health: {PlayerHealth}";
        // Debug.Log($"[GameManager] Enemy reached the end! Health: {PlayerHealth}/{startingHealth}");
        if (PlayerHealth <= 0)
        {
            Debug.Log("[GameManager] Game over!");
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }
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

    // Returns false if the player cannot afford it
    public bool SpendMoney(int amount)
    {
        if (PlayerMoney < amount)
        {
            Debug.Log($"[GameManager] Not enough money. Have {PlayerMoney}, need {amount}.");
            return false;
        }
        PlayerMoney -= amount;
        moneyUI.text = $"Money: {PlayerMoney}";
        // Debug.Log($"[GameManager] Spent {amount}. Money: {PlayerMoney}");
        return true;
    }

    public void AddMoney(int amount)
    {
        PlayerMoney += amount;
        moneyUI.text = $"Money: {PlayerMoney}";
        // Debug.Log($"[GameManager] +{amount} money. Money: {PlayerMoney}");
    }
}
