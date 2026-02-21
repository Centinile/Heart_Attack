using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    public List<Enemy> ActiveEnemies = new List<Enemy>();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterEnemy(Enemy e)
    {
        ActiveEnemies.Add(e);
    }

    public void UnregisterEnemy(Enemy e)
    {
        ActiveEnemies.Remove(e);
    }
}
