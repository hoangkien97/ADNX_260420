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
            // Mọi người đều thử làm Host. 
            // Ai bấm trước -> Chiếm được cổng mạng -> Làm Host.
            // Ai bấm sau -> Bị kẹt cổng mạng -> NetworkBootstrap tự động bắt lỗi và chuyển thành Client.
            bootstrap.StartHostAndLoad("SampleScene");
        }
        else
        {
            // Không có network → load thẳng như cũ
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
