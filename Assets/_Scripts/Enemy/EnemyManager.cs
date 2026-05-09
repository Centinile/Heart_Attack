using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    public List<EnemyBrain> ActiveEnemies = new List<EnemyBrain>();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterEnemy(EnemyBrain e)
    {
        ActiveEnemies.Add(e);
    }

    public void UnregisterEnemy(EnemyBrain e)
    {
        ActiveEnemies.Remove(e);
    }
}
