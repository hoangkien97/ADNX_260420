using UnityEngine;
using PurrNet;

/// <summary>
/// Trọng tài (Coordinator) điều phối các Component của người chơi.
/// Tự động yêu cầu Unity thêm 4 component con vào.
/// Cung cấp các hàm công khai (Legacy Interface) để không làm gãy các logic khác.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerVisuals))]
public class Player : NetworkBehaviour
{
    private PlayerStats stats;
    private PlayerHealth health;
    private PlayerVisuals visuals;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
        visuals = GetComponent<PlayerVisuals>();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (isOwner)
        {
            CmdRequestResetState();
        }
        if (asServer)
        {
            networkManager.onPlayerLeft += OnOwnerLeft;
        }
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        if (asServer)
        {
            networkManager.onPlayerLeft -= OnOwnerLeft;
        }
    }

    private void OnOwnerLeft(PlayerID leftPlayer, bool asServer)
    {
        if (!asServer) return;
        if (!owner.HasValue || owner.Value != leftPlayer) return;
        RpcHideForLeaving();
    }

    [ServerRpc(requireOwnership: true)]
    private void CmdRequestResetState()
    {
        if (stats != null) stats.ResetRunStateOnServer();
        if (health != null) health.ResetHealthOnServer();
        if (health != null) health.RpcResetVisuals();
    }

    // ─────────────────── CHAT SYSTEM ─────────────────────────

    [ServerRpc(requireOwnership: true)]
    public void CmdSendChat(string message)
    {
        string name = stats != null ? stats.PlayerDisplayName : "Player";
        RpcReceiveChat(name, message);
    }

    [ObserversRpc(runLocally: true)]
    public void RpcReceiveChat(string senderName, string message)
    {
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.AddMessage(senderName, message);
        }
    }

    // ─────────────────── GLOBAL LEAVE HANDLING ───────────────

    [ServerRpc(requireOwnership: true)]
    public void CmdNotifyLeaving()
    {
        RpcHideForLeaving();
    }

    [ObserversRpc(runLocally: true)]
    private void RpcHideForLeaving()
    {
        if (visuals != null) visuals.ForceHideAll();
    }

    public static void NotifyAllPlayersLeaving()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (Player p in players)
        {
            if (p != null && p.isSpawned && p.isOwner)
            {
                p.CmdNotifyLeaving();
            }
        }
    }

    // ─────────────────── LEGACY INTERFACE (Giữ nguyên cho các script cũ) ─────────────────────────

    public float MaxHp => stats != null ? stats.MaxHp : 100f;
    public float MoveSpeed => stats != null ? stats.MoveSpeed : 5f;
    public int MyCoins => stats != null ? stats.MyCoins : 0;
    public bool IsDead => health != null && health.IsDead;
    public string PlayerDisplayName => stats != null ? stats.PlayerDisplayName : "Player";
    
    public void TakeDamage(float damage) { if (health != null) health.TakeDamage(damage); }
    public void Heal(float amount) { if (health != null) health.Heal(amount); }
    
    public void AddCoins(int amount) { if (stats != null) stats.AddCoins(amount); }
    
    [ServerRpc(requireOwnership: true)]
    public void CmdBuyShopItem(int itemType, int cost, float effectValue) 
    { 
        if (stats != null) stats.ProcessBuyShopItem(itemType, cost, effectValue); 
    }
    
    public void AddKillScore(int amount = 1)
    {
        if (isSpawned) RpcGrantKillScore(amount);
        else GameManager.Score += amount;
    }

    [ObserversRpc(runLocally: true)]
    private void RpcGrantKillScore(int amount)
    {
        if (!isOwner) return;
        GameManager.Score += amount;
    }

    public void AddMaxHP(float amount) 
    { 
        if (stats != null) stats.AddMaxHP_Offline(amount); 
        if (health != null) health.Heal(amount); 
    }
    public void AddSpeed(float amount) { if (stats != null) stats.AddSpeed_Offline(amount); }
    public void AddDamage(float amount) { } // Empty because handled by BonusDamage in GameConfig/GameManager
    
    public float GetBonusDamage() { return stats != null ? stats.GetBonusDamage() : 0f; }
}
