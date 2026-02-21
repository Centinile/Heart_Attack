using UnityEngine;

/// <summary>
/// An ability that triggers when the enemy dies, dealing damage to nearby buildings.
/// The enemy explodes on death.
/// </summary>
[CreateAssetMenu(
    menuName = "Abilities/On Death/Explosion",
    fileName = "DeathExplosion_",
    order = 200)]
public class DeathExplosionAbility : AbilityBase
{
    [Header("Explosion Settings")]
    [Tooltip("Radius of the explosion.")]
    [SerializeField] private float explosionRadius = 3f;
    
    [Tooltip("Damage dealt to buildings in the explosion area.")]
    [SerializeField] private float explosionDamage = 50f;
    
    [Tooltip("Layer mask for valid targets.")]
    [SerializeField] private LayerMask targetLayers;
    
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
        targetLayers = LayerMask.GetMask("Defense", "Wall", "Resource");
    }
    
    protected override void ExecuteAbility(Enemy user)
    {
        if (user == null) return;
        
        Vector3 explosionCenter = user.transform.position;
        
        // Find all buildings in explosion radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            explosionCenter,
            explosionRadius,
            targetLayers
        );
        
        // Apply damage to each hit building
        foreach (Collider2D hit in hits)
        {
            Building building = hit.GetComponent<Building>();
            if (building != null)
            {
                building.TakeDamage(explosionDamage);
            }
        }
        
        // Spawn visual effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, explosionCenter, Quaternion.identity);
        }
        
        // Play sound effect
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, explosionCenter);
        }
        
        Debug.Log($"DeathExplosionAbility: Exploded dealing {explosionDamage} damage to {hits.Length} buildings");
    }
}