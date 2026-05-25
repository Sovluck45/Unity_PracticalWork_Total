using UnityEngine;
using UnityEngine.SceneManagement;

namespace MVRApp
{
    public class SceneManager : MonoBehaviour
    {
        public static SceneManager Instance { get; private set; }

        public int Score { get; set; }
        public float Health { get; set; } = 100f;

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

        public void LoadMainMenu()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        public void LoadGameScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }
    }
}
