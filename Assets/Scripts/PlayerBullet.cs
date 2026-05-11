using UnityEngine;
using PurrNet;

/// <summary>
/// PlayerBullet với PurrNet multiplayer support.
/// - Được Server spawn (qua Gun.CmdShoot)
/// - Di chuyển chạy trên tất cả clients (visual)
/// - OnTriggerEnter2D chỉ xử lý damage trên Server
/// - Destroy → Despawn() trên network
/// </summary>
public class PlayerBullet : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float timeDestroy = 1f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private GameObject bloodPrefab;

    // Người bắn viên đạn này (dùng để cộng điểm đúng người)
    private Player shooter;

    public float BaseDamage => damage;

    public void SetShooter(Player player) => shooter = player;

    /// <summary>
    /// Áp dụng bonus damage từ bên ngoài (Gun gọi trước khi đặt vị trí)
    /// </summary>
    public void ApplyBonusDamage(float bonus)
    {
        damage += bonus;
    }

    // ─────────────────── NETWORK LIFECYCLE ───────────────────

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer)
        {
            // Chỉ đặt bộ đếm giờ tự hủy (bonus damage đã được Gun áp trước đó)
            Invoke(nameof(NetworkDespawn), timeDestroy);
        }
    }

    // ─────────────────── UNITY LIFECYCLE ─────────────────────

    private void Start()
    {
        // Fallback offline: destroy thẳng sau timeDestroy
        if (!isSpawned)
        {
            damage += GameManager.BonusDamage;
            Destroy(gameObject, timeDestroy);
        }
    }

    private void Update()
    {
        MoveBullet();
    }

    // ─────────────────── MOVEMENT ────────────────────────────

    private void MoveBullet()
    {
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }

    // ─────────────────── COLLISION ───────────────────────────

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        // Damage chỉ xử lý trên Server (hoặc offline)
        if (isSpawned && !isServer) return;

        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                hasHit = true;
                enemy.TakeDamage(damage, shooter);
                SpawnBloodEffect();
            }
            NetworkDespawn();
        }
        else if (collision.CompareTag("Rock"))
        {
            NetworkDespawn();
        }
    }

    /// <summary>
    /// Fallback: khi quái đứng quá sát, viên đạn sinh ra đã nằm trong lòng quái,
    /// Unity không gọi OnTriggerEnter2D nên cần dùng OnTriggerStay2D để bắt va chạm.
    /// </summary>
    private bool hasHit = false;
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (hasHit) return;
        if (isSpawned && !isServer) return;

        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                hasHit = true;
                enemy.TakeDamage(damage, shooter);
                SpawnBloodEffect();
            }
            NetworkDespawn();
        }
    }

    // ─────────────────── HELPERS ─────────────────────────────

    private void SpawnBloodEffect()
    {
        if (bloodPrefab == null) return;

        GameObject blood = Instantiate(bloodPrefab, transform.position, Quaternion.identity);
        Destroy(blood, 1f);
    }

    private void NetworkDespawn()
    {
        CancelInvoke(nameof(NetworkDespawn));

        if (isSpawned)
        {
            // Network: despawn qua PurrNet (sync sang tất cả clients)
            Despawn();
        }
        else
        {
            // Offline: destroy bình thường
            Destroy(gameObject);
        }
    }
}
