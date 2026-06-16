using UnityEngine;
using PurrNet;

/// <summary>
/// Trách nhiệm duy nhất: Quản lý lượng máu hiện tại (currentHp) và logic Sống/Chết.
/// Server toàn quyền quyết định việc trừ máu và phát thông báo chết.
/// </summary>
public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private SyncVar<float> currentHp = new SyncVar<float>(100f, ownerAuth: false);
    private bool _isDead;
    public bool IsDead => _isDead;

    private PlayerStats stats;
    
    // Gửi tín hiệu để PlayerVisuals cập nhật UI
    public System.Action<float> OnHpChangedEvent;
    public System.Action OnDieEvent;
    public System.Action OnReviveEvent;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        currentHp.onChanged += HandleHpChanged;
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        currentHp.onChanged -= HandleHpChanged;
    }

    private void HandleHpChanged(float val)
    {
        OnHpChangedEvent?.Invoke(val);
    }

    public float CurrentHp => isSpawned ? currentHp.value : (stats != null ? stats.MaxHp : 100f);

    public void ResetHealthOnServer()
    {
        _isDead = false;
        if (stats != null)
        {
            currentHp.value = stats.MaxHp;
        }
        else
        {
            currentHp.value = 100f;
        }
    }

    public void TakeDamage(float damage)
    {
        // Chỉ Server xử lý sát thương
        if (isSpawned && !isServer) return;

        float max = stats != null ? stats.MaxHp : 100f;
        float newHp = Mathf.Clamp(currentHp.value - damage, 0f, max);
        currentHp.value = newHp;

        if (newHp <= 0f && !_isDead)
        {
            _isDead = true;
            RpcDie();
        }
    }

    public void Heal(float healAmount)
    {
        // Chỉ Server xử lý hồi máu
        if (isSpawned && !isServer) return;
        
        float max = stats != null ? stats.MaxHp : 100f;
        currentHp.value = Mathf.Clamp(currentHp.value + healAmount, 0f, max);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcDie()
    {
        _isDead = true;
        OnDieEvent?.Invoke(); // Báo cho Visuals ẩn hình ảnh

        if (isSpawned && !isOwner) return;

        // Kêu Spectate hoặc Load cảnh GameOver
        SpectateManager spectateManager = FindAnyObjectByType<SpectateManager>();
        if (spectateManager != null)
        {
            spectateManager.StartSpectating(GetComponent<Player>());
        }
        else
        {
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

    [ObserversRpc(runLocally: true)]
    public void RpcResetVisuals()
    {
        _isDead = false;
        OnReviveEvent?.Invoke();
    }
}
