using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data", order = 0)]
public class EnemyDataSO : ScriptableObject
{
    [Header("Info")]
    public string enemyName = "Enemy";

    [Header("Stats")]
    public float maxHp       = 100f;
    public float moveSpeed   = 2f;
    public float enterDamage = 10f;
    public float stayDamage  = 1f;

    [Header("Drop")]
    public GameObject dropPrefab;
    public float dropLifetime = 7f;
}
