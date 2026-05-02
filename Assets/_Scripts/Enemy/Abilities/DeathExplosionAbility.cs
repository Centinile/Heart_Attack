using UnityEngine;

[CreateAssetMenu(
    menuName = "Abilities/On Death/Explosion",
    fileName = "DeathExplosion_",
    order = 200)]
public class DeathExplosionAbility : AbilityBase
{
    [Header("Explosion Settings")]
    [Tooltip("Radius of the explosion.")]
    [SerializeField] private float explosionRadius = 3f;

    [Header("Visual Effects")]
    [Tooltip("Particle effect to spawn at death location.")]
    [SerializeField] private GameObject explosionEffect;

    [Tooltip("Sound to play when exploding.")]
    [SerializeField] private AudioClip explosionSound;

    public DeathExplosionAbility()
    {
        abilityName = "Death Explosion";
        cooldown = 0f;
        triggerOnDeath = true;
        preventDeath = false;
    }

    protected override void ExecuteAbility(IEnemy user)
    {
        if (user == null) return;

        Vector3 explosionCenter = user.transform.position;

        // 1. Get the enemy's actual current damage (scaled or base)
        // Note: Make sure scaledDamage in Enemy.cs is not private, or add a public getter
        float damageToApply = user.Data.AttackDamage; 

        // 2. Scan for ALL colliders in radius (No LayerMask used)
        Collider2D[] hits = Physics2D.OverlapCircleAll(explosionCenter, explosionRadius);

        // Track damaged buildings to avoid double-hitting (if one building has multiple colliders)
        System.Collections.Generic.HashSet<Building> damagedBuildings = new System.Collections.Generic.HashSet<Building>();

        foreach (Collider2D hit in hits)
        {
            // 3. Same logic as your enemy's regular attack
            Building building = hit.GetComponentInParent<Building>();
            
            if (building != null && !damagedBuildings.Contains(building))
            {
                building.TakeDamage(damageToApply);
                damagedBuildings.Add(building);

                // Optional: Trigger any on-attack abilities for each building hit
                if (user.Data != null)
                    user.Data.TriggerAttackAbilities(user, building);
            }
        }

        // --- Visuals and Audio ---
        if (explosionEffect != null)
            Instantiate(explosionEffect, explosionCenter, Quaternion.identity);

        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, explosionCenter);

        Debug.Log($"Death Explosion: Dealt {damageToApply} damage to {damagedBuildings.Count} unique buildings.");
    }
}