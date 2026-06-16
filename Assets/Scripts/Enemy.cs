using UnityEngine;
using PurrNet;

/// <summary>
/// Trọng tài (Coordinator) cho Quái vật.
/// Lưu trữ cấu hình EnemyDataSO và chia sẻ số liệu (Máu, Tốc độ) cho các Component con.
/// Tự động yêu cầu gắn thêm Movement, Health và Combat.
/// </summary>
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyCombat))]
public class Enemy : NetworkBehaviour
{
    [SerializeField] private EnemyDataSO enemyData;
    
    // Multiplier được Server điều khiển để buff quái sau mỗi Wave
    [SerializeField] private SyncVar<float> syncMultiplier = new SyncVar<float>(1f, ownerAuth: false);
    
    private float enemyMoveSpeed;
    private float maxHp;
    private float enterDamage;
    private float stayDamage;

    private float baseMoveSpeed;
    private float baseMaxHp;
    private float baseEnterDamage;
    private float baseStayDamage;
    private bool statsInitialized = false;

    private EnemySpawner spawner;

    public EnemyDataSO Data => enemyData;
    public float MoveSpeed => enemyMoveSpeed;
    public float MaxHp => maxHp;
    public float EnterDamage => enterDamage;
    public float StayDamage => stayDamage;

    public System.Action<float> OnMultiplierChangedEvent;

    public void Initialize(EnemySpawner spawner)
    {
        this.spawner = spawner;
    }
    
    public void NotifyDied()
    {
        if (spawner != null) spawner.OnEnemyDied();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        syncMultiplier.onChanged += OnMultiplierChanged;
        
        // Client vừa vào game, lập tức đồng bộ chỉ số
        if (!asServer) ApplyStatMultiplierLocal(syncMultiplier.value);
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        syncMultiplier.onChanged -= OnMultiplierChanged;
    }

    private void Awake()
    {
        ApplyDataSO();
    }

    private void ApplyDataSO()
    {
        if (enemyData == null) return;
        enemyMoveSpeed = enemyData.moveSpeed;
        maxHp = enemyData.maxHp;
        enterDamage = enemyData.enterDamage;
        stayDamage = enemyData.stayDamage;
    }

    public void ApplyStatMultiplier(float multiplier)
    {
        if (isSpawned && isServer) syncMultiplier.value = multiplier;
        ApplyStatMultiplierLocal(multiplier);
    }

    private void OnMultiplierChanged(float newMultiplier)
    {
        if (!isServer) ApplyStatMultiplierLocal(newMultiplier);
    }

    private void ApplyStatMultiplierLocal(float multiplier)
    {
        if (!statsInitialized)
        {
            baseMoveSpeed = enemyMoveSpeed;
            baseMaxHp = maxHp;
            baseEnterDamage = enterDamage;
            baseStayDamage = stayDamage;
            statsInitialized = true;
        }

        enemyMoveSpeed = baseMoveSpeed * multiplier;
        maxHp = baseMaxHp * multiplier;
        enterDamage = baseEnterDamage * multiplier;
        stayDamage = baseStayDamage * multiplier;

        OnMultiplierChangedEvent?.Invoke(multiplier);
    }
    
    // ─────────────────── LEGACY INTERFACE ─────────────────────────
    
    public void TakeDamage(float damage, Player attacker = null) 
    { 
        EnemyHealth health = GetComponent<EnemyHealth>();
        if (health != null) health.TakeDamage(damage, attacker); 
    }
}
