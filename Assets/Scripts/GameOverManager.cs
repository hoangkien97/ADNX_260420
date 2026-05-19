using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private RankingManager rankingManager;

    [SerializeField] private GameObject restartButtonObj;

    private void Start()
    {
        if (rankingManager == null)
            rankingManager = FindAnyObjectByType<RankingManager>();

        if (rankingManager != null)
            rankingManager.RefreshUI();

        ApiManager api = ApiManager.EnsureInstance();
        api.PostScore(GameManager.Score, GameManager.Wave, success =>
        {
            if (rankingManager != null)
            {
                rankingManager.RefreshUI();
            }
        });

        // Ẩn nút Restart nếu là Client
        if (PurrNet.NetworkManager.main != null && PurrNet.NetworkManager.main.isClient && !PurrNet.NetworkManager.main.isServer)
        {
            if (restartButtonObj != null)
                restartButtonObj.SetActive(false);
        }
    }
    public void GoMainMenu()
    {
        NetworkBootstrap bootstrap = NetworkBootstrap.Instance
            ?? FindAnyObjectByType<NetworkBootstrap>();
            
        if (bootstrap != null)
        {
            bootstrap.DisconnectAndLoad("GameStart");
        }
        else
        {
            SceneManager.LoadScene("GameStart");
        }
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void RestartGame()
    {
        GameManager.ResetRunState();
        Time.timeScale = 1f;
        GoMainMenu();
    }

}
