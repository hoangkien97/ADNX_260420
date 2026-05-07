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
    private Animator animator;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private Image hpBar;
    private GameManager gameManager;

    // SyncVar: Server ghi, tất cả clients đọc (ownerAuth: false = chỉ server mới ghi)
    [SerializeField] private SyncVar<float> currentHp = new SyncVar<float>(100f, ownerAuth: false);

    // SyncVar: Owner ghi tên player (ownerAuth: true = owner có quyền ghi)
    [SerializeField] private SyncVar<string> playerName = new SyncVar<string>("", ownerAuth: true);

    private bool _isDead;

    public float MaxHp => maxHp;
    public float MoveSpeed => speed;
    public bool IsDead => _isDead;
    public string PlayerDisplayName => isSpawned ? playerName.value : "Player";

    // ─────────────────── NETWORK LIFECYCLE ───────────────────

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        // Subscribe SyncVar callback để update HP bar khi nhận giá trị mới
        currentHp.onChanged += OnHpChanged;
        playerName.onChanged += OnPlayerNameChanged;

        if (asServer)
        {
            // Server: khởi tạo HP với bonus
            maxHp += GameManager.BonusMaxHP;
            currentHp.value = maxHp;
        }

        if (isOwner)
        {
            // Owner: Lấy tên trực tiếp từ ApiManager
            string name = ApiManager.IsLoggedIn ? ApiManager.CurrentUsername : "Guest_" + Random.Range(1000, 9999);
            
            playerName.value = name;

            // Owner: áp bonus speed
            speed += GameManager.BonusSpeed;
            
            // Tìm và gắn Cinemachine vào player này
            AssignCinemachineCamera();
        }

        gameManager = FindAnyObjectByType<GameManager>();
        UpdateHpBar();
        
        // Khởi tạo UI hiển thị tên bằng giá trị hiện tại của SyncVar
        // (Rất quan trọng cho client join sau, hoặc chủ phòng tự cập nhật UI)
        OnPlayerNameChanged(playerName.value);
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        currentHp.onChanged -= OnHpChanged;
        playerName.onChanged -= OnPlayerNameChanged;
    }

    // ─────────────────── UNITY LIFECYCLE ─────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Fallback: chạy khi không có network (single-player / offline mode)
        if (!isSpawned)
        {
            speed += GameManager.BonusSpeed;
            maxHp += GameManager.BonusMaxHP;
            currentHp.value = maxHp;
            gameManager = FindAnyObjectByType<GameManager>();
            UpdateHpBar();
        }
    }

    private void Update()
    {
        // Chỉ local player (IsOwner) mới xử lý input
        if (isSpawned && !isOwner) return;

        if (Time.timeScale == 0f) return; // Không di chuyển khi pause

        MovePlayer();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameManager == null)
                gameManager = FindAnyObjectByType<GameManager>();
            gameManager?.TogglePause();
        }
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
            rb.linearVelocity = input.normalized * speed;
        }

        if (input.x < 0)       spriteRenderer.flipX = true;
        else if (input.x > 0)  spriteRenderer.flipX = false;

        animator.SetBool("isRun", input != Vector2.zero);
    }

    // ─────────────────── HEALTH ──────────────────────────────

    /// <summary>
    /// Gây damage cho player. Chỉ Server xử lý.
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Nếu đang networked, chỉ server mới được xử lý
        if (isSpawned && !isServer) return;

        float newHp = Mathf.Clamp(currentHp.value - damage, 0f, maxHp);
        currentHp.value = newHp;

        if (newHp <= 0f && !_isDead)
        {
            _isDead = true;
            RpcDie();
        }
    }

    /// <summary>
    /// Broadcast tới tất cả clients: player này đã chết → spectate hoặc GameOver.
    /// </summary>
    [ObserversRpc(runLocally: true)]
    private void RpcDie()
    {
        _isDead = true;
        gameObject.SetActive(false);

        // Chỉ local player mới cần chuyển sang spectate / GameOver
        if (isSpawned && !isOwner) return;

        SpectateManager spectateManager = FindAnyObjectByType<SpectateManager>();
        if (spectateManager != null)
        {
            spectateManager.StartSpectating(this);
        }
        else
        {
            // Fallback single-player: load GameOver thẳng
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameOver");
        }
    }

    public void Heal(float healAmount)
    {
        if (isSpawned && !isServer) return;
        currentHp.value = Mathf.Clamp(currentHp.value + healAmount, 0f, maxHp);
    }

    public void AddMaxHP(float amount)
    {
        if (isSpawned && !isServer) return;
        maxHp += amount;
        currentHp.value = Mathf.Clamp(currentHp.value + amount, 0f, maxHp);
    }

    public void AddSpeed(float amount)
    {
        // Chỉ áp dụng local (shop riêng từng người)
        speed += amount;
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
        if (hpBar != null && maxHp > 0f)
            hpBar.fillAmount = currentHp.value / maxHp;
    }
}
