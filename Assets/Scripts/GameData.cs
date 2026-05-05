using System.Collections.Generic;
[System.Serializable]
public class GameData
{
    public List<EnemyDataRecord> enemyDataList = new List<EnemyDataRecord>();
}

[System.Serializable]
public class EnemyDataRecord
{
    public string enemyName;
    public float  maxHp;
    public float  moveSpeed;
    public float  enterDamage;
    public float  stayDamage;
    public float  dropLifetime;
}
