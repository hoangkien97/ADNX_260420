using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Chạy trước tất cả script khác (order -100) để GameConfigSO được nạp từ JSON
/// trước khi Player, Gun, EnemySpawner... đọc giá trị trong Awake/Start của chúng.
/// </summary>
[DefaultExecutionOrder(-100)]
public class EnemyDataManager : MonoBehaviour
{
    public static EnemyDataManager Instance { get; private set; }

    [SerializeField] private EnemyDataSO[] allEnemyData;
    [SerializeField] private string dataFileName = "game_data.json";

    private FileDataHandler<GameData> dataHandler;
    private FileSystemWatcher fileWatcher;
    private bool requiresReload = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        dataHandler = new FileDataHandler<GameData>(Application.persistentDataPath, dataFileName);

        // Load ngay trong Awake để GameConfigSO sẵn sàng trước khi Player/Gun/Spawner... đọc dữ liệu
        Load();
    }

    private void Start()
    {
        // FileWatcher khởi động sau Awake (chỉ theo dõi thay đổi trong khi game chạy)
        SetupFileWatcher();
    }

    /// <summary>
    /// Gọi từ Inspector (chuột phải vào component) để tạo file JSON mẫu.
    /// Bấm lần đầu để sinh game_data.json với giá trị hiện tại từ ScriptableObject.
    /// </summary>
    [ContextMenu("💾 Xuất game_data.json (Save)")]
    public void EditorSave()
    {
        dataHandler = new FileDataHandler<GameData>(Application.persistentDataPath, dataFileName);
        Save();
        Debug.Log($"[EnemyDataManager] Đã lưu tại: {Application.persistentDataPath}/{dataFileName}");
    }

    [ContextMenu("📂 Nạp lại game_data.json (Load)")]
    public void EditorLoad()
    {
        dataHandler = new FileDataHandler<GameData>(Application.persistentDataPath, dataFileName);
        Load();
        Debug.Log($"[EnemyDataManager] Đã load từ: {Application.persistentDataPath}/{dataFileName}");
    }

    private void SetupFileWatcher()
    {
        string path = Application.persistentDataPath;
        if (!Directory.Exists(path)) return;

        fileWatcher = new FileSystemWatcher(path, dataFileName);
        fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        fileWatcher.Changed += (s, e) => requiresReload = true;
        fileWatcher.EnableRaisingEvents = true;
    }

    private void Update()
    {
        if (requiresReload)
        {
            requiresReload = false;
            Invoke(nameof(DelayedLoad), 0.2f);
        }
    }

    private void DelayedLoad()
    {
        Load();
        //Debug.Log("[EnemyDataManager] Tự động cập nhật data vì file JSON vừa thay đổi!");
    }

    private void OnDestroy()
    {
        if (fileWatcher != null)
        {
            fileWatcher.EnableRaisingEvents = false;
            fileWatcher.Dispose();
        }
    }

    public GameConfigSO gameConfig;

    public void Save()
    {
        if (allEnemyData == null || allEnemyData.Length == 0) return;

        GameData data = new GameData();
        
        // 1. Enemy
        foreach (EnemyDataSO so in allEnemyData)
        {
            if (so == null) continue;
            data.enemyDataList.Add(new EnemyDataRecord
            {
                enemyName    = so.enemyName,
                maxHp        = so.maxHp,
                moveSpeed    = so.moveSpeed,
                enterDamage  = so.enterDamage,
                stayDamage   = so.stayDamage,
                dropLifetime = so.dropLifetime,
                dropPrefabName = so.dropPrefab != null ? so.dropPrefab.name : ""
            });
        }

        // 2. Global Configs
        if (gameConfig != null)
        {
            data.player.maxHp = gameConfig.playerMaxHp;
            data.player.speed = gameConfig.playerSpeed;
            data.player.damage = gameConfig.playerDamage;

            data.gun.shotDelay = gameConfig.shotDelay;
            data.gun.maxAmmo = gameConfig.maxAmmo;

            data.bullet.moveSpeed = gameConfig.bulletMoveSpeed;
            data.bullet.timeDestroy = gameConfig.bulletTimeDestroy;
            data.bullet.damage = gameConfig.playerDamage;

            data.spawner.maxEnemiesInWave = gameConfig.maxEnemiesInWave;
            data.spawner.timeBetweenSpawns = gameConfig.timeBetweenSpawns;
            data.spawner.numberScale = gameConfig.numberScale;
            data.spawner.bonusCoin = gameConfig.bonusCoin;

            data.heal.healValue = gameConfig.healValue;

            data.shop.upgradeSpeed.baseCost = gameConfig.speedBaseCost;
            data.shop.upgradeSpeed.effectValue = gameConfig.speedEffectValue;
            
            data.shop.upgradeDamage.baseCost = gameConfig.damageBaseCost;
            data.shop.upgradeDamage.effectValue = gameConfig.damageEffectValue;

            data.shop.upgradeMaxHP.baseCost = gameConfig.hpBaseCost;
            data.shop.upgradeMaxHP.effectValue = gameConfig.hpEffectValue;
        }

        dataHandler.Save(data);
    }

    public void Load()
    {
        GameData data = dataHandler.Load();
        if (data == null) return;

        ApplyDataToSO(data);
    }
    private void ApplyDataToSO(GameData data)
    {
        var soMap = new Dictionary<string, EnemyDataSO>();
        foreach (EnemyDataSO so in allEnemyData)
            if (so != null && !soMap.ContainsKey(so.enemyName))
                soMap[so.enemyName] = so;

        foreach (EnemyDataRecord r in data.enemyDataList)
        {
            if (!soMap.TryGetValue(r.enemyName, out EnemyDataSO target)) continue;
            target.maxHp        = r.maxHp;
            target.moveSpeed    = r.moveSpeed;
            target.enterDamage  = r.enterDamage;
            target.stayDamage   = r.stayDamage;
            target.dropLifetime = r.dropLifetime;

            if (!string.IsNullOrEmpty(r.dropPrefabName))
            {
                GameObject prefab = Resources.Load<GameObject>("EnemyDrops/" + r.dropPrefabName);
                if (prefab != null) target.dropPrefab = prefab;
                else Debug.LogWarning($"[EnemyDataManager] Drop prefab not found: EnemyDrops/{r.dropPrefabName}");
            }
            else
            {
                target.dropPrefab = null; 
            }
        }

        // 2. Global Configs
        if (gameConfig != null)
        {
            gameConfig.playerMaxHp = data.player.maxHp;
            gameConfig.playerSpeed = data.player.speed;
            gameConfig.playerDamage = data.player.damage;

            gameConfig.shotDelay = data.gun.shotDelay;
            gameConfig.maxAmmo = data.gun.maxAmmo;

            gameConfig.bulletMoveSpeed = data.bullet.moveSpeed;
            gameConfig.bulletTimeDestroy = data.bullet.timeDestroy;

            gameConfig.maxEnemiesInWave = data.spawner.maxEnemiesInWave;
            gameConfig.timeBetweenSpawns = data.spawner.timeBetweenSpawns;
            gameConfig.numberScale = data.spawner.numberScale;
            gameConfig.bonusCoin = data.spawner.bonusCoin;

            gameConfig.healValue = data.heal.healValue;

            gameConfig.speedBaseCost = data.shop.upgradeSpeed.baseCost;
            gameConfig.speedEffectValue = data.shop.upgradeSpeed.effectValue;

            gameConfig.damageBaseCost = data.shop.upgradeDamage.baseCost;
            gameConfig.damageEffectValue = data.shop.upgradeDamage.effectValue;

            gameConfig.hpBaseCost = data.shop.upgradeMaxHP.baseCost;
            gameConfig.hpEffectValue = data.shop.upgradeMaxHP.effectValue;
        }
    }

    public void DeleteSave()
    {
        dataHandler = new FileDataHandler<GameData>(Application.persistentDataPath, dataFileName);
        System.IO.File.Delete(System.IO.Path.Combine(Application.persistentDataPath, dataFileName));
    }

    // ─────────────────── MẠNG (MULTIPLAYER SYNC) ─────────────────
    
    public string GetJsonString()
    {
        if (dataHandler != null)
        {
            GameData data = dataHandler.Load();
            if (data != null)
            {
                return JsonUtility.ToJson(data);
            }
        }
        return "";
    }

    public void LoadFromJsonString(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString)) return;

        try
        {
            GameData data = JsonUtility.FromJson<GameData>(jsonString);
            if (data != null)
            {
                ApplyDataToSO(data);
                Debug.Log("[EnemyDataManager] Đã cập nhật cấu hình từ Server (Multiplayer).");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyDataManager] Lỗi khi đọc JSON từ Server: {e.Message}");
        }
    }
}
