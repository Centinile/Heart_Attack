using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Setup")]
    public WaveData[] waves;
    public Button startWaveButton;
    public Transform[] wayPoints;

    [Header("Spawn Area")]
    public BoxCollider2D spawnBounds;
    public float spawnOutsideOffset = 1f; // how far outside map enemies appear

    public int currentWaveIndex;
    public bool waveRunning = false;

    [Header("Freeplay")]
    public bool freeplayMode = false;
    public EnemyRaidGroup[] availableGroups;

    public int baseFreeplayWeight = 50;
    public int weightIncreasePerWave = 5;

    void Start()
    {
        startWaveButton.onClick.AddListener(StartWave);
    }

    public void StartWave()
    {
        if (waveRunning) return;

        // Allow freeplay after designed waves end
        if (!freeplayMode && currentWaveIndex >= waves.Length)
            freeplayMode = true;

        StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        waveRunning = true;
        startWaveButton.interactable = false;

        List<Coroutine> activeSpawns = new List<Coroutine>();

        if (!freeplayMode && currentWaveIndex < waves.Length)
        {
            WaveData wave = waves[currentWaveIndex];

            Debug.Log($"Mode: DESIGNED WAVE");
            LogSpawnData("Designed Wave", wave);

            activeSpawns.Add(StartCoroutine(SpawnGroup(waves[currentWaveIndex])));
        }
        else
        {
            Debug.Log($"Mode: FREEPLAY");

            List<EnemyRaidGroup> groups = GenerateFreeplayWave(currentWaveIndex);

            foreach (var group in groups)
            {
                LogSpawnData("Raid Group", group);
                activeSpawns.Add(StartCoroutine(SpawnGroup(group)));
            }

            Debug.Log($"========== WAVE {currentWaveIndex} END ==========");

            waveRunning = false;
            startWaveButton.interactable = true;
            currentWaveIndex++;
        }

        // Wait until all spawns finish
        foreach (var routine in activeSpawns)
        {
            yield return routine;
        }

        waveRunning = false;
        startWaveButton.interactable = true;
        currentWaveIndex++;
    }

    // ==============================
    // SPAWNING
    // ==============================

    void SpawnEnemy(GameObject prefab)
    {
        Vector3 spawnPos = GenerateEdgePosition();

        GameObject e = Instantiate(prefab, spawnPos, Quaternion.identity);
        Enemy enemy = e.GetComponent<Enemy>();

    }

    Vector3 GenerateEdgePosition()
    {
        Bounds bounds = spawnBounds.bounds;

        float minX = bounds.min.x;
        float maxX = bounds.max.x;
        float minY = bounds.min.y;
        float maxY = bounds.max.y;

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);

        switch (Random.Range(0, 4))
        {
            // Left
            case 0:
                return new Vector3(minX - spawnOutsideOffset, y, 0);

            // Right
            case 1:
                return new Vector3(maxX + spawnOutsideOffset, y, 0);

            // Bottom
            case 2:
                return new Vector3(x, minY - spawnOutsideOffset, 0);

            // Top
            default:
                return new Vector3(x, maxY + spawnOutsideOffset, 0);
        }
    }

    public bool IsWithinBounds(Transform target)
    {
        return spawnBounds.bounds.Contains(target.position);
    }

    // ==============================
    // FREEPLAY GENERATION
    // ==============================

    List<EnemyRaidGroup> GenerateFreeplayWave(int waveNumber)
    {
        int weightLimit = baseFreeplayWeight + (waveNumber * weightIncreasePerWave);
        int currentWeight = 0;

        Debug.Log($"Freeplay Weight Limit: {weightLimit}");

        List<EnemyRaidGroup> selectedGroups = new List<EnemyRaidGroup>();

        int safety = 0; // prevents infinite loop

        while (currentWeight < weightLimit && safety < 500)
        {
            EnemyRaidGroup randomGroup =
                availableGroups[Random.Range(0, availableGroups.Length)];

            if (currentWeight + randomGroup.weightCost > weightLimit)
            {
                safety++;
                continue;
            }

            selectedGroups.Add(randomGroup);
            currentWeight += randomGroup.weightCost;

            Debug.Log($"Added Group: {randomGroup.name} | Cost: {randomGroup.weightCost} | Total Weight: {currentWeight}");
        }

        Debug.Log($"Final Freeplay Weight Used: {currentWeight}");

        return selectedGroups;
    }

    // ==============================
    // GROUP SPAWNING
    // ==============================

    IEnumerator SpawnGroup(SpawnData data)
    {
        List<GameObject> queue = data.BuildSpawnQueue();

        foreach (var prefab in queue)
        {
            SpawnEnemy(prefab);
            yield return new WaitForSeconds(0.05f); // spawn next frame
        }
    }

    void LogSpawnData(string header, SpawnData data)
    {
        string log = $"[{header}] {data.name} → ";

        foreach (var entry in data.spawnEntries)
        {
            log += $"{entry.prefab.name} x{entry.count}, ";
        }

        Debug.Log(log);
    }

}
