using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Text txtCoin;
    private static int countCoin = 0;
    [SerializeField] private GameObject pausePanel;
    private bool isPaused = false;

    public static float BonusSpeed = 0f;
    public static float BonusDamage = 0f;
    public static float BonusMaxHP = 0f;

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
        if (!isPaused && Time.timeScale == 0f)
        {
            return;
        }

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pausePanel.SetActive(isPaused);
    }

    public void GoMainMenu()
    {
        BonusSpeed = 0f;
        BonusDamage = 0f;
        BonusMaxHP = 0f;
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameStart");
    }


    public void UpgradeSpeed(float amount)
    {
        BonusSpeed += amount;
        Player player = FindAnyObjectByType<Player>();
        if (player != null) player.AddSpeed(amount);
    }

    public void UpgradeDamage(float amount)
    {
        BonusDamage += amount;
    }

    public void UpgradeMaxHP(float amount)
    {
        BonusMaxHP += amount;
        Player player = FindAnyObjectByType<Player>();
        if (player != null) player.AddMaxHP(amount);
    }

}

