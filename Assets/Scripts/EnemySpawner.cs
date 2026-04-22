using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private float timeBetweenSpawns = 2f;
    [SerializeField] private int poolSize = 10;

    private List<GameObject> enemyPool;

    void Start()
    {
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
        while (true)
        {
            yield return new WaitForSecondsRealtime(timeBetweenSpawns);

            Camera cam = Camera.main;
            if (cam == null) yield break; 

            float height = cam.orthographicSize;
            float width = height * cam.aspect;
            Vector3 camPos = cam.transform.position;

            Vector3[] spawnPoints = new Vector3[]
            {
            camPos + new Vector3(-width, -height, 0),
            camPos + new Vector3( width, -height, 0),
            camPos + new Vector3(-width,  height, 0),
            camPos + new Vector3( width,  height, 0),
            };
            SpawnEnemy(spawnPoints[Random.Range(0, spawnPoints.Length)]);
        }
    }

    private void SpawnEnemy(Vector3 spawnPosition)
    {
        if (enemyPool.Count == 0) return;

        int index = Random.Range(0, enemyPool.Count);
        GameObject enemy = enemyPool[index];
        enemyPool.RemoveAt(index);

        GameObject newPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (enemy.name != newPrefab.name + "(Clone)")
        {
            Destroy(enemy);
            enemy = Instantiate(newPrefab);
        }

        enemy.transform.position = spawnPosition;
        enemy.SetActive(true);
        enemy.GetComponent<Enemy>().Initialize(this);
    }

    public void ReturnEnemyToPool(GameObject enemy)
    {
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
}