using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static System.Action OnWaveCleared;

    [Header("Wave Setup")]
    public WaveData[] waves;
    public Button startWaveButton;
    public TMP_Text waveText; // ADD: assign in inspector
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

    [Header("Freeplay")]
    public bool freeplayMode = false;
    public EnemyRaidGroup[] availableGroups;
    public int baseFreeplayWeight = 50;
    public int weightIncreasePerWave = 5;

    private void Start()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.AddListener(StartWave);

        freeplayMode      = GameManager.Instance.enableRandomWaves;
        autoStartNextWave = GameManager.Instance.enableNoBreaks;
        enableScaling     = GameManager.Instance.enableStatRamping;

        UpdateWaveText();
    }

    private void UpdateWaveText()
    {
        if (waveText == null) return;

        int displayWave = currentWaveIndex + 1;

        if (waveRunning)
        {
            bool isFreeplay = freeplayMode || currentWaveIndex >= waves.Length;
            waveText.text = isFreeplay
                ? $"Wave {displayWave} (Freeplay)"
                : $"Wave {displayWave}";
        }
        else
        {
            // Resting phase — show upcoming wave
            waveText.text = $"Next: Wave {displayWave}";
        }
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
        GameManager.Instance.EnterGameplayPhase();

        if (startWaveButton != null) startWaveButton.interactable = false;

        if (currentWaveIndex >= waves.Length)
            freeplayMode = true;

        UpdateWaveText(); // Show current wave immediately

        string modeColor = freeplayMode ? "orange" : "cyan";
        Debug.Log($"<color={modeColor}><b>[WAVE {currentWaveIndex + 1}]</b> STARTED ({(freeplayMode ? "FREEPLAY" : "DESIGNED")})</color>");

        float totalNutrientReward = 0;

        // ── STEP 1: SPAWNING ──────────────────────────────────────────
        if (!freeplayMode)
        {
            WaveData currentWaveData = waves[currentWaveIndex];
            totalNutrientReward = currentWaveData.NutrientReward;
            yield return StartCoroutine(SpawnGroup(currentWaveData));
        }
        else
        {
            List<EnemyRaidGroup> selectedGroups = GenerateFreeplayWave(currentWaveIndex);
            foreach (var group in selectedGroups) totalNutrientReward += group.NutrientReward;

            List<string> raidNames = new List<string>();
            foreach (var g in selectedGroups)
                raidNames.Add(string.IsNullOrEmpty(g.raidName) ? g.name : g.raidName);
            Debug.Log($"<color=orange><b>[FREEPLAY WAVE {currentWaveIndex + 1}]</b> Raids: {string.Join(", ", raidNames)}</color>");

            List<GameObject> masterSpawnQueue = new List<GameObject>();
            foreach (var group in selectedGroups) masterSpawnQueue.AddRange(group.BuildSpawnQueue());
            ShuffleList(masterSpawnQueue);

            foreach (GameObject enemyPrefab in masterSpawnQueue)
            {
                SpawnEnemy(enemyPrefab);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            }
        }

        // ── STEP 2: SURVIVAL ──────────────────────────────────────────
        yield return new WaitForSeconds(1f);

        while (GameObject.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Length > 0)
            yield return new WaitForSeconds(0.5f);

        // ── STEP 3: WAVE CLEAR ────────────────────────────────────────
        Debug.Log($"Wave {currentWaveIndex + 1} Cleared!");
        GameManager.Instance.AddNutrients(totalNutrientReward);

        OnWaveCleared?.Invoke();

        waveRunning = false;
        currentWaveIndex++;

        GameManager.Instance.CheckVictory(currentWaveIndex);
        if (GameManager.Instance.currentState == GameManager.GameState.Victory) yield break;

        if (currentWaveIndex >= waves.Length)
            freeplayMode = true;

        // ── STEP 4: RESTING ───────────────────────────────────────────
        GameManager.Instance.EnterRestingPhase();

        if (startWaveButton != null) startWaveButton.interactable = true;

        UpdateWaveText(); // Show next wave during rest

        if (autoStartNextWave)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            StartWave();
        }
    }

    // ── Spawning ───────────────────────────────────────────────────────

    void SpawnEnemy(GameObject prefab)
    {
        Vector3 spawnPos = GenerateEdgePosition();
        GameObject e = Instantiate(prefab, spawnPos, Quaternion.identity);
        EnemyBrain enemy = e.GetComponent<EnemyBrain>();
        if (enableScaling && enemy != null)
        {
            float multiplier = 1f + (currentWaveIndex * scalingPerWave);
            enemy.ApplyScaling(multiplier);
        }
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

    // Spawns strictly ON the polygon edge, not inside or outside
    Vector3 GenerateEdgePosition()
    {
        Vector2[] points = spawnBounds.points;
        Vector2 offset = spawnBounds.transform.position;

        // Build a list of edge segments with their lengths
        // so we can pick a random point weighted by segment length
        float totalLength = 0f;
        float[] segmentLengths = new float[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i] + offset;
            Vector2 b = points[(i + 1) % points.Length] + offset;
            segmentLengths[i] = Vector2.Distance(a, b);
            totalLength += segmentLengths[i];
        }

        // Pick a random distance along the total perimeter
        float randomDist = Random.Range(0f, totalLength);
        float cumulative = 0f;

        for (int i = 0; i < points.Length; i++)
        {
            cumulative += segmentLengths[i];
            if (randomDist <= cumulative)
            {
                // Interpolate along this segment
                Vector2 a = points[i] + offset;
                Vector2 b = points[(i + 1) % points.Length] + offset;
                float t = 1f - (cumulative - randomDist) / segmentLengths[i];
                Vector2 spawnPos = Vector2.Lerp(a, b, t);
                return new Vector3(spawnPos.x, spawnPos.y, 0f);
            }
        }

        // Fallback
        Vector2 fallback = points[0] + offset;
        return new Vector3(fallback.x, fallback.y, 0f);
    }

    // ── Freeplay ───────────────────────────────────────────────────────

    List<EnemyRaidGroup> GenerateFreeplayWave(int waveNumber)
    {
        int weightLimit = baseFreeplayWeight + (waveNumber * weightIncreasePerWave);
        int currentWeight = 0;
        List<EnemyRaidGroup> selectedGroups = new List<EnemyRaidGroup>();
        int safety = 0;

        while (currentWeight < weightLimit && safety < 500)
        {
            EnemyRaidGroup randomGroup = GetWeightedRandomGroup();
            if (currentWeight + randomGroup.weightCost > weightLimit) { safety++; continue; }
            selectedGroups.Add(randomGroup);
            currentWeight += randomGroup.weightCost;
        }
        return selectedGroups;
    }

    EnemyRaidGroup GetWeightedRandomGroup()
    {
        int total = 0;
        foreach (var g in availableGroups) total += g.selectionWeight;
        int random = Random.Range(0, total);
        int cumulative = 0;
        foreach (var group in availableGroups)
        {
            cumulative += group.selectionWeight;
            if (random < cumulative) return group;
        }
        return availableGroups[0];
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
}