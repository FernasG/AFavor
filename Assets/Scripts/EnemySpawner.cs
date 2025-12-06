using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Configuração de Spawn")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public float timeBetweenSpawns = 5f;

    [Header("Limite de Inimigos")]
    public int maxEnemies = 2;
    private int currentEnemies = 0;


    private float nextSpawnTime;

    void Start()
    {

        nextSpawnTime = Time.time;


        UpdateEnemyCount();
    }

    void Update()
    {

        if (Time.time >= nextSpawnTime)
        {
            TrySpawnEnemy();
            nextSpawnTime = Time.time + timeBetweenSpawns;
        }
    }

    void UpdateEnemyCount()
    {

        currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;


    }

    void TrySpawnEnemy()
    {
        UpdateEnemyCount();

        if (currentEnemies < maxEnemies)
        {

            if (spawnPoints.Length == 0)
            {
                Debug.LogError("Nenhum ponto de spawn configurado!");
                return;
            }
            int spawnPointIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[spawnPointIndex];


            GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);


            newEnemy.transform.SetParent(this.transform);


            currentEnemies++;

            Debug.Log($"Inimigo criado! Total atual: {currentEnemies}/{maxEnemies}");
        }
        else
        {

        }
    }
}