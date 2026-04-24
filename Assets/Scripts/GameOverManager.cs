using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private RankingManager rankingManager;

    private void Start()
    {
        bool isNewRecord;
        RankingManager.SaveScore(GameManager.Score, out isNewRecord);

        if (rankingManager == null)
            rankingManager = FindAnyObjectByType<RankingManager>();

        if (rankingManager != null)
            rankingManager.RefreshUI(isNewRecord); 
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
