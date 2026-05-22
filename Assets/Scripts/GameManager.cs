using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PurrNet;

/// <summary>
/// GameManager với PurrNet multiplayer support.
/// - Score: local per-player (mỗi người có điểm riêng)
/// - Wave: Global (đồng bộ chung cho toàn bộ phòng)
/// - CountCoin: local per-player (shop riêng từng người)
/// - BonusSpeed, BonusDamage, BonusMaxHP: vẫn static vì per-player (shop riêng)
/// - Pause: Server-authoritative – chỉ Server mới được pause, broadcast tới tất cả Client
/// </summary>
public class GameManager : NetworkBehaviour
{
    public const float DefaultBonusSpeed = 0f;
    public const float DefaultBonusDamage = 0f;
    public const float DefaultBonusMaxHP = 0f;
    public const int DefaultCoinCount = 0;
    public const int DefaultScore = 0;
    public const int DefaultWave = 1;

    [SerializeField] private Text txtCoin;
    [SerializeField] private Text txtScore;

    // Coin: local per-player (không sync)
    private static int countCoin = 0;

    // Score: local per-player (không sync, mỗi người có điểm riêng)
    private static int score = 0;

    // Wave: Global
    // - Online: đồng bộ qua SyncVar để late-joiner nhận đúng giá trị hiện tại
    // - Offline: dùng biến static local như cũ
    private static int wave = DefaultWave;
    [SerializeField] private SyncVar<int> syncWave = new SyncVar<int>(DefaultWave, ownerAuth: false);

    [SerializeField] private GameObject pausePanel;
    private bool isPaused = false;
    [SerializeField] private GameObject shopPanel;
    private bool isShopOpen = false;

    // Pause/Shop: server ghi, tất cả client nhận (bao gồm late joiner)
    [SerializeField] private SyncVar<bool> syncPaused   = new SyncVar<bool>(false, ownerAuth: false);
    [SerializeField] private SyncVar<bool> syncShopOpen = new SyncVar<bool>(false, ownerAuth: false);
    
    // Đồng bộ file JSON cấu hình từ Server xuống Client
    [SerializeField] private SyncVar<string> syncGameConfigJson = new SyncVar<string>("", ownerAuth: false);

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Toggle sfxToggle;
    private static GameManager instance;
    public static GameManager Instance => instance;

    // Bonus stats: per-player static (shop riêng từng người)
    public static float BonusSpeed = DefaultBonusSpeed;
    public static float BonusDamage = DefaultBonusDamage;
    public static float BonusMaxHP = DefaultBonusMaxHP;

    // ─────────────────── COIN (LOCAL) ────────────────────────

    public static int CountCoin
    {
        get => countCoin;
        set
        {
            countCoin = Mathf.Max(DefaultCoinCount, value);
            instance?.UpdateCoinText();
        }
    }

    // ─────────────────── SCORE (LOCAL PER-PLAYER) ─────────────

    public static int Score
    {
        get => score;
        set
        {
            score = Mathf.Max(DefaultScore, value);
            instance?.UpdateScoreText();
        }
    }

    // ─────────────────── WAVE (LOCAL PER-PLAYER) ─────────────

    public static int Wave
    {
        get
        {
            if (instance != null && instance.isSpawned)
                return instance.syncWave.value;
            return wave;
        }
        private set
        {
            int clamped = Mathf.Max(DefaultWave, value);

            if (instance != null && instance.isSpawned)
            {
                // Online mode: chỉ Server mới được phép ghi SyncVar ownerAuth:false
                if (instance.isServer)
                {
                    if (instance.syncWave.value != clamped)
                        instance.syncWave.value = clamped;
                }
                else
                {
                    // Client reset local state khi thoát room/menu, không đụng vào SyncVar
                    wave = clamped;
                }
            }
            else
            {
                wave = clamped;
            }

            instance?.UpdateScoreText();
        }
    }

