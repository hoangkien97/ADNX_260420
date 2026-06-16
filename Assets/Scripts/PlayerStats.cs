using UnityEngine;
using PurrNet;

/// <summary>
/// Trái tim dữ liệu mạng: Chỉ lưu trữ các SyncVar về Chỉ số (Máu tối đa, Tốc độ, Sát thương, Tiền).
/// Server nắm quyền tuyệt đối (ownerAuth = false).
/// </summary>
public class PlayerStats : NetworkBehaviour
{
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float baseMaxHp = 100f;
    private float baseDamage = 50f;

    [SerializeField] private SyncVar<int> myCoins = new SyncVar<int>(0, ownerAuth: false);
    [SerializeField] private SyncVar<float> currentSpeed = new SyncVar<float>(5f, ownerAuth: false);
    [SerializeField] private SyncVar<float> currentMaxHp = new SyncVar<float>(100f, ownerAuth: false);
    [SerializeField] private SyncVar<float> currentDamage = new SyncVar<float>(50f, ownerAuth: false);
    [SerializeField] private SyncVar<string> playerName = new SyncVar<string>("", ownerAuth: true);

    public System.Action<float> OnMaxHpChanged;
    public System.Action<int> OnCoinsChanged;
    public System.Action<string> OnPlayerNameChanged;

    public float MoveSpeed => isSpawned ? currentSpeed.value : baseSpeed;
    public float MaxHp => isSpawned ? currentMaxHp.value : baseMaxHp;
    public int MyCoins => isSpawned ? myCoins.value : GameManager.CountCoin;
    public string PlayerDisplayName => isSpawned ? playerName.value : "Player";

    private static GameConfigSO GameConfig => EnemyDataManager.Instance?.gameConfig;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        
        currentMaxHp.onChanged += HandleMaxHpChanged;
        myCoins.onChanged += HandleCoinsChanged;
        playerName.onChanged += HandlePlayerNameChanged;

        if (isOwner)
        {
            IAuthService auth = ServiceLocator.Get<IAuthService>();
            playerName.value = auth != null && auth.IsLoggedIn ? auth.CurrentUsername : "Guest_" + Random.Range(1000, 9999);
            HandleCoinsChanged(myCoins.value);
        }
        HandlePlayerNameChanged(playerName.value);
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        currentMaxHp.onChanged -= HandleMaxHpChanged;
        myCoins.onChanged -= HandleCoinsChanged;
        playerName.onChanged -= HandlePlayerNameChanged;
    }

    private void Awake()
    {
        ApplyGameConfig();
    }

    public void ApplyGameConfig()
    {
        GameConfigSO cfg = GameConfig;
        if (cfg != null)
        {
            baseSpeed = cfg.playerSpeed;
            baseMaxHp = cfg.playerMaxHp;
            baseDamage = cfg.playerDamage;
        }
        else
        {
            baseDamage = 50f;
        }
    }

    public void ResetRunStateOnServer()
    {
        GameConfigSO cfg = GameConfig;
        currentMaxHp.value = cfg != null ? cfg.playerMaxHp : baseMaxHp;
        currentSpeed.value = cfg != null ? cfg.playerSpeed : baseSpeed;
        currentDamage.value = cfg != null ? cfg.playerDamage : baseDamage;
        myCoins.value = 0;
    }

    private void HandleMaxHpChanged(float val) => OnMaxHpChanged?.Invoke(val);
    private void HandleCoinsChanged(int val)
    {
        OnCoinsChanged?.Invoke(val);
        if (isOwner && GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCoinTextUI(val);
        }
    }
    private void HandlePlayerNameChanged(string val) => OnPlayerNameChanged?.Invoke(val);

    // Được gọi từ Player Coordinator (ServerRpc)
    public void ProcessBuyShopItem(int itemType, int cost, float effectValue)
    {
        if (myCoins.value >= cost)
        {
            myCoins.value -= cost;
            if (itemType == 0) currentSpeed.value += effectValue;
            else if (itemType == 1) currentDamage.value += effectValue;
            else if (itemType == 2)
            {
                currentMaxHp.value += effectValue;
                PlayerHealth ph = GetComponent<PlayerHealth>();
                if (ph != null) ph.Heal(effectValue);
            }
        }
    }

    public void AddCoins(int amount)
    {
        if (isSpawned && isServer) myCoins.value += amount;
        else if (!isSpawned) GameManager.CountCoin += amount;
    }

    // Fallback Offline functions
    public void AddMaxHP_Offline(float amount) { baseMaxHp += amount; }
    public void AddSpeed_Offline(float amount) { baseSpeed += amount; }
    public float GetBonusDamage() 
    {
        return isSpawned ? (currentDamage.value - baseDamage) : GameManager.BonusDamage; 
    }
}
