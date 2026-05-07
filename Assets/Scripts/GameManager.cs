using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PurrNet;

/// <summary>
/// GameManager với PurrNet multiplayer support.
/// - Score và Wave: Server ghi, tất cả clients đọc (SyncVar)
/// - CountCoin: local per-player (shop riêng từng người)
/// - BonusSpeed, BonusDamage, BonusMaxHP: vẫn static vì per-player (shop riêng)
/// - Pause: chỉ dừng local client, không sync
/// </summary>
public class GameManager : NetworkBehaviour
{
    public const float DefaultBonusSpeed  = 0f;
    public const float DefaultBonusDamage = 0f;
    public const float DefaultBonusMaxHP  = 0f;
    public const int   DefaultCoinCount   = 0;
    public const int   DefaultScore       = 0;
    public const int   DefaultWave        = 1;

    [SerializeField] private Text txtCoin;
    [SerializeField] private Text txtScore;

    // Coin: local per-player (không sync)
    private static int countCoin = 0;

    // Score và Wave: sync qua SyncVar (Server ghi)
    [SerializeField] private SyncVar<int> syncScore = new SyncVar<int>(0, ownerAuth: false);
    [SerializeField] private SyncVar<int> syncWave  = new SyncVar<int>(1, ownerAuth: false);

    [SerializeField] private GameObject pausePanel;
    private bool isPaused = false;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle sfxToggle;
    private static GameManager instance;

    // Bonus stats: per-player static (shop riêng từng người)
    public static float BonusSpeed  = DefaultBonusSpeed;
    public static float BonusDamage = DefaultBonusDamage;
    public static float BonusMaxHP  = DefaultBonusMaxHP;

    // ─────────────────── COIN (LOCAL) ────────────────────────

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

    // ─────────────────── SCORE (SYNCED) ──────────────────────

    public static int Score
    {
        get => instance != null ? instance.syncScore.value : 0;
        set
        {
            if (instance == null) return;
            // Chỉ server mới ghi SyncVar (hoặc offline)
            if (instance.isSpawned && !instance.isServer) return;
            instance.syncScore.value = Mathf.Max(DefaultScore, value);
        }
    }

    // ─────────────────── WAVE (SYNCED) ───────────────────────

    public static int Wave
    {
        get => instance != null ? instance.syncWave.value : DefaultWave;
        private set
        {
            if (instance == null) return;
            if (instance.isSpawned && !instance.isServer) return;
            instance.syncWave.value = Mathf.Max(DefaultWave, value);
        }
    }

    // ─────────────────── NETWORK LIFECYCLE ───────────────────

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        syncScore.onChanged += OnScoreChanged;
        syncWave.onChanged  += OnWaveChanged;

        UpdateScoreText();
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        syncScore.onChanged -= OnScoreChanged;
        syncWave.onChanged  -= OnWaveChanged;
    }

    // ─────────────────── UNITY LIFECYCLE ─────────────────────

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

        if (pausePanel != null) pausePanel.SetActive(false);
        if (shopPanel  != null) shopPanel.SetActive(false);

        SetupAudioUI();
    }

    private void SetupAudioUI()
    {
        AudioManager am = FindAnyObjectByType<AudioManager>();
        if (am == null) return;

        if (musicSlider != null)
        {
            try { musicSlider.value = am.GetMusicVolume(); } catch { }
            musicSlider.onValueChanged.AddListener(am.SetMusicVolume);
        }
        if (sfxToggle != null)
        {
            try { sfxToggle.isOn = am.GetSfxEnabled(); } catch { }
            sfxToggle.onValueChanged.AddListener(am.SetSfxEnabled);
        }
    }

    private void Update()
    {
        bool isShopOpen = shopPanel != null && shopPanel.activeInHierarchy;
        if (!isPaused && !isShopOpen && Time.timeScale == 0f)
            Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // ─────────────────── STATIC METHODS ──────────────────────

    public static void UpdateCoin() => CountCoin++;

    public static void AddScore(int amount = 1) => Score += amount;

    public static void AdvanceWave()
    {
        if (instance == null) return;
        if (instance.isSpawned && !instance.isServer) return;
        instance.syncWave.value = Mathf.Max(DefaultWave, instance.syncWave.value + 1);
    }

    public static void ResetRunState()
    {
        BonusSpeed  = DefaultBonusSpeed;
        BonusDamage = DefaultBonusDamage;
        BonusMaxHP  = DefaultBonusMaxHP;
        CountCoin   = DefaultCoinCount;

        // Score và Wave reset qua instance (server)
        if (instance != null)
        {
            if (!instance.isSpawned || instance.isServer)
            {
                instance.syncScore.value = DefaultScore;
                instance.syncWave.value  = DefaultWave;
            }
        }
    }

    // ─────────────────── PAUSE ───────────────────────────────

    public void TogglePause()
    {
        if (!isPaused && Time.timeScale == 0f) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (pausePanel != null)
            pausePanel.SetActive(isPaused);
    }

    public void GoMainMenu()
    {
        ResetRunState();
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameStart");
    }

    // ─────────────────── SHOP UPGRADES ───────────────────────

    public void UpgradeSpeed(float amount)
    {
        BonusSpeed += amount;
        Player player = FindAnyObjectByType<Player>();
        if (player != null) player.AddSpeed(amount);
    }

    public void UpgradeDamage(float amount) => BonusDamage += amount;

    public void UpgradeMaxHP(float amount)
    {
        BonusMaxHP += amount;
        Player player = FindAnyObjectByType<Player>();
        if (player != null) player.AddMaxHP(amount);
    }

    // ─────────────────── UI CALLBACKS ────────────────────────

    private void OnScoreChanged(int newScore) => UpdateScoreText();
    private void OnWaveChanged(int newWave)   => UpdateScoreText();

    private void UpdateCoinText()
    {
        if (txtCoin != null)
            txtCoin.text = countCoin.ToString();
    }

    private void UpdateScoreText()
    {
        if (txtScore != null)
            txtScore.text = (isSpawned ? syncScore.value : 0).ToString();
    }
}