    // ─────────────────── NETWORK LIFECYCLE ───────────────────

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer)
        {
            // Server đọc JSON từ local và nạp vào SyncVar để đẩy xuống Client
            if (EnemyDataManager.Instance != null)
            {
                syncGameConfigJson.value = EnemyDataManager.Instance.GetJsonString();
            }
        }
        else
        {
            // Client kết nối vào -> Lấy JSON từ Server đè lên JSON local
            if (!string.IsNullOrEmpty(syncGameConfigJson.value) && EnemyDataManager.Instance != null)
            {
                EnemyDataManager.Instance.LoadFromJsonString(syncGameConfigJson.value);
            }
        }

        // Đăng ký sự kiện khi JSON thay đổi (phòng trường hợp late joiner hoặc server cập nhật data)
        syncGameConfigJson.onChanged += OnGameConfigJsonChanged;

        syncPaused.onChanged += ApplyPause;
        syncShopOpen.onChanged += OnShopOpenChanged;
        
        // Late-joiner nhận ngay trạng thái hiện tại
        ApplyPause(syncPaused.value);
        ApplyShop(syncShopOpen.value);

        // Server đảm bảo giá trị hợp lệ tối thiểu
        if (asServer)
            syncWave.value = Mathf.Max(DefaultWave, syncWave.value);

        UpdateScoreText();
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        syncGameConfigJson.onChanged -= OnGameConfigJsonChanged;
        syncPaused.onChanged -= ApplyPause;
        syncShopOpen.onChanged -= OnShopOpenChanged;
    }

    private void OnGameConfigJsonChanged(string newJson)
    {
        if (!isServer && !string.IsNullOrEmpty(newJson) && EnemyDataManager.Instance != null)
        {
            EnemyDataManager.Instance.LoadFromJsonString(newJson);
        }
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
        UpdateCoinText();
        UpdateScoreText();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);

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
        //bool isShopOpen = shopPanel != null && shopPanel.activeInHierarchy;
        //if (!isPaused && !isShopOpen && Time.timeScale == 0f)
        //    Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // ─────────────────── STATIC METHODS ──────────────────────

    public static void UpdateCoin() => CountCoin++;


    /// <summary>
    /// Tăng Wave cho tất cả người chơi (gọi từ Server khi hết đợt quái).
    /// </summary>
    public static void AdvanceWave()
    {
        if (instance == null) return;

        if (instance.isSpawned && instance.isServer)
        {
            // Server tăng wave trong SyncVar, tất cả client (kể cả late joiner) sẽ nhận đúng state
            instance.syncWave.value = Mathf.Max(DefaultWave, instance.syncWave.value + 1);
            instance.UpdateScoreText();
        }
        else if (!instance.isSpawned)
        {
            // Offline mode
            Wave++;
        }
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

    // ─────────────────── PAUSE ───────────────────────────────

    /// <summary>
    /// Server-only: Broadcast pause tới tất cả Client qua RPC.
    /// </summary>
    public void TogglePause()
    {
        // Chỉ Server (Host) mới có quyền pause trong multiplayer
        if (isSpawned && !isServer) return;

        if (!isPaused && Time.timeScale == 0f) return;

        bool newPaused = !isPaused;

        if (isSpawned)
        {
            // Ghi SyncVar → PurrNet tự đồng bộ cho tất cả client (kể cả late joiner)
            syncPaused.value = newPaused;
            // Vẫn gửi RPC để áp dụng ngư thì lập trên host (vì onChanged chỉ trigger trên client)
            RpcSetPause(newPaused);
        }
        else
            ApplyPause(newPaused);    // Offline mode
    }

    /// <summary>
    /// Chỉ pause local (không gửi RPC). Dùng cho client đã chết cần hiện pause panel để Quit.
    /// Time.timeScale không được sync qua mạng nên chỉ ảnh hưởng máy này.
    /// </summary>
    public void TogglePauseLocal()
    {
        // Chỉ hiện/ẩn panel local để client đã chết có thể Quit.
        // Không được đụng vào isPaused (state đồng bộ từ server) và không đổi timeScale.
        if (pausePanel != null)
            pausePanel.SetActive(!pausePanel.activeSelf);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcSetPause(bool paused)
    {
        ApplyPause(paused);
    }

    private void ApplyPause(bool paused)
    {
        isPaused = paused;
        if (pausePanel != null)
            pausePanel.SetActive(isPaused);

        RefreshGlobalTimeScale();
    }

    public void GoMainMenu()
    {
        // Nếu là Server/Host, báo cho tất cả client biết để tụi nó tự thoát
        if (isSpawned && isServer)
        {
            RpcForceQuitToMenu();
            // Host cần đợi 1 chút để RPC bay tới client rồi mới thoát, nếu không client sẽ kẹt
            StartCoroutine(HostQuitCoroutine());
        }
        else
        {
            // Client thoát ngay
            ExecuteQuitLocal();
        }
    }

    private System.Collections.IEnumerator HostQuitCoroutine()
    {
        // Đợi một khoảng để RpcForceQuitToMenu kịp gửi qua mạng
        yield return new WaitForSecondsRealtime(0.2f);
        ExecuteQuitLocal();
    }

    [ObserversRpc(runLocally: false)]
    private void RpcForceQuitToMenu()
    {
        Debug.Log("[GameManager] Nhận lệnh từ Host: Bắt buộc quay về GameStart.");
        ExecuteQuitLocal();
    }

    private void ExecuteQuitLocal()
    {
        ResetRunState();
        Time.timeScale = 1f;

        NetworkBootstrap bootstrap = NetworkBootstrap.Instance ?? FindAnyObjectByType<NetworkBootstrap>();
        if (bootstrap != null)
        {
            bootstrap.DisconnectAndLoad("GameStart");
        }
        else
        {
            SceneManager.LoadScene("GameStart");
        }
    }

    // ─────────────────── SHOP UPGRADES ───────────────────────

    private Player GetLocalPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            // Trả về nếu là Owner (Multiplayer) hoặc nếu game offline (!isSpawned)
            if (!p.isSpawned || p.isOwner) return p;
        }
        return null;
    }

    public void UpgradeSpeed(float amount)
    {
        BonusSpeed += amount;
        Player player = GetLocalPlayer();
        if (player != null) player.AddSpeed(amount);
    }

    public void UpgradeDamage(float amount)
    {
        Player player = GetLocalPlayer();

        if (!isSpawned)
        {
            BonusDamage += amount;
        }

        if (player != null) player.AddDamage(amount);
    }

    public void UpgradeMaxHP(float amount)
    {
        BonusMaxHP += amount;
        Player player = GetLocalPlayer();
        if (player != null) player.AddMaxHP(amount);
    }

    // ─────────────────── SHOP NETWORK SYNC ─────────────────────

    public void OpenShopForAll()
    {
        if (!isServer) return;
        syncShopOpen.value = true;
        RpcOpenShop();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcOpenShop()
    {
        ApplyShop(true);
    }

    public void CloseShopForAll()
    {
        if (!isServer) return;
        syncShopOpen.value = false;
        RpcCloseShop();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcCloseShop()
    {
        ApplyShop(false);
    }

    private void ApplyShop(bool open)
    {
        isShopOpen = open;
        if (shopPanel != null) shopPanel.SetActive(open);

        RefreshGlobalTimeScale();
    }

    public void GrantBonusCoinForAll(int amount)
    {
        if (isServer) RpcGrantBonusCoin(amount);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcGrantBonusCoin(int amount)
    {
        CountCoin += amount;
    }




    // ─────────────────── UI CALLBACKS ────────────────────────

    private void UpdateCoinText()
    {
        if (txtCoin != null)
            txtCoin.text = countCoin.ToString();
    }

    private void UpdateScoreText()
    {
        if (txtScore != null)
            txtScore.text = score.ToString();
    }

    private void OnWaveChanged(int newWave)
    {
        wave = Mathf.Max(DefaultWave, newWave);
    }

    private void OnPausedChanged(bool paused)
    {
        ApplyPause(paused);
    }

    private void OnShopOpenChanged(bool open)
    {
        ApplyShop(open);
    }

    private void RefreshGlobalTimeScale()
    {
        // Khi pause HOẶC shop đang mở thì cả local simulation phải dừng.
        Time.timeScale = (isPaused || isShopOpen) ? 0f : 1f;
    }
}