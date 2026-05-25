using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject gamePanel;
    [SerializeField] Slider healthSlider;
    [SerializeField] Text scoreText;
    [SerializeField] InputField playerNameInput;

    float health = 100f;
    int score;

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

    void Start()
    {
        if (loginPanel != null)
            loginPanel.SetActive(true);
        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (MVRApp.SceneManager.Instance != null)
        {
            score = MVRApp.SceneManager.Instance.Score;
            health = MVRApp.SceneManager.Instance.Health;
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 100f;
            healthSlider.value = health;
        }

        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (MVRApp.SceneManager.Instance != null)
            MVRApp.SceneManager.Instance.Score = score;
        RefreshUI();
    }

    public void SetHealth(float value)
    {
        health = Mathf.Clamp(value, 0f, 100f);
        if (MVRApp.SceneManager.Instance != null)
            MVRApp.SceneManager.Instance.Health = health;
        RefreshUI();
    }

    public string GetPlayerName()
    {
        if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
            return playerNameInput.text.Trim();
        return "Player";
    }

    public void OnMenuButton()
    {
        NetworkManager network = NetworkManager.Instance;
        if (network != null)
            network.Disconnect();
    }
}
