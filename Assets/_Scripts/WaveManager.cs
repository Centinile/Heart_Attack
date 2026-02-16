using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class WaveData
{
    public float duration = 10f;
    public int easyEnemies = 5;
    public int hardEnemies = 2;
}

public class WaveManager : MonoBehaviour
{
    public WaveData[] waves;
    public Button startWaveButton;

    public GameObject easyEnemyPrefab;
    public GameObject hardEnemyPrefab;

    public Transform[] wayPoints;
    public int currentWaveIndex;
    public bool waveRunning = false;

    void Start()
    {
        startWaveButton.onClick.AddListener(StartWave);
    }

    public void StartWave()
    {
        if(waveRunning == true) return;
        if(currentWaveIndex >= waves.Length) return;

        StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        waveRunning = true;
        startWaveButton.interactable = false;

        WaveData wave = waves[currentWaveIndex];

        for(int i = 0; i < wave.easyEnemies; i++)
        {
            SpawnEnemy(easyEnemyPrefab);
            yield return new WaitForSeconds((wave.duration / 3) / wave.easyEnemies);
        }

        for(int i = 0; i < wave.easyEnemies; i++)
        {
            SpawnEnemy(hardEnemyPrefab);
            yield return new WaitForSeconds((wave.duration / 3) / wave.hardEnemies);
        }

        yield return new WaitForSeconds(wave.duration / 3);
        waveRunning = false;
        startWaveButton.interactable = true;
        currentWaveIndex++;

    }

    void SpawnEnemy(GameObject prefab)
    {
        GameObject e = Instantiate(prefab, wayPoints[0].position, Quaternion.identity);
        Enemy enemy = e.GetComponent<Enemy>();
        enemy.wayPoints = wayPoints;
    }
}
