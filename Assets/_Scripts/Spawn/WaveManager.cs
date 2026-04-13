using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static System.Action OnWaveCleared;
    [Header("Wave Setup")]
    public WaveData[] waves;
    public Button startWaveButton;
    public int currentWaveIndex;
    public bool waveRunning = false;

    [Header("Automation Settings")]
    public bool autoStartNextWave = false; 
    public float timeBetweenWaves = 5f;

    [Header("Scaling Settings")]
    public bool enableScaling = false; 
    public float scalingPerWave = 0.02f; 

    [Header("Spawn Area")]
    public PolygonCollider2D spawnBounds;
    public float spawnOutsideOffset = 1f;

    [Header("Freeplay")]
    public bool freeplayMode = false;
    public EnemyRaidGroup[] availableGroups;
    public int baseFreeplayWeight = 50;
    public int weightIncreasePerWave = 5;

    void Start()
    {
        if(startWaveButton != null)
            startWaveButton.onClick.AddListener(StartWave);
    }

    public void ToggleAutoStart(bool value)
    {
        autoStartNextWave = value;
        if (autoStartNextWave && !waveRunning) StartWave();
    }

    public void StartWave()
    {
        if (waveRunning) return;
        StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        waveRunning = true;
        // Tell the GameManager we are in Gameplay
        GameManager.Instance.EnterGameplayPhase();

        if (startWaveButton != null) startWaveButton.interactable = false;

        // --- Freeplay Check ---
        // If current index is at or beyond the hand-designed waves, lock into freeplay
        if (currentWaveIndex >= waves.Length) 
        {
            freeplayMode = true;
        }

        string modeColor = freeplayMode ? "orange" : "cyan";
        Debug.Log($"<color={modeColor}><b>[WAVE {currentWaveIndex + 1}]</b> STARTED ({ (freeplayMode ? "FREEPLAY" : "DESIGNED") })</color>");
        
        float totalNutrientReward = 0;

        // --- STEP 1: SPAWNING PHASE ---
        if (!freeplayMode)
        {
            Debug.Log($"Wave {currentWaveIndex + 1}: DESIGNED");
            WaveData currentWaveData = waves[currentWaveIndex];
            totalNutrientReward = currentWaveData.NutrientReward;
            yield return StartCoroutine(SpawnGroup(currentWaveData));
        }
        else
        {
            Debug.Log($"Wave {currentWaveIndex + 1}: FREEPLAY");
            // Freeplay logic
            List<EnemyRaidGroup> selectedGroups = GenerateFreeplayWave(currentWaveIndex);
            foreach(var group in selectedGroups) totalNutrientReward += group.NutrientReward;

            List<string> raidNames = new List<string>();
            foreach(var g in selectedGroups) 
                raidNames.Add(string.IsNullOrEmpty(g.raidName) ? g.name : g.raidName);
            Debug.Log($"<color=orange><b>[FREEPLAY WAVE {currentWaveIndex + 1}]</b> Raids Joining: {string.Join(", ", raidNames)}</color>");

            List<GameObject> masterSpawnQueue = new List<GameObject>();
            foreach (var group in selectedGroups) masterSpawnQueue.AddRange(group.BuildSpawnQueue());

            ShuffleList(masterSpawnQueue);

            foreach (GameObject enemyPrefab in masterSpawnQueue)
            {
                SpawnEnemy(enemyPrefab);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            }
        }

        // --- STEP 2: SURVIVAL PHASE ---
        yield return new WaitForSeconds(1f); // Buffer for enemies to initialize

        while (GameObject.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length > 0)
        {
            yield return new WaitForSeconds(0.5f); 
        }

        // --- STEP 3: WAVE CLEAR & RESTING ---
        Debug.Log($"Wave {currentWaveIndex + 1} Cleared!");
        GameManager.Instance.AddNutrients(totalNutrientReward);

        // This triggers your resource structures to produce nutrients
        OnWaveCleared?.Invoke();

        waveRunning = false;
        currentWaveIndex++;

        // Tell the GameManager to enter Resting Phase
        GameManager.Instance.EnterRestingPhase();

        if (startWaveButton != null) startWaveButton.interactable = true;

        // --- STEP 4: AUTOMATION ---
        if (autoStartNextWave)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            StartWave();
        }
    }

    // --- SELECTION & UTILS ---

    void SpawnEnemy(GameObject prefab)
    {
        Vector3 spawnPos = GenerateEdgePosition();
        GameObject e = Instantiate(prefab, spawnPos, Quaternion.identity);
        Enemy enemy = e.GetComponent<Enemy>();

        if (enableScaling && enemy != null)
        {
            float multiplier = 1f + (currentWaveIndex * scalingPerWave);
            enemy.ApplyScaling(multiplier);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    List<EnemyRaidGroup> GenerateFreeplayWave(int waveNumber)
    {
        int weightLimit = baseFreeplayWeight + (waveNumber * weightIncreasePerWave);
        int currentWeight = 0;
        List<EnemyRaidGroup> selectedGroups = new List<EnemyRaidGroup>();

        int safety = 0;
        while (currentWeight < weightLimit && safety < 500)
        {
            EnemyRaidGroup randomGroup = GetWeightedRandomGroup();
            if (currentWeight + randomGroup.weightCost > weightLimit)
            {
                safety++;
                continue;
            }
            selectedGroups.Add(randomGroup);
            currentWeight += randomGroup.weightCost;
        }
        return selectedGroups;
    }

    EnemyRaidGroup GetWeightedRandomGroup()
    {
        int totalSelectionWeight = 0;
        foreach (var g in availableGroups) totalSelectionWeight += g.selectionWeight;

        int randomValue = Random.Range(0, totalSelectionWeight);
        int cumulativeWeight = 0;

        foreach (var group in availableGroups)
        {
            cumulativeWeight += group.selectionWeight;
            if (randomValue < cumulativeWeight) return group;
        }
        return availableGroups[0];
    }

    IEnumerator SpawnGroup(SpawnData data)
    {
        List<GameObject> queue = data.BuildSpawnQueue();
        foreach (var prefab in queue)
        {
            SpawnEnemy(prefab);
            yield return new WaitForSeconds(0.1f); 
        }
    }

    Vector3 GenerateEdgePosition()
    {
        Bounds bounds = spawnBounds.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        switch (Random.Range(0, 4))
        {
            case 0: return new Vector3(bounds.min.x - spawnOutsideOffset, y, 0); // Left
            case 1: return new Vector3(bounds.max.x + spawnOutsideOffset, y, 0); // Right
            case 2: return new Vector3(x, bounds.min.y - spawnOutsideOffset, 0); // Bottom
            default: return new Vector3(x, bounds.max.y + spawnOutsideOffset, 0); // Top
        }
    }
}