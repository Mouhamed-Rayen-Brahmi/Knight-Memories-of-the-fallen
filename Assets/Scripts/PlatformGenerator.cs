using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformGenerator : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField] private float worldWidth = 50f;
    [SerializeField] private float worldHeight = 20f;
    [SerializeField] private float groundY = 0f;

    [Header("Enemy Spawning")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private int maxTotalEnemies = 15;

    [Header("Boss Settings")]
    [SerializeField] private GameObject bossPrefab;

    private List<GameObject> instantiatedEnemies = new List<GameObject>();

    void Start()
    {
        GenerateEnemiesAndBossOnly();
    }

    [ContextMenu("Generate Enemies and Boss Only")]
    public void GenerateEnemiesAndBossOnly()
    {
        Debug.Log("=== GENERATING ENEMIES AND BOSS ONLY ===");
        ClearEnemiesAndBossOnly();

        // Spawn regular enemies at random positions
        if (enemyPrefabs != null && enemyPrefabs.Count > 0)
        {
            int totalEnemiesSpawned = 0;
            int maxEnemies = Mathf.Max(1, maxTotalEnemies);
            for (int i = 0; i < maxEnemies; i++)
            {
                GameObject enemyPrefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Count)];
                float x = UnityEngine.Random.Range(-worldWidth * 0.4f, worldWidth * 0.4f);
                float y = UnityEngine.Random.Range(groundY + 2f, worldHeight - 2f);
                Vector3 spawnPos = new Vector3(x, y, 0f);
                GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, transform);
                enemy.name = $"Enemy_{totalEnemiesSpawned + 1}";
                instantiatedEnemies.Add(enemy);
                totalEnemiesSpawned++;
            }
            Debug.Log($"Spawned {totalEnemiesSpawned} enemies.");
        }

        // Spawn boss if BossArea exists
        GameObject bossArea = GameObject.FindGameObjectWithTag("BossArea");
        if (bossArea != null && bossPrefab != null)
        {
            Vector3 bossSpawnPos = bossArea.transform.position + Vector3.up * 1.5f;
            GameObject boss = Instantiate(bossPrefab, bossSpawnPos, Quaternion.identity, transform);
            boss.name = "Boss";
            Debug.Log("✅ Boss spawned in BossArea!");
        }
        else
        {
            Debug.LogWarning("⚠️ BossArea not found or bossPrefab not assigned. Boss will not spawn.");
        }
    }

    private void ClearEnemiesAndBossOnly()
    {
        foreach (GameObject enemy in instantiatedEnemies)
        {
            if (enemy != null)
            {
                if (Application.isPlaying)
                    Destroy(enemy);
                else
                    DestroyImmediate(enemy);
            }
        }
        instantiatedEnemies.Clear();

        GameObject bossObj = GameObject.Find("Boss");
        if (bossObj != null)
        {
            if (Application.isPlaying)
                Destroy(bossObj);
            else
                DestroyImmediate(bossObj);
        }
    }
}