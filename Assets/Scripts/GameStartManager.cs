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
        if (!ApiManager.IsLoggedIn)
        {
            SceneManager.LoadScene("Login");
            return;
        }

        GameManager.ResetRunState();
        SceneManager.LoadScene("SampleScene");
    }

    public void Logout()
    {
        ApiManager.EnsureInstance().Logout();
        SceneManager.LoadScene("Login");
    }
}
