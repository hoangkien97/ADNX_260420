using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    public void GameStart()
    {
        ResetRunState();
        SceneManager.LoadScene("SampleScene");
    }
    public void Quit()
    {
        Application.Quit();
    }

    private void ResetRunState()
    {
        GameManager.ResetRunState();
    }
}
