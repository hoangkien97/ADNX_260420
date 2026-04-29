using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public const float DefaultBonusSpeed = 0f;
    public const float DefaultBonusDamage = 0f;
    public const float DefaultBonusMaxHP = 0f;
    public const int DefaultCoinCount = 0;
    public const int DefaultScore = 0;
    public const int DefaultWave = 1;

    [SerializeField] private Text txtCoin;
    [SerializeField] private Text txtScore;
    private static int countCoin = 0;
    private static int score = 0;
    private static int wave = DefaultWave;
    [SerializeField] private GameObject pausePanel;
    private bool isPaused = false;
    [SerializeField] private GameObject shopPanel;
    private static GameManager instance;

    public static float BonusSpeed = DefaultBonusSpeed;
    public static float BonusDamage = DefaultBonusDamage;
    public static float BonusMaxHP = DefaultBonusMaxHP;

    public static int CountCoin
    {
        get => countCoin;
        set
        {
            countCoin = value;
            PlayerPrefs.SetInt("countCoin", countCoin);
            PlayerPrefs.Save();
            instance?.UpdateCoinText();
        }
    }

    public static int Score
    {
        get => score;
        set
        {
            score = Mathf.Max(DefaultScore, value);
            instance?.UpdateScoreText();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureUnpausedAfterSceneLoad()
    {
        Time.timeScale = 1f;
    }

    private void Awake()
    {
        instance = this;
        Time.timeScale = 1f;
        isPaused = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;
        countCoin = PlayerPrefs.GetInt("countCoin", DefaultCoinCount);
        UpdateCoinText();
        UpdateScoreText();
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    public static int Wave
    {
        get => wave;
        private set => wave = Mathf.Max(DefaultWave, value);
    }

    private void Update()
    {
        bool isShopOpen = shopPanel != null && shopPanel.activeInHierarchy;
        if (!isPaused && !isShopOpen && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    public static void UpdateCoin()
    {
        CountCoin++;
    }

    public static void AddScore(int amount = 1)
    {
        Score += amount;
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
        ResetRunState();
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameStart");
    }

    public static void ResetRunState()
    {
        BonusSpeed = DefaultBonusSpeed;
        BonusDamage = DefaultBonusDamage;
        BonusMaxHP = DefaultBonusMaxHP;
        CountCoin = DefaultCoinCount;
        Score = DefaultScore;
        Wave = DefaultWave;
    }

    public static void AdvanceWave()
    {
        Wave++;
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

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void UpdateCoinText()
    {
        if (txtCoin != null)
        {
            txtCoin.text = countCoin.ToString();
        }
    }

    private void UpdateScoreText()
    {
        if (txtScore != null)
        {
            txtScore.text = score.ToString();
        }
    }

}

