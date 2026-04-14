using UnityEngine;

/// <summary>
/// A passive ability that damages nearby buildings continuously.
/// Creates an aura of damage around the enemy.
/// </summary>
[CreateAssetMenu(
    menuName = "Abilities/Passive/Damage Aura",
    fileName = "DamageAura_",
    order = 100)]
public class DamageAuraAbility : AbilityBase
{
    [Header("Aura Settings")]
    [Tooltip("Radius of the damage aura.")]
    [SerializeField] private float auraRadius = 2f;
    
    [Tooltip("Damage dealt per second to buildings in the aura.")]
    [SerializeField] private float damagePerSecond = 5f;
    
    [Tooltip("Layer mask for valid targets.")]
    [SerializeField] private LayerMask targetLayers;
    
    [Header("Visual Feedback")]
    [Tooltip("Optional: Particle effect to display around the enemy.")]
    [SerializeField] private GameObject auraEffect;
    
    private GameObject spawnedEffect;
    
    public DamageAuraAbility()
    {
        abilityName = "Damage Aura";
        cooldown = 1f; // Damage tick interval
        isPassive = true;
        targetLayers = LayerMask.GetMask("Defense", "Wall", "Resource");
    }
    
    protected override void ExecuteAbility(Enemy user)
    {
        if (user == null) return;
        
        // Find all colliders in the aura area
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            user.transform.position,
            auraRadius,
            targetLayers
        );
        
        // Apply damage to each hit building
        float damageThisTick = damagePerSecond * (cooldown > 0 ? cooldown : Time.deltaTime);
        
        foreach (Collider2D hit in hits)
        {
            Building building = hit.GetComponent<Building>();
            if (building != null)
            {
                building.TakeDamage(damageThisTick);
            }
        }
    }
    
    protected override void OnAbilityAssigned(Enemy user)
    {
        base.OnAbilityAssigned(user);
        
        // Spawn visual effect
        if (auraEffect != null && user != null)
        {
            spawnedEffect = Instantiate(auraEffect, user.transform);
        }
    }
    
    protected override void OnAbilityRemoved()
    {
        if (spawnedEffect != null)
        {
            Destroy(spawnedEffect);
        }
        
        base.OnAbilityRemoved();
    }
}