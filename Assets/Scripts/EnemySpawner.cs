using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PurrNet;

/// <summary>
/// EnemySpawner với PurrNet multiplayer support.
/// - Toàn bộ spawn logic chỉ chạy trên Server (Host)
/// - Pool dùng UnityProxy.InstantiateDirectly() để bypass PurrNet tracking
/// - Spawn position dựa trên player gần nhất còn sống
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private float timeBetweenSpawns = 2f;
    [SerializeField] private int maxEnemiesInWave = 10;
    [SerializeField] private float numberScale = 1.5f;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private int bonusCoin = 5;

    private int deadEnemiesCount = 0;
    private int enemiesSpawnedThisWave = 0;
    private float currentStatMultiplier = 1f;

    void Start()
    {
        // Chỉ Server mới chạy logic sinh quái
        if (NetworkManager.main != null && !NetworkManager.main.isServer)
        {
            Debug.Log("[EnemySpawner] Client mode: skipping spawn logic.");
            return;
        }

        Time.timeScale = 1f;
        StartCoroutine(SpawnEnemyCoroutine());
    }

    private IEnumerator SpawnEnemyCoroutine()
    {
        // Chờ A* graph scan xong
        yield return new WaitUntil(() =>
            AstarPath.active != null &&
            !AstarPath.active.isScanning &&
            AstarPath.active.graphs != null);
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return new WaitForSeconds(timeBetweenSpawns);

            if (shopPanel != null && shopPanel.activeInHierarchy)
                continue;

            if (enemiesSpawnedThisWave < maxEnemiesInWave)
            {
                Vector3 spawnPos = GetSpawnPositionNearRandomPlayer();
                SpawnEnemyNetworked(spawnPos);
                enemiesSpawnedThisWave++;
            }
        }
    }

    private Vector3 GetSpawnPositionNearRandomPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        List<Player> alive = new List<Player>();
        foreach (var p in players)
            if (p != null && !p.IsDead && p.gameObject.activeInHierarchy)
                alive.Add(p);

        Vector3 center = alive.Count > 0
            ? alive[Random.Range(0, alive.Count)].transform.position
            : Camera.main != null ? Camera.main.transform.position : Vector3.zero;

        return GetSpawnEdgePosition(center);
    }

    private Vector3 GetSpawnEdgePosition(Vector3 center)
    {
        Camera cam = Camera.main;
        if (cam == null) return center + new Vector3(10f, 0f, 0f);

        float height = cam.orthographicSize;
        float width  = height * cam.aspect;
        center.z = 0f;

        Vector3[] corners = new Vector3[]
        {
            center + new Vector3(-width, -height, 0),
            center + new Vector3( width, -height, 0),
            center + new Vector3(-width,  height, 0),
            center + new Vector3( width,  height, 0),
        };
        return corners[Random.Range(0, corners.Length)];
    }

    private void SpawnEnemyNetworked(Vector3 spawnPosition)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        
        // PurrNet tracking: dùng UnityProxy khi chưa track, Instantiate bình thường khi đã track
        GameObject enemyObj;
        if (NetworkManager.main != null && NetworkManager.main.sceneModule != null)
            enemyObj = UnityProxy.InstantiateDirectly(prefab);
        else
            enemyObj = Instantiate(prefab);

        enemyObj.transform.position = spawnPosition;
        enemyObj.SetActive(true);

        // BẮT BUỘC: Spawn lên mạng để sync tới Client
        if (enemyObj.TryGetComponent<NetworkIdentity>(out var netId) && NetworkManager.main != null)
        {
            netId.Spawn(prefab, NetworkManager.main);
        }

        Enemy e = enemyObj.GetComponent<Enemy>();
        if (e != null)
        {
            e.Initialize(this);
            e.ApplyStatMultiplier(currentStatMultiplier);
        }
    }

    public void OnEnemyDied()
    {
        deadEnemiesCount++;
        if (deadEnemiesCount >= maxEnemiesInWave)
            StartCoroutine(WaveCompletedRoutine());
    }

    private IEnumerator WaveCompletedRoutine()
    {
        GameManager.CountCoin += bonusCoin;
        yield return new WaitForSeconds(3f);

        if (shopPanel != null) shopPanel.SetActive(true);
        Time.timeScale = 0f;

        maxEnemiesInWave = Mathf.RoundToInt(maxEnemiesInWave * numberScale);
        currentStatMultiplier *= numberScale;
        deadEnemiesCount       = 0;
        enemiesSpawnedThisWave = 0;
        GameManager.AdvanceWave();
    }
}
