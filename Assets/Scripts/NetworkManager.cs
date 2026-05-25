using Mirror;
using UnityEngine;

public class NetworkManager : Mirror.NetworkManager
{
    public static NetworkManager Instance { get; private set; }

    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject gamePanel;

    public override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        base.Awake();
    }

    public new void StartHost()
    {
        if (NetworkServer.active || NetworkClient.active)
            return;

        networkAddress = "localhost";
        base.StartHost();
    }

    public new void StartClient()
    {
        if (NetworkClient.active)
            return;

        networkAddress = "localhost";
        base.StartClient();
    }

    public void OnConnected()
    {
        if (loginPanel != null)
            loginPanel.SetActive(false);
        if (gamePanel != null)
            gamePanel.SetActive(true);

        if (UIManager.Instance != null)
            UIManager.Instance.RefreshUI();

        if (MVRApp.SceneManager.Instance != null &&
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameScene")
            MVRApp.SceneManager.Instance.LoadGameScene();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        OnConnected();
    }

    public void Disconnect()
    {
        if (NetworkClient.active)
            StopClient();
        if (NetworkServer.active)
            StopServer();

        if (loginPanel != null)
            loginPanel.SetActive(true);
        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (MVRApp.SceneManager.Instance != null)
            MVRApp.SceneManager.Instance.LoadMainMenu();
    }
}
