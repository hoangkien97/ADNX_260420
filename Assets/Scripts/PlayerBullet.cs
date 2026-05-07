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

    public float BaseDamage => damage;

    // ─────────────────── NETWORK LIFECYCLE ───────────────────

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer)
        {
            // Server áp bonus damage và tự despawn sau timeDestroy giây
            damage += GameManager.BonusDamage;
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
        // Damage chỉ xử lý trên Server (hoặc offline)
        if (isSpawned && !isServer) return;

        if (collision.CompareTag("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                SpawnBloodEffect();
            }
            NetworkDespawn();
        }
        else if (collision.CompareTag("Rock"))
        {
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
