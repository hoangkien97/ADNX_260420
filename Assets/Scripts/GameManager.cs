using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Text txtCoin;
    private static int countCoin = 0;
    [SerializeField] private GameObject pausePanel;
    private bool isPaused = false;

    public static int CountCoin
    {
        get => countCoin;
        set
        {
            countCoin = value;
            PlayerPrefs.SetInt("countCoin", countCoin);
            PlayerPrefs.Save();
        }
    }

    private void Start()
    {
        countCoin = PlayerPrefs.GetInt("countCoin", 0);
        if (txtCoin != null)
            txtCoin.text = countCoin.ToString();
        pausePanel.SetActive(false);
    }

    public static void UpdateCoin()
    {
        CountCoin++;
    }
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pausePanel.SetActive(isPaused);
    }

}

