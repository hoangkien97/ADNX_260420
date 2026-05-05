using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private float timeBetweenSpawns = 2f;
    [SerializeField] private int poolSize = 10;
    private List<GameObject> enemyPool;
    [SerializeField] private float numberScale = 1.5f;
    [SerializeField] private GameObject shopPanel;
    private int deadEnemiesCount = 0;
    private int enemiesSpawnedThisWave = 0;
    private float currentStatMultiplier = 1f;
    [SerializeField] private int bonusCoin = 5;

    void Start()
    {
        Time.timeScale = 1f;
        enemyPool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject enemy = Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Length)]);
            enemy.SetActive(false);
            enemyPool.Add(enemy);
        }
        StartCoroutine(SpawnEnemyCoroutine());
    }

    private IEnumerator SpawnEnemyCoroutine()
    {
        // Chờ A* graph được scan xong trước khi bắt đầu spawn
        yield return new WaitUntil(() => AstarPath.active != null && AstarPath.active.isScanning == false && AstarPath.active.graphs != null);
        yield return new WaitForSeconds(1f); // thêm buffer nhỏ cho chắc

        while (true)
        {
            yield return new WaitForSeconds(timeBetweenSpawns);

            if (shopPanel != null && shopPanel.activeInHierarchy)
            {
                continue; 
            }

            Camera cam = Camera.main;
            if (cam == null) yield break; 

            float height = cam.orthographicSize;
            float width = height * cam.aspect;
            Vector3 camPos = cam.transform.position;
            camPos.z = 0f;

            Vector3[] spawnPoints = new Vector3[]
            {
            camPos + new Vector3(-width, -height, 0),
            camPos + new Vector3( width, -height, 0),
            camPos + new Vector3(-width,  height, 0),
            camPos + new Vector3( width,  height, 0),
            };

            if (enemiesSpawnedThisWave < poolSize)
            {
                SpawnEnemy(spawnPoints[Random.Range(0, spawnPoints.Length)]);
                enemiesSpawnedThisWave++;
            }
        }
    }

    private void SpawnEnemy(Vector3 spawnPosition)
    {
        if (enemyPool.Count == 0) return;
        GameObject enemy = enemyPool[0];
        enemyPool.RemoveAt(0);
        enemy.transform.position = spawnPosition;
        enemy.SetActive(true);
        Enemy e = enemy.GetComponent<Enemy>();
        e.Initialize(this);
        e.ApplyStatMultiplier(currentStatMultiplier);
    }

    public void ReturnEnemyToPool(GameObject enemy)
    {
        if (enemy == null) return;

        if (enemyPool.Count < poolSize)
        {
            enemy.SetActive(false);
            enemyPool.Add(enemy);
        }
        else
        {
            Destroy(enemy);
        }
    }

    public void OnEnemyDied()
    {
        deadEnemiesCount++;
        if (deadEnemiesCount >= poolSize)
        {
            StartCoroutine(WaveCompletedRoutine());
        }
    }

    private IEnumerator WaveCompletedRoutine()
    {
        GameManager.CountCoin += bonusCoin;
        yield return new WaitForSeconds(3f);

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }
        Time.timeScale = 0f;

        int newPoolSize = Mathf.RoundToInt(poolSize * numberScale);
        int addedEnemies = newPoolSize - poolSize;
        poolSize = newPoolSize;
        currentStatMultiplier *= numberScale;
        deadEnemiesCount = 0;
        enemiesSpawnedThisWave = 0; 
        GameManager.AdvanceWave();

        for (int i = 0; i < addedEnemies; i++)
        {
            GameObject enemyObj = Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Length)]);
            enemyObj.SetActive(false);
            enemyPool.Add(enemyObj);
        }
    }
}
