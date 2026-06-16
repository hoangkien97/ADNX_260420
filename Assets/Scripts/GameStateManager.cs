using UnityEngine;
using PurrNet;

/// <summary>
/// Trách nhiệm duy nhất: Lưu trữ Data toàn cầu (Wave, Score, Coin) và đồng bộ JSON config.
/// </summary>
public class GameStateManager : NetworkBehaviour
{
    private GameManager coordinator;

    public int countCoin = 0;
    public int score = 0;
    public int wave = 1;

    [SerializeField] public SyncVar<int> syncWave = new SyncVar<int>(1, ownerAuth: false);
    [SerializeField] public SyncVar<string> syncGameConfigJson = new SyncVar<string>("", ownerAuth: false);

    private void Awake()
    {
        coordinator = GetComponent<GameManager>();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        if (asServer)
        {
            if (EnemyDataManager.Instance != null)
                syncGameConfigJson.value = EnemyDataManager.Instance.GetJsonString();
            syncWave.value = Mathf.Max(1, syncWave.value);
        }
        else
        {
            if (!string.IsNullOrEmpty(syncGameConfigJson.value) && EnemyDataManager.Instance != null)
                EnemyDataManager.Instance.LoadFromJsonString(syncGameConfigJson.value);
        }

        syncGameConfigJson.onChanged += OnGameConfigJsonChanged;
    }

    protected override void OnDespawned(bool asServer)
    {
        base.OnDespawned(asServer);
        syncGameConfigJson.onChanged -= OnGameConfigJsonChanged;
    }

    private void OnGameConfigJsonChanged(string newJson)
    {
        if (!isServer && !string.IsNullOrEmpty(newJson) && EnemyDataManager.Instance != null)
        {
            EnemyDataManager.Instance.LoadFromJsonString(newJson);
        }
    }

    public void AdvanceWave()
    {
        if (isSpawned && isServer) syncWave.value = Mathf.Max(1, syncWave.value + 1);
        else if (!isSpawned) wave++;
    }

    public void ResetState()
    {
        countCoin = 0;
        score = 0;
        wave = 1;
    }
}
