using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    public void GameStart()
    {
        ResetPlayerStats();
        SceneManager.LoadScene("SampleScene");
    }
    public void Quit()
    {
        Application.Quit();
    }

    private void ResetPlayerStats()
    {
        PlayerPrefs.SetFloat("moveSpeed", 5f);
        PlayerPrefs.SetFloat("maxHP", 500f);
        PlayerPrefs.SetFloat("damage", 20f);
        PlayerPrefs.SetInt("countCoin", 0);
        //GameManager.Level = 1;
    }
}