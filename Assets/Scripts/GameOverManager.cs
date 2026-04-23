using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
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
        GameManager.CountCoin = 0;
        SceneManager.LoadScene("SampleScene");
    }
}
