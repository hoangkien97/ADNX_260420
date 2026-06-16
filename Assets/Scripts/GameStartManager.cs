using UnityEngine;
using UnityEngine.SceneManagement;
using PurrNet;

public class GameStartManager : MonoBehaviour
{
    private void Start()
    {
        // Initialization handled by AppBootstrap
    }

    public void GameStart()
    {
        // Initialization handled by AppBootstrap
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
        ServiceLocator.Get<IAuthService>().Logout();
        SceneManager.LoadScene("Login");
    }

    public void QuitGame()
    {
        EnemyDataManager.Instance?.Save();
        Application.Quit();
    }
}
