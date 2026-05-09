using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private RankingManager rankingManager;

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
    }
    public void GoMainMenu()
    {
        NetworkBootstrap bootstrap = NetworkBootstrap.Instance
            ?? FindAnyObjectByType<NetworkBootstrap>();
        bootstrap?.Disconnect();

        SceneManager.LoadScene("GameStart");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void RestartGame()
    {
        GameManager.ResetRunState();
        Time.timeScale = 1f;

        NetworkBootstrap bootstrap = NetworkBootstrap.Instance
            ?? FindAnyObjectByType<NetworkBootstrap>();

        if (bootstrap != null)
        {
            bootstrap.RestartAsHost("SampleScene");
        }
        else
        {
            SceneManager.LoadScene("SampleScene");
        }
    }
}
