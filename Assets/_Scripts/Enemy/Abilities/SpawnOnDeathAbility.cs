using UnityEngine;

[CreateAssetMenu(
    menuName = "Abilities/On Death/Spawn Enemy",
    fileName = "SpawnOnDeath_",
    order = 201)]
public class SpawnOnDeathAbility : AbilityBase
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int spawnCount = 1;
    [SerializeField] private float spawnRadius = 0.5f;

    public SpawnOnDeathAbility()
    {
        abilityName = "Spawn On Death";
        cooldown = 0f;
        triggerOnDeath = true;
        preventDeath = false;
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

        Debug.Log($"[SpawnOnDeath] Spawned {spawnCount}x {enemyPrefab.name}");
    }
}