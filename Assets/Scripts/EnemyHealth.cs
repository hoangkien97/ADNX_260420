using UnityEngine;
using UnityEngine.UI;
using PurrNet;

/// <summary>
/// Trách nhiệm duy nhất: Quản lý Máu, Giao diện thanh máu và Logic Chết/Rớt đồ.
/// </summary>
public class EnemyHealth : NetworkBehaviour
{
    private Enemy coordinator;
    [SerializeField] private Image hpBar;
    private Canvas hpBarCanvas;

    [SerializeField] private SyncVar<float> currentHp = new SyncVar<float>(100f, ownerAuth: false);
    private Player lastKiller;

    private void Awake()
    {
        coordinator = GetComponent<Enemy>();
        if (hpBar != null) hpBarCanvas = hpBar.GetComponentInParent<Canvas>();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        currentHp.onChanged += OnHpChanged;
        
        if (asServer && coordinator != null)
        {
            currentHp.value = coordinator.MaxHp;
        }
        UpdateHpBar();
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        currentHp.onChanged -= OnHpChanged;
    }

    private void OnEnable()
    {
        if (coordinator != null) coordinator.OnMultiplierChangedEvent += HandleMultiplierChanged;
    }

    private void OnDisable()
    {
        if (coordinator != null) coordinator.OnMultiplierChangedEvent -= HandleMultiplierChanged;
    }

    private void HandleMultiplierChanged(float multiplier)
    {
        if (isSpawned && isServer && coordinator != null)
            currentHp.value = coordinator.MaxHp;
        UpdateHpBar();
    }

    private void Start()
    {
        if (!isSpawned && coordinator != null)
        {
            currentHp.value = coordinator.MaxHp;
        }
        UpdateHpBar();
    }

    private void Update()
    {
        // Fix góc nghiêng của UI Canvas
        if (hpBarCanvas != null)
        {
            hpBarCanvas.overrideSorting = true;
            int hpOrder = Mathf.RoundToInt(-transform.position.z * 1000000f);
            hpBarCanvas.sortingOrder = hpOrder + 1;
        }
        if (hpBar != null) hpBar.transform.rotation = Quaternion.identity;
    }

    public void TakeDamage(float damage, Player attacker = null)
    {
        if (isSpawned && !isServer) return;
        if (attacker != null) lastKiller = attacker;

        float max = coordinator != null ? coordinator.MaxHp : 100f;
        float newHp = Mathf.Clamp(currentHp.value - damage, 0f, max);
        currentHp.value = newHp;

        if (newHp <= 0f) Die();
    }

    private void Die()
    {
        if (lastKiller != null) lastKiller.AddKillScore();

        if (isServer || !isSpawned)
            SpawnLoot(transform.position);

        if (coordinator != null) coordinator.NotifyDied();
        
        if (isSpawned) Despawn();
        else Destroy(gameObject);
    }

    private void SpawnLoot(Vector3 position)
    {
        if (coordinator == null || coordinator.Data == null) return;
        GameObject prefab = coordinator.Data.dropPrefab;
        if (prefab == null) return;

        GameObject dropItem = Instantiate(prefab, position, Quaternion.identity);
        ItemDespawner despawner = dropItem.AddComponent<ItemDespawner>();
        despawner.StartDespawn(coordinator.Data.dropLifetime);
    }

    private void OnHpChanged(float newHp) => UpdateHpBar();

    private void UpdateHpBar()
    {
        float max = coordinator != null ? coordinator.MaxHp : 100f;
        if (hpBar != null && max > 0f)
            hpBar.fillAmount = currentHp.value / max;
    }
}
