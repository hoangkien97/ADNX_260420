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
            if (bulletPrefabs != null && bulletPrefabs.TryGetComponent<PlayerBullet>(out PlayerBullet bullet))
                return bullet.BaseDamage + GameManager.BonusDamage;
            return GameManager.BonusDamage;
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

        // Chỉ owner mới xử lý input
        if (isSpawned && !isOwner) return;

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
            // Offline fallback: dùng InstantiateDirectly tránh bị PurrNet chặn
            if (bulletPrefabs != null)
            {
                GameObject b = UnityProxy.InstantiateDirectly(bulletPrefabs);
                b.transform.SetPositionAndRotation(firePos.position, firePos.rotation);
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

        // Spawn bullet dưới dạng thường – PurrNet sẽ tự track nếu có NetworkIdentity
        GameObject bullet = UnityProxy.InstantiateDirectly(bulletPrefabs);
        bullet.transform.SetPositionAndRotation(position, rotation);

        // Nếu bullet có NetworkIdentity, spawn nó lên network
        if (bullet.TryGetComponent<NetworkIdentity>(out var netId))
            netId.Spawn(bulletPrefabs, networkManager);
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
