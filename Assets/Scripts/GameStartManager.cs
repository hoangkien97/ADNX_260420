using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    private void Start()
    {
        ApiManager.EnsureInstance();
    }

    public void GameStart()
    {
        ApiManager.EnsureInstance();
        GameManager.ResetRunState();
        SceneManager.LoadScene("SampleScene");
    }

    public void Logout()
    {
        ApiManager.EnsureInstance().Logout();
        SceneManager.LoadScene("Login");
    }
    public void QuitGame()
    {
        EnemyDataManager.Instance?.Save();
        Application.Quit();
    }
}
