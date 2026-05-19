using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet;

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

        NetworkBootstrap bootstrap = NetworkBootstrap.Instance
            ?? FindAnyObjectByType<NetworkBootstrap>();

        if (bootstrap != null)
        {
            // Ai bấm trước -> Chiếm được cổng mạng -> Làm Host.
            bootstrap.StartHostAndLoad("SampleScene");
        }
        else
        {
            SceneManager.LoadScene("SampleScene");
        }
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
