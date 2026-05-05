using System.Collections.Generic;
using UnityEngine;

public class EnemyDataManager : MonoBehaviour
{
    public static EnemyDataManager Instance { get; private set; }

    [SerializeField] private EnemyDataSO[] allEnemyData;
    [SerializeField] private string dataFileName = "enemy_data.json";

    private FileDataHandler dataHandler;

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
                dropLifetime = so.dropLifetime
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
        }
    }

    public void DeleteSave()
    {
        dataHandler = new FileDataHandler(Application.persistentDataPath, dataFileName);
        System.IO.File.Delete(System.IO.Path.Combine(Application.persistentDataPath, dataFileName));
    }

}
