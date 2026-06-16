using UnityEngine;
using PurrNet;

/// <summary>
/// Trọng tài (Coordinator) cho GameManager.
/// Cung cấp các biến static (Score, Wave, CountCoin) để không phá vỡ logic cũ.
/// Tự động kết nối với GameStateManager, UIManager, SessionManager.
/// </summary>
[RequireComponent(typeof(GameStateManager))]
[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(SessionManager))]
public class GameManager : NetworkBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    public GameStateManager State { get; private set; }
    public UIManager UI { get; private set; }
    public SessionManager Session { get; private set; }

    public static float BonusSpeed = 0f;
    public static float BonusDamage = 0f;
    public static float BonusMaxHP = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureUnpausedAfterSceneLoad()
    {
        Time.timeScale = 1f;
    }

    private void Awake()
    {
        instance = this;
        State = GetComponent<GameStateManager>();
        UI = GetComponent<UIManager>();
        Session = GetComponent<SessionManager>();
    }
    
    private void Start()
    {
        UI.UpdateCoinText(CountCoin);
        UI.UpdateScoreText(Score);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static int CountCoin
    {
        get => instance != null && instance.State != null ? instance.State.countCoin : 0;
        set
        {
            if (instance != null && instance.State != null)
            {
                instance.State.countCoin = Mathf.Max(0, value);
                instance.UI.UpdateCoinText(instance.State.countCoin);
            }
        }
    }

    public static void UpdateCoin() => CountCoin++;

    public static int Score
    {
        get => instance != null && instance.State != null ? instance.State.score : 0;
        set
        {
            if (instance != null && instance.State != null)
            {
                instance.State.score = Mathf.Max(0, value);
                instance.UI.UpdateScoreText(instance.State.score);
            }
        }
    }

    public static int Wave
    {
        get
        {
            if (instance != null && instance.State != null)
                return instance.isSpawned ? instance.State.syncWave.value : instance.State.wave;
            return 1;
        }
    }

    public static void AdvanceWave()
    {
        if (instance != null && instance.State != null)
        {
            instance.State.AdvanceWave();
            instance.UI.UpdateScoreText(Score); // Force UI update
        }
    }

    public void UpdateCoinTextUI(int coinValue)
    {
        if (UI != null) UI.UpdateCoinText(coinValue);
    }

    public void TogglePause() { if (Session != null) Session.TogglePause(); }
    public void TogglePauseLocal() { if (UI != null) UI.TogglePausePanelLocal(); }
    public void GoMainMenu() { if (Session != null) Session.GoMainMenu(); }
    public void OpenShopForAll() { if (Session != null) Session.OpenShopForAll(); }
    public void CloseShopForAll() { if (Session != null) Session.CloseShopForAll(); }

    public void ResetRunStateLocal()
    {
        BonusSpeed = 0f;
        BonusDamage = 0f;
        BonusMaxHP = 0f;
        if (State != null) State.ResetState();
    }

    public static void ResetRunState()
    {
        if (instance != null) instance.ResetRunStateLocal();
    }

    public void GrantBonusCoinForAll(int amount)
    {
        if (isSpawned && isServer)
        {
            Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach(var p in players) if (p.isSpawned) p.AddCoins(amount);
        }
        else if (!isSpawned)
        {
            CountCoin += amount;
        }
    }

    public void UpgradeSpeed(float amount)
    {
        BonusSpeed += amount;
        Player player = GetLocalPlayer();
        if (player != null) player.AddSpeed(amount);
    }

    public void UpgradeDamage(float amount)
    {
        if (!isSpawned) BonusDamage += amount;
        Player player = GetLocalPlayer();
        if (player != null) player.AddDamage(amount);
    }

    public void UpgradeMaxHP(float amount)
    {
        BonusMaxHP += amount;
        Player player = GetLocalPlayer();
        if (player != null) player.AddMaxHP(amount);
    }

    private Player GetLocalPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsInactive.Exclude);
        foreach (var p in players)
            if (!p.isSpawned || p.isOwner) return p;
        return null;
    }
}