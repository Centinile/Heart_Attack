using UnityEngine;
using System.Collections.Generic;

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

    [Header("Visual Feedback")]
    [Tooltip("Particle effect to display around the enemy.")]
    [SerializeField] private GameObject auraEffect;
    
    private GameObject spawnedEffect;

    public DamageAuraAbility()
    {
        abilityName = "Damage Aura";
        cooldown = 1f; // Damage tick interval
        isPassive = true;
    }
    
    protected override void ExecuteAbility(IEnemy user)
    {
        if (user == null) return;
        
        // 1. Same logic as Explosion: Scan everything in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(user.transform.position, auraRadius);
        
        // 2. Calculate damage based on the tick interval (cooldown)
        float damageThisTick = damagePerSecond * (cooldown > 0 ? cooldown : Time.deltaTime);
        
        // 3. Track unique buildings to prevent multiple-collider damage bugs
        HashSet<Building> damagedBuildings = new HashSet<Building>();

        foreach (Collider2D hit in hits)
        {
            // Use GetComponentInParent to match your Enemy's TryAttack logic
            Building building = hit.GetComponentInParent<Building>();
            
            if (building != null && !damagedBuildings.Contains(building))
            {
                building.TakeDamage(damageThisTick);
                damagedBuildings.Add(building);
            }
        }
        Debug.Log($"Aura Damage: Dealt {damageThisTick} damage to {damagedBuildings.Count} unique buildings.");
    }
    
    protected override void OnAbilityAssigned(IEnemy user)
    {
        base.OnAbilityAssigned(user);
        
        // Spawn visual effect as a child so it follows the enemy
        if (auraEffect != null && user != null)
        {
            spawnedEffect = Instantiate(auraEffect, user.transform);
        }
    }
    
    protected override void OnAbilityRemoved()
    {
        // Cleanup the visual effect when the enemy/ability is destroyed
        if (spawnedEffect != null)
        {
            Destroy(spawnedEffect);
        }
        
        base.OnAbilityRemoved();
    }
}