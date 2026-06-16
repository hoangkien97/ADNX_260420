using UnityEngine;
using PurrNet;

/// <summary>
/// Trọng tài (Coordinator) cho vũ khí.
/// Đã được chia nhỏ thành GunRotation, GunShooter và GunVisuals theo SRP.
/// Script này đóng vai trò giữ nguyên các interface cũ (như CurrentDamage) 
/// để không làm hỏng các hệ thống bên ngoài đang gọi tới Gun.
/// </summary>
public class Gun : NetworkBehaviour
{
    private GunShooter shooter;

    private void Awake()
    {
        shooter = GetComponent<GunShooter>();
    }

    public float CurrentDamage
    {
        get
        {
            if (shooter != null) return shooter.GetCurrentDamage();
            return 0f;
        }
    }
}
