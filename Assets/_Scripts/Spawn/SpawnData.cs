using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnEntry
{
    public GameObject prefab;
    public int count;
}

public abstract class SpawnData : ScriptableObject
{
    [Header("Spawn Configuration")]
    public List<SpawnEntry> spawnEntries = new List<SpawnEntry>();

    public virtual List<GameObject> BuildSpawnQueue()
    {
        List<GameObject> queue = new List<GameObject>();

        foreach (var entry in spawnEntries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                queue.Add(entry.prefab);
            }
        }

        return queue;
    }
}
