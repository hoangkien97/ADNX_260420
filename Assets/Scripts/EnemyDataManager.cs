using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EnemyDataManager : MonoBehaviour
{
    public static EnemyDataManager Instance { get; private set; }

    [SerializeField] private EnemyDataSO[] allEnemyData;
    [SerializeField] private string dataFileName = "enemy_data.json";

    private FileDataHandler dataHandler;
    private FileSystemWatcher fileWatcher;
    private bool requiresReload = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        dataHandler = new FileDataHandler(Application.persistentDataPath, dataFileName);
    }

    private void Start()
    {
        Load();
        SetupFileWatcher();
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
        Debug.Log("[EnemyDataManager] Tự động cập nhật data vì file JSON vừa thay đổi!");
    }

    private void OnDestroy()
    {
        if (fileWatcher != null)
        {
            fileWatcher.EnableRaisingEvents = false;
            fileWatcher.Dispose();
        }
    }

    public void Save()
    {
        if (allEnemyData == null || allEnemyData.Length == 0) return;

        GameData data = new GameData();
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

        dataHandler.Save(data);
    }

    public void Load()
    {
        GameData data = dataHandler.Load();
        if (data == null) return;

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
        }
    }

    public void DeleteSave()
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, dataFileName);
        System.IO.File.Delete(System.IO.Path.Combine(Application.persistentDataPath, dataFileName));
    }

}
