using UnityEngine;

/// <summary>
/// An ability that triggers when the enemy spawns, granting a temporary speed boost.
/// The enemy starts with increased movement speed.
/// </summary>
[CreateAssetMenu(
    menuName = "Abilities/On Spawn/Speed Boost",
    fileName = "SpawnSpeedBoost_",
    order = 300)]
public class SpawnSpeedBoostAbility : AbilityBase
{
    [Header("Boost Settings")]
    [Tooltip("Speed multiplier applied at spawn.")]
    [SerializeField] private float speedMultiplier = 1.5f;
    
    [Tooltip("Duration of the speed boost in seconds.")]
    [SerializeField] private float boostDuration = 2f;
    
    public SpawnSpeedBoostAbility()
    {
        abilityName = "Spawn Speed Boost";
        cooldown = 0f;
        triggerOnSpawn = true;
    }
    
    protected override void ExecuteAbility(IEnemy user)
    {
        if (user == null) return;
        
        // Apply speed boost
        user.ModifySpeed(speedMultiplier, boostDuration);
        
        Debug.Log($"SpawnSpeedBoostAbility: Applied {speedMultiplier}x speed for {boostDuration} seconds");
    }
}