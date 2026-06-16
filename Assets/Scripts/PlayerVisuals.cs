using UnityEngine;
using UnityEngine.UI;
using PurrNet;

/// <summary>
/// Trách nhiệm duy nhất: Điều khiển đồ họa (Hình ảnh, Animation, Thanh máu, Tên hiển thị).
/// "Ngu ngốc" hoàn toàn, chỉ lắng nghe sự kiện từ các component khác.
/// </summary>
public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private Image hpBar;
    private SpriteRenderer spriteRenderer;
    private NetworkAnimator networkAnimator;

    private PlayerHealth health;
    private PlayerStats stats;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        networkAnimator = GetComponent<NetworkAnimator>();
        health = GetComponent<PlayerHealth>();
        stats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHpChangedEvent += UpdateHpBar;
            health.OnDieEvent += HandleDie;
            health.OnReviveEvent += HandleRevive;
        }
        if (stats != null)
        {
            stats.OnMaxHpChanged += UpdateHpBarMax;
            stats.OnPlayerNameChanged += HandleNameChanged;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHpChangedEvent -= UpdateHpBar;
            health.OnDieEvent -= HandleDie;
            health.OnReviveEvent -= HandleRevive;
        }
        if (stats != null)
        {
            stats.OnMaxHpChanged -= UpdateHpBarMax;
            stats.OnPlayerNameChanged -= HandleNameChanged;
        }
    }

    private void Start()
    {
        UpdateHpBar(health != null ? health.CurrentHp : 100f);
        
        Player p = GetComponent<Player>();
        if (p != null && p.isSpawned && p.isOwner) AssignCinemachineCamera();
        else if (p != null && !p.isSpawned) AssignCinemachineCamera(); // Offline fallback
    }

    private void Update()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        }
    }

    public void UpdateMovementAnimation(Vector2 input)
    {
        if (input.x < 0) spriteRenderer.flipX = true;
        else if (input.x > 0) spriteRenderer.flipX = false;

        if (networkAnimator != null)
            networkAnimator.SetBool("isRun", input != Vector2.zero);
    }

    private void UpdateHpBar(float currentHp)
    {
        if (hpBar != null && stats != null)
        {
            float max = stats.MaxHp;
            if (max > 0f) hpBar.fillAmount = currentHp / max;
        }
    }

    private void UpdateHpBarMax(float maxHp)
    {
        if (health != null) UpdateHpBar(health.CurrentHp);
    }

    private void HandleNameChanged(string newName)
    {
        PlayerNameDisplay display = GetComponentInChildren<PlayerNameDisplay>();
        if (display != null) display.SetName(newName);
    }

    private void HandleDie()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        var cols = GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = false;

        if (hpBar != null && hpBar.transform.parent != null)
            hpBar.transform.parent.gameObject.SetActive(false);

        Gun gun = GetComponentInChildren<Gun>(true);
        if (gun != null) gun.gameObject.SetActive(false);
    }

    private void HandleRevive()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        var cols = GetComponents<Collider2D>();
        foreach (var c in cols) c.enabled = true;

        if (hpBar != null && hpBar.transform.parent != null)
            hpBar.transform.parent.gameObject.SetActive(true);

        Gun gun = GetComponentInChildren<Gun>(true);
        if (gun != null) gun.gameObject.SetActive(true);
    }

    private void AssignCinemachineCamera()
    {
        Component[] allComponents = FindObjectsByType<Component>(FindObjectsSortMode.None);
        foreach (var comp in allComponents)
        {
            string compName = comp.GetType().Name;
            if (compName == "CinemachineVirtualCamera" || compName == "CinemachineCamera")
            {
                var followProp = comp.GetType().GetProperty("Follow");
                if (followProp != null) followProp.SetValue(comp, this.transform);
                
                var lookAtProp = comp.GetType().GetProperty("LookAt");
                if (lookAtProp != null) lookAtProp.SetValue(comp, this.transform);
                break;
            }
        }
    }

    public void ForceHideAll()
    {
        HandleDie();
    }
}
