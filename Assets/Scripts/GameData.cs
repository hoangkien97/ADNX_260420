using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    public List<EnemyDataRecord> enemyDataList = new List<EnemyDataRecord>();
    public PlayerConfigRecord player = new PlayerConfigRecord();
    public GunConfigRecord gun = new GunConfigRecord();
    public BulletConfigRecord bullet = new BulletConfigRecord();
    public SpawnerConfigRecord spawner = new SpawnerConfigRecord();
    public HealConfigRecord heal = new HealConfigRecord();
    public ShopConfigRecord shop = new ShopConfigRecord();
}

[System.Serializable]
public class EnemyDataRecord
{
    public string enemyName;
    public float maxHp;
    public float moveSpeed;
    public float enterDamage;
    public float stayDamage;
    public float dropLifetime;
    public string dropPrefabName;
}

[System.Serializable]
public class PlayerConfigRecord
{
    public float maxHp = 100f;
    public float speed = 5f;
    public float damage = 50f;
}

[System.Serializable]
public class GunConfigRecord
{
    public float shotDelay = 0.5f;
    public int maxAmmo = 10;
}

[System.Serializable]
public class BulletConfigRecord
{
    public float moveSpeed = 10f;
    public float timeDestroy = 1f;
    public float damage = 50f;
}

[System.Serializable]
public class SpawnerConfigRecord
{
    public int maxEnemiesInWave = 10;
    public float timeBetweenSpawns = 2f;
    public float numberScale = 1.5f;
    public int bonusCoin = 5;
}

[System.Serializable]
public class HealConfigRecord
{
    public float healValue = 20f;
}

[System.Serializable]
public class ShopUpgradeRecord
{
    public int baseCost = 3;
    public float effectValue = 1f;
}

[System.Serializable]
public class ShopConfigRecord
{
    public ShopUpgradeRecord upgradeSpeed = new ShopUpgradeRecord();
    public ShopUpgradeRecord upgradeDamage = new ShopUpgradeRecord();
    public ShopUpgradeRecord upgradeMaxHP = new ShopUpgradeRecord();
}
