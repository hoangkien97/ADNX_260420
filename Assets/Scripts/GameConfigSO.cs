using UnityEngine;

/// <summary>
/// Chứa toàn bộ config có thể tuỳ chỉnh qua JSON.
/// Tạo 1 asset duy nhất: Assets/Data/GameConfig.asset
/// GameDataManager sẽ load JSON và ghi đè các giá trị này.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Config", order = 0)]
public class GameConfigSO : ScriptableObject
{
    [Header("── Player Stats ──")]
    public float playerMaxHp    = 100f;
    public float playerSpeed    = 5f;
    public float playerDamage   = 50f;  // base damage của viên đạn

    [Header("── Gun ──")]
    public float shotDelay      = 0.5f;
    public int   maxAmmo        = 10;

    [Header("── Bullet ──")]
    public float bulletMoveSpeed  = 10f;
    public float bulletTimeDestroy = 1f;

    [Header("── Heal Item ──")]
    public float healValue      = 20f;

    [Header("── Enemy Spawner ──")]
    public int   maxEnemiesInWave   = 10;
    public float timeBetweenSpawns  = 2f;
    public float numberScale        = 1.5f;
    public int   bonusCoin          = 5;

    [Header("── Shop: UpgradeSpeed ──")]
    public int   speedBaseCost      = 3;
    public float speedEffectValue   = 1f;

    [Header("── Shop: UpgradeDamage ──")]
    public int   damageBaseCost     = 3;
    public float damageEffectValue  = 10f;

    [Header("── Shop: UpgradeMaxHP ──")]
    public int   hpBaseCost         = 3;
    public float hpEffectValue      = 20f;
}
