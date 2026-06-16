using UnityEngine;
using PurrNet;

/// <summary>
/// Đảm nhiệm 1 việc duy nhất: Quản lý băng đạn, tính toán Cooldown, và lệnh gọi lên Server để bắn mạng.
/// </summary>
public class GunShooter : NetworkBehaviour
{
    [SerializeField] private Transform firePos;
    [SerializeField] private GameObject bulletPrefabs;
    [SerializeField] private float shotDelay = 0.5f;
    [SerializeField] private int maxAmmo = 10;
    
    private float nextshot;
    
    [SerializeField] private SyncVar<int> currentAmmo = new SyncVar<int>(10, ownerAuth: true);

    public System.Action<int> OnAmmoChangedEvent;
    public System.Action OnShootEvent;
    public System.Action OnReloadEvent;

    private static GameConfigSO GameConfig => EnemyDataManager.Instance?.gameConfig;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        
        GameConfigSO cfg = GameConfig;
        if (cfg != null)
        {
            shotDelay = cfg.shotDelay;
            maxAmmo = cfg.maxAmmo;
        }

        if (isOwner)
        {
            currentAmmo.value = maxAmmo;
        }

        currentAmmo.onChanged += HandleAmmoChanged;
        HandleAmmoChanged(currentAmmo.value);
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        currentAmmo.onChanged -= HandleAmmoChanged;
    }

    private void Start()
    {
        if (!isSpawned)
        {
            GameConfigSO cfg = GameConfig;
            if (cfg != null)
            {
                shotDelay = cfg.shotDelay;
                maxAmmo = cfg.maxAmmo;
            }
            currentAmmo.value = maxAmmo;
            HandleAmmoChanged(currentAmmo.value);
        }
    }

    private void HandleAmmoChanged(int newAmmo)
    {
        OnAmmoChangedEvent?.Invoke(newAmmo);
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        Player parentPlayer = GetComponentInParent<Player>();
        if (parentPlayer != null && parentPlayer.isSpawned && !parentPlayer.isOwner) return;
        if (parentPlayer == null && isSpawned && !isOwner) return;

        Shoot();
        ReLoad();
    }

    private void Shoot()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.time < nextshot) return;
        if (currentAmmo.value <= 0) return;

        nextshot = Time.time + shotDelay;
        currentAmmo.value--;

        OnShootEvent?.Invoke();

        if (isSpawned)
        {
            CmdShoot(firePos.position, firePos.rotation);
        }
        else
        {
            if (bulletPrefabs != null)
            {
                GameObject b = Instantiate(bulletPrefabs);
                b.transform.SetPositionAndRotation(firePos.position, firePos.rotation);

                PlayerBullet pb = b.GetComponent<PlayerBullet>();
                if (pb != null)
                {
                    Player p = GetComponentInParent<Player>();
                    pb.SetShooter(p);
                    float bonus = p != null ? p.GetBonusDamage() : GameManager.BonusDamage;
                    pb.ApplyBonusDamage(bonus);
                }
            }
        }
    }

    private void ReLoad()
    {
        if (!Input.GetKeyDown(KeyCode.R)) return;
        
        currentAmmo.value = maxAmmo;
        OnReloadEvent?.Invoke();
    }

    [ServerRpc(requireOwnership: true)]
    private void CmdShoot(Vector3 position, Quaternion rotation)
    {
        if (bulletPrefabs == null) return;

        GameObject bullet = Instantiate(bulletPrefabs, position, rotation);
        PlayerBullet pb = bullet.GetComponent<PlayerBullet>();
        if (pb != null)
        {
            Player p = GetComponentInParent<Player>();
            pb.SetShooter(p);
            float bonus = p != null ? p.GetBonusDamage() : GameManager.BonusDamage;
            pb.ApplyBonusDamage(bonus);
        }
    }

    public float GetCurrentDamage()
    {
        float bonus = 0f;
        Player p = GetComponentInParent<Player>();
        if (p != null) bonus = p.GetBonusDamage();
        else bonus = GameManager.BonusDamage;

        if (bulletPrefabs != null && bulletPrefabs.TryGetComponent<PlayerBullet>(out PlayerBullet bullet))
            return bullet.BaseDamage + bonus;
        return bonus;
    }
}
