using UnityEngine;

public class HealItem : MonoBehaviour
{
    [SerializeField] public float healValue = 20f;

    private static GameConfigSO GameConfig => EnemyDataManager.Instance?.gameConfig;

    private void Awake()
    {
        GameConfigSO cfg = GameConfig;
        if (cfg != null)
            healValue = cfg.healValue;
    }
}
