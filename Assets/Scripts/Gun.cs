using UnityEngine;
using TMPro;
using PurrNet;

/// <summary>
/// Gun controller với PurrNet multiplayer support.
/// - isOwner: chỉ local player xử lý input xoay súng, bắn, reload
/// - Bắn → [ServerRpc] → Server spawn bullet
/// - Ammo sync qua SyncVar để hiện đúng trên UI của owner
/// - Gun rotation đồng bộ qua NetworkTransform (gắn trên prefab)
/// </summary>
public class Gun : NetworkBehaviour
{
    private float rotateOffset = 180f;
    [SerializeField] private Transform firePos;
    [SerializeField] private GameObject bulletPrefabs;
    [SerializeField] private float shotDelay = 0.5f;
    private float nextshot;
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private AudioManager audioManager;

    // SyncVar ammo: owner ghi (ownerAuth:true), server xác nhận
    [SerializeField] private SyncVar<int> currentAmmo = new SyncVar<int>(10, ownerAuth: true);

    public float CurrentDamage
    {
        get
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

    // ─────────────────── NETWORK LIFECYCLE ───────────────────

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (isOwner)
        {
            currentAmmo.value = maxAmmo;
        }

        currentAmmo.onChanged += OnAmmoChanged;
        UpdateAmmoText();
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        currentAmmo.onChanged -= OnAmmoChanged;
    }

    // ─────────────────── UNITY LIFECYCLE ─────────────────────

    private void Start()
    {
        // Fallback offline
        if (!isSpawned)
        {
            currentAmmo.value = maxAmmo;
            UpdateAmmoText();
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        // SÚNG NẰM Ở CHILD OBJECT: Phải check quyền sở hữu dựa trên Player cha
        Player parentPlayer = GetComponentInParent<Player>();
        if (parentPlayer != null)
        {
            if (parentPlayer.isSpawned && !parentPlayer.isOwner) return;
        }
        else
        {
            // Fallback nếu súng không nằm trong Player
            if (isSpawned && !isOwner) return;
        }

        RotateGun();
        Shoot();
        ReLoad();
    }

    // ─────────────────── GUN LOGIC ───────────────────────────

    private void RotateGun()
    {
        if (Input.mousePosition.x < 0 || Input.mousePosition.y < 0 ||
            Input.mousePosition.x > Screen.width || Input.mousePosition.y > Screen.height)
            return;

        Vector3 displacement = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float angle = Mathf.Atan2(displacement.y, displacement.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle + rotateOffset);

        // Flip súng dựa theo góc
        transform.localScale = (angle < -90 || angle > 90)
            ? new Vector3(1, 1, 1)
            : new Vector3(1, -1, 1);
    }

    private void Shoot()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.time < nextshot) return;
        if (currentAmmo.value <= 0) return;

        nextshot = Time.time + shotDelay;
        currentAmmo.value--;
        UpdateAmmoText();

        if (audioManager != null)
            audioManager.PlayShootSound();

        if (isSpawned)
        {
            // Networked: gửi lên server để spawn bullet
            CmdShoot(firePos.position, firePos.rotation);
        }
        else
        {
            // Offline fallback: spawn bình thường
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
        UpdateAmmoText();

        if (audioManager != null)
            audioManager.PlayReloadSound();
    }

    // ─────────────────── SERVER RPC ──────────────────────────

    /// <summary>
    /// Owner gửi lên Server yêu cầu spawn bullet tại vị trí và góc xoay đã tính.
    /// </summary>
    [ServerRpc(requireOwnership: true)]
    private void CmdShoot(Vector3 position, Quaternion rotation)
    {
        if (bulletPrefabs == null) return;

        GameObject bullet = Instantiate(bulletPrefabs, position, rotation);

        // Gắn "thông tin người bắn" và áp BonusDamage TRƯỚC khi đặt vị trí
        // (tránh trường hợp quái đứng sát → trigger va chạm trước khi kịp cộng bonus)
        PlayerBullet pb = bullet.GetComponent<PlayerBullet>();
        if (pb != null)
        {
            Player p = GetComponentInParent<Player>();
            pb.SetShooter(p);

            float bonus = p != null ? p.GetBonusDamage() : GameManager.BonusDamage;
            pb.ApplyBonusDamage(bonus);
        }
    }

    // ─────────────────── UI ──────────────────────────────────

    private void OnAmmoChanged(int newAmmo)
    {
        UpdateAmmoText();
    }

    private void UpdateAmmoText()
    {
        if (ammoText == null) return;
        ammoText.text = currentAmmo.value > 0 ? currentAmmo.value.ToString() : "Reload";
    }
}
