using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PurrNet;

/// <summary>
/// Player controller với PurrNet multiplayer support.
/// - isOwner: chỉ local player xử lý input và di chuyển
/// - isServer: Host xử lý TakeDamage và Die logic
/// - currentHp sync qua SyncVar (Server ghi, tất cả clients đọc)
/// - Khi chết: chuyển sang Spectate thay vì load GameOver ngay
/// </summary>
public class Player : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private NetworkAnimator networkAnimator;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private Image hpBar;
    private GameManager gameManager;

    // Lấy config từ singleton thay vì kéo thả vào từng prefab
    private static GameConfigSO GameConfig => EnemyDataManager.Instance?.gameConfig;

    // Các chỉ số được Server quản lý tuyệt đối (ownerAuth: false)
    [SerializeField] private SyncVar<int> myCoins = new SyncVar<int>(0, ownerAuth: false);
    [SerializeField] private SyncVar<float> currentSpeed = new SyncVar<float>(5f, ownerAuth: false);
    [SerializeField] private SyncVar<float> currentMaxHp = new SyncVar<float>(100f, ownerAuth: false);
    [SerializeField] private SyncVar<float> currentDamage = new SyncVar<float>(50f, ownerAuth: false);

    // SyncVar: Server ghi, tất cả clients đọc (ownerAuth: false = chỉ server mới ghi)
    [SerializeField] private SyncVar<float> currentHp = new SyncVar<float>(100f, ownerAuth: false);

    // SyncVar: Owner ghi tên player (ownerAuth: true = owner có quyền ghi)
    [SerializeField] private SyncVar<string> playerName = new SyncVar<string>("", ownerAuth: true);

    private bool _isDead;
    private float baseSpeed;
    private float baseMaxHp;
    private float baseDamage;

    public float MaxHp => isSpawned ? currentMaxHp.value : maxHp;
    public float MoveSpeed => isSpawned ? currentSpeed.value : speed;
    public int MyCoins => isSpawned ? myCoins.value : GameManager.CountCoin;
    public bool IsDead => _isDead;
    public string PlayerDisplayName => isSpawned ? playerName.value : "Player";

    // ─────────────────── NETWORK LIFECYCLE ───────────────────

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (isOwner)
        {
            // Yêu cầu Server khôi phục toàn bộ trạng thái (HP, hình dáng, vũ khí)
            CmdRequestResetState();
        }

        if (asServer)
        {
            // Server: Theo dõi sự kiện player thoát – tự động ẩn player ngay lập tức khi owner disconnect
            networkManager.onPlayerLeft += OnOwnerLeft;
        }

        // Subscribe SyncVar callback để update UI
        currentHp.onChanged += OnHpChanged;
        currentMaxHp.onChanged += OnHpChanged; // Cập nhật lại UI khi maxHp đổi
        playerName.onChanged += OnPlayerNameChanged;

        if (isOwner)
        {
            // Owner: Lấy tên trực tiếp từ ApiManager
            string name = ApiManager.IsLoggedIn ? ApiManager.CurrentUsername : "Guest_" + Random.Range(1000, 9999);
            
            playerName.value = name;
            
            // Tìm và gắn Cinemachine vào player này
            AssignCinemachineCamera();
        }

        gameManager = FindAnyObjectByType<GameManager>();
        UpdateHpBar();
        
        // Khởi tạo UI hiển thị tên bằng giá trị hiện tại của SyncVar
        OnPlayerNameChanged(playerName.value);
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        currentHp.onChanged -= OnHpChanged;
        currentMaxHp.onChanged -= OnHpChanged;
        playerName.onChanged -= OnPlayerNameChanged;

        if (asServer)
        {
            networkManager.onPlayerLeft -= OnOwnerLeft;
        }
    }

    private void OnOwnerLeft(PlayerID leftPlayer, bool asServer)
    {
        // Chỉ chạy trên Server và chỉ khi đúng player của object này thoát
        if (!asServer) return;
        if (!owner.HasValue || owner.Value != leftPlayer) return;

        // Ẩn player ngay lập tức mà không cần chờ RPC
        RpcHideForLeaving();
    }

    // ─────────────────── SCORE (PER-PLAYER) ─────────────────

    /// <summary>
    /// Server gọi hàm này khi quái bị người chơi này giết.
    /// Chỉ Owner (người chơi sở hữu nhân vật này) mới cộng điểm.
    /// </summary>
    public void AddKillScore(int amount = 1)
    {
        if (isSpawned)
        {
            RpcGrantKillScore(amount);
        }
        else
        {
            // Offline mode
            GameManager.Score += amount;
        }
    }

    [ObserversRpc(runLocally: true)]
    private void RpcGrantKillScore(int amount)
    {
        // Chỉ Owner của nhân vật này mới được cộng điểm
        if (!isOwner) return;
        GameManager.Score += amount;
    }

    // ─────────────────── UNITY LIFECYCLE ─────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        networkAnimator = GetComponent<NetworkAnimator>();

        ApplyGameConfig();

        baseSpeed = speed;
        baseMaxHp = maxHp;
        
        // Cố gắng đọc base damage từ GunConfig nếu có (offline fallback)
        GameConfigSO cfg = GameConfig;
        baseDamage = cfg != null ? cfg.playerDamage : 50f;
    }

    private void ApplyGameConfig()
    {
        GameConfigSO cfg = GameConfig;
        if (cfg != null)
        {
            speed  = cfg.playerSpeed;
            maxHp  = cfg.playerMaxHp;
        }
    }

    private void Start()
    {
        // Fallback: chạy khi không có network (single-player / offline mode)
        if (!isSpawned)
        {
            gameManager = FindAnyObjectByType<GameManager>();
            UpdateHpBar();
        }
    }

    private void Update()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }

        if (isOwner && Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameManager == null)
                gameManager = FindAnyObjectByType<GameManager>();

            if (_isDead && isSpawned && !isServer)
            {
                // Client đã chết: hiện/ẩn pause panel local để Quit
                gameManager?.TogglePauseLocal();
            }
            else if (!isSpawned || isServer)
            {
                // Offline hoặc là Server/Host: pause toàn phòng
                gameManager?.TogglePause();
            }
            else
            {
                // Client còn sống: chỉ hiện/ẩn pause panel local để Quit
                // Không gửi RPC, không ảnh hưởng Server hay người chơi khác
                gameManager?.TogglePauseLocal();
            }
        }

        // Ngưng mọi tương tác di chuyển nếu đã chết
        if (_isDead) return;

        // Chỉ local player (IsOwner) đang sống mới xử lý input
        if (isSpawned && !isOwner) return;

        if (Time.timeScale == 0f) return; // Không di chuyển khi pause

        MovePlayer();
    }

    private void AssignCinemachineCamera()
    {
        // Hỗ trợ cả Cinemachine v2 và v3
        var cinemachineBrain = Camera.main != null ? Camera.main.GetComponent<Component>() : null; 
        
        // Tìm GameObject chứa Cinemachine Virtual Camera
        Component[] allComponents = FindObjectsByType<Component>(FindObjectsSortMode.None);
        foreach (var comp in allComponents)
        {
            string compName = comp.GetType().Name;
            if (compName == "CinemachineVirtualCamera" || compName == "CinemachineCamera")
            {
                // Cố gắng gán Follow
                var followProp = comp.GetType().GetProperty("Follow");
                if (followProp != null) followProp.SetValue(comp, this.transform);
                
                var lookAtProp = comp.GetType().GetProperty("LookAt");
                if (lookAtProp != null) lookAtProp.SetValue(comp, this.transform);
                
                Debug.Log($"[Player] Đã gán Camera follow vào {compName}");
                break;
            }
        }
    }

    // ─────────────────── MOVEMENT ────────────────────────────

    private void MovePlayer()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (rb.bodyType == RigidbodyType2D.Static)
            {
                Debug.LogWarning("[Player] Rigidbody2D đang bị Static! Không thể di chuyển.");
                return;
            }
            float currentMoveSpeed = isSpawned ? currentSpeed.value : speed;
            rb.linearVelocity = input.normalized * currentMoveSpeed;
        }

        if (input.x < 0)       spriteRenderer.flipX = true;
        else if (input.x > 0)  spriteRenderer.flipX = false;

        if (networkAnimator != null)
            networkAnimator.SetBool("isRun", input != Vector2.zero);
    }

    // ─────────────────── HEALTH ──────────────────────────────

    /// <summary>
    /// Gây damage cho player. Chỉ Server xử lý.
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Nếu đang networked, chỉ server mới được xử lý
        if (isSpawned && !isServer) return;

        float max = isSpawned ? currentMaxHp.value : maxHp;
        float newHp = Mathf.Clamp(currentHp.value - damage, 0f, max);
        currentHp.value = newHp;

        if (newHp <= 0f && !_isDead)
        {
            _isDead = true;
            RpcDie();
        }
    }

    // ─────────────────── RESPAWN / RECONNECT ─────────────────

    [ServerRpc(requireOwnership: true)]
    private void CmdRequestResetState()
    {
        _isDead = false;
        
        GameConfigSO cfg = GameConfig;
        
        // Khởi tạo các chỉ số trên Server
        currentMaxHp.value = cfg != null ? cfg.playerMaxHp : baseMaxHp;
        currentHp.value = currentMaxHp.value;
        currentSpeed.value = cfg != null ? cfg.playerSpeed : baseSpeed;
        currentDamage.value = cfg != null ? cfg.playerDamage : baseDamage;
        myCoins.value = 0; // Reset ví tiền khi bắt đầu run mới
        
        RpcResetVisuals();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcResetVisuals()
    {
        _isDead = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in sprites) s.enabled = true;

        var cols = GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = true;

        if (hpBar != null && hpBar.transform.parent != null)
            hpBar.transform.parent.gameObject.SetActive(true);

        Gun gun = GetComponentInChildren<Gun>(true);
        if (gun != null) gun.gameObject.SetActive(true);
    }

    // ─────────────────── CHAT SYSTEM ─────────────────────────

    [ServerRpc(requireOwnership: true)]
    public void CmdSendChat(string message)
    {
        Debug.Log($"[Server] Received CmdSendChat from {PlayerDisplayName}. Message: {message}");
        RpcReceiveChat(PlayerDisplayName, message);
    }

    [ObserversRpc(runLocally: true)]
    public void RpcReceiveChat(string senderName, string message)
    {
        Debug.Log($"[Client/Observer] Received RpcReceiveChat from {senderName}. Message: {message}");
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.AddMessage(senderName, message);
        }
    }

    /// <summary>
    /// Gọi trước khi Client thoát – bảo Server despawn player này sạch sẽ.
    /// </summary>
    [ServerRpc(requireOwnership: true)]
    public void CmdNotifyLeaving()
    {
        // Broadcast ẩn player trên tất cả client (bao gồm server)
        // Không dùng Destroy() vì PurrNet Object Pooling sẽ chặn lại
        RpcHideForLeaving();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcHideForLeaving()
    {
        _isDead = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Ẩn toàn bộ hình ảnh
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in sprites) s.enabled = false;

        // Tắt va chạm
        var cols = GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = false;

        // Ẩn thanh máu
        if (hpBar != null && hpBar.transform.parent != null)
            hpBar.transform.parent.gameObject.SetActive(false);

        // Ẩn súng
        Gun gun = GetComponentInChildren<Gun>(true);
        if (gun != null) gun.gameObject.SetActive(false);
    }

    public static void NotifyAllPlayersLeaving()
    {
        Player[] players = FindObjectsOfType<Player>();
        foreach (Player p in players)
        {
            if (p != null && p.isSpawned && p.isOwner)
            {
                p.CmdNotifyLeaving();
            }
        }
    }

    // ─────────────────── DEATH ───────────────────────────────

    /// <summary>
    /// Broadcast tới tất cả clients: player này đã chết → spectate hoặc GameOver.
    /// </summary>
    [ObserversRpc(runLocally: true)]
    private void RpcDie()
    {
        _isDead = true;

        if (rb != null) rb.linearVelocity = Vector2.zero; // Ngừng trôi
        
        // Ẩn hình ảnh và tắt va chạm thay vì tắt cả GameObject để Update() vẫn chạy bắt phím Pause
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        var cols = GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = false;
        if (hpBar != null && hpBar.transform.parent != null)
            hpBar.transform.parent.gameObject.SetActive(false);

        // Ẩn luôn súng
        Gun gun = GetComponentInChildren<Gun>();
        if (gun != null) gun.gameObject.SetActive(false);

        // Chỉ local player mới cần chuyển sang spectate / GameOver
        if (isSpawned && !isOwner) return;

        SpectateManager spectateManager = FindObjectOfType<SpectateManager>(true);
        if (spectateManager != null)
        {
            spectateManager.StartSpectating(this);
        }
        else
        {
            // Thay vì LoadScene trực tiếp, ủy quyền cho Server
            Time.timeScale = 1f;
            if (PurrNet.NetworkManager.main != null)
            {
                if (PurrNet.NetworkManager.main.isServer)
                    PurrNet.NetworkManager.main.sceneModule.LoadSceneAsync("GameOver", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
            }
        }
    }

    public void AddCoins(int amount)
    {
        if (isSpawned && isServer)
            myCoins.value += amount;
        else if (!isSpawned)
            GameManager.CountCoin += amount;
    }

    [ServerRpc(requireOwnership: true)]
    public void CmdBuyShopItem(int itemType, int cost, float effectValue)
    {
        // 1. Kiểm tra ví tiền trên Server
        if (myCoins.value >= cost)
        {
            // 2. Trừ tiền
            myCoins.value -= cost;
            
            // 3. Tăng chỉ số an toàn trên Server
            // itemType: 0 = Speed, 1 = Damage, 2 = MaxHP (Tương ứng với ShopItemType enum)
            if (itemType == 0)
                currentSpeed.value += effectValue;
            else if (itemType == 1)
                currentDamage.value += effectValue;
            else if (itemType == 2)
            {
                currentMaxHp.value += effectValue;
                currentHp.value += effectValue; // Thêm máu hiện tại luôn
            }
        }
    }

    public void Heal(float healAmount)
    {
        if (isSpawned && !isServer) return;
        float max = isSpawned ? currentMaxHp.value : maxHp;
        currentHp.value = Mathf.Clamp(currentHp.value + healAmount, 0f, max);
    }

    // ─────────────────── OFFLINE FALLBACK STATS ───────────────────
    public void AddMaxHP(float amount)
    {
        if (isSpawned) return;
        maxHp += amount;
        currentHp.value = Mathf.Clamp(currentHp.value + amount, 0f, maxHp);
    }

    public void AddSpeed(float amount)
    {
        if (isSpawned) return;
        speed += amount;
    }

    public void AddDamage(float amount)
    {
        // Thực tế Gun.cs tự cộng GameManager.BonusDamage nếu offline, nhưng ta vẫn giữ để tránh lỗi compile
    }

    public float GetBonusDamage() 
    {
        // currentDamage là tổng sát thương gốc + bonus, nên nếu súng chỉ cần tổng thì trả về currentDamage
        // Hoặc trả về phần chênh lệch. Nếu offline, trả về biến tĩnh của GameManager.
        return isSpawned ? (currentDamage.value - baseDamage) : GameManager.BonusDamage; 
    }

    // ─────────────────── CALLBACKS ───────────────────────────

    private void OnHpChanged(float newHp)
    {
        UpdateHpBar();
    }

    private void OnPlayerNameChanged(string newName)
    {
        PlayerNameDisplay display = GetComponentInChildren<PlayerNameDisplay>();
        if (display != null)
        {
            display.SetName(newName);
        }
    }

    private void UpdateHpBar()
    {
        if (hpBar != null)
        {
            float max = isSpawned ? currentMaxHp.value : maxHp;
            if (max > 0f)
                hpBar.fillAmount = currentHp.value / max;
        }
    }
}
