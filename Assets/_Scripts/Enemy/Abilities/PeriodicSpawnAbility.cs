using UnityEngine;

[CreateAssetMenu(
    menuName = "Abilities/Passive/Periodic Spawn",
    fileName = "PeriodicSpawn_",
    order = 400)]
public class PeriodicSpawnAbility : AbilityBase
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int spawnCount = 1;
    [SerializeField] private float spawnRadius = 0.5f;

    public PeriodicSpawnAbility()
    {
        abilityName = "Periodic Spawn";
        cooldown = 5f;
        isPassive = true;
    }

    protected override void ExecuteAbility(IEnemy user)
    {
        if (user == null || enemyPrefab == null) return;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = user.transform.position + new Vector3(offset.x, offset.y, 0f);
            Object.Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"[PeriodicSpawn] Spawned {spawnCount}x {enemyPrefab.name}");
    }
}