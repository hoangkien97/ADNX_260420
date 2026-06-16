using UnityEngine;
using TMPro;

/// <summary>
/// Đảm nhiệm 1 việc duy nhất: Giao tiếp với UI (Chữ) và Âm thanh. 
/// "Ngu ngốc" hoàn toàn: Lắng nghe lệnh từ GunShooter.
/// </summary>
public class GunVisuals : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private AudioManager audioManager;
    
    private GunShooter shooter;

    private void Awake()
    {
        shooter = GetComponent<GunShooter>();
    }

    private void Start()
    {
        if (audioManager == null)
        {
            audioManager = FindAnyObjectByType<AudioManager>();
        }
    }

    private void OnEnable()
    {
        if (shooter != null)
        {
            shooter.OnAmmoChangedEvent += UpdateAmmoText;
            shooter.OnShootEvent += PlayShootSound;
            shooter.OnReloadEvent += PlayReloadSound;
        }
    }

    private void OnDisable()
    {
        if (shooter != null)
        {
            shooter.OnAmmoChangedEvent -= UpdateAmmoText;
            shooter.OnShootEvent -= PlayShootSound;
            shooter.OnReloadEvent -= PlayReloadSound;
        }
    }

    private void UpdateAmmoText(int currentAmmo)
    {
        if (ammoText == null) return;
        ammoText.text = currentAmmo > 0 ? currentAmmo.ToString() : "Reload";
    }

    private void PlayShootSound()
    {
        if (audioManager != null)
            audioManager.PlayShootSound();
    }

    private void PlayReloadSound()
    {
        if (audioManager != null)
            audioManager.PlayReloadSound();
    }
}
