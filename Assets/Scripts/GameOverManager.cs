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
        SceneManager.LoadScene("GameStart");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    public void RestartGame()
    {
        GameManager.ResetRunState();
        SceneManager.LoadScene("SampleScene");
    }
}
