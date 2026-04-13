using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ProjectileData data;
    
    private Transform target;
    private Vector3 lastTargetPosition; // Tracks where the enemy was
    private float damage;
    private bool hasHit;
    private AttackType attackType;
    private float splashRadius;
    private DefenseTower sourceTower;

    public void Initialize(Transform target, float damage, DefenseTower source, AttackType attackType = AttackType.SingleTarget, float splashRadius = 0f)
    {
        this.target = target;
        this.damage = damage;
        this.sourceTower = source;
        this.attackType = attackType;
        this.splashRadius = splashRadius;
        
        // Initial fallback in case target is destroyed the exact frame it's fired
        if (target != null) lastTargetPosition = target.position;

        Destroy(gameObject, data.Lifetime);
        
        if (data.TrailEffect != null)
        {
            Instantiate(data.TrailEffect, transform.position, Quaternion.identity, transform);
        }
        
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && data.Sprite != null)
        {
            spriteRenderer.sprite = data.Sprite;
        }
    }
    
    void Update()
    {
        if (hasHit) return;

        // 1. Update the last known position while the target still exists
        if (target != null)
        {
            lastTargetPosition = target.position;
        }

        // 2. Move toward the position (either the moving enemy or the ghost of where it was)
        Vector3 direction = (lastTargetPosition - transform.position).normalized;
        
        if (direction != Vector3.zero)
        {
            if (data.Homing) transform.up = direction;
            transform.position += direction * data.Speed * Time.deltaTime;
        }

        // 3. Check for arrival
        float distance = Vector2.Distance(transform.position, lastTargetPosition);
        if (distance < 0.2f) // Reduced threshold for better accuracy
        {
            Hit();
        }
    }
    
    void Hit()
    {
        if (hasHit) return;
        hasHit = true;
        
        // If the target is gone, we can still do splash damage at the location
        if (attackType == AttackType.SplashDamage)
        {
            ApplySplashDamage();
        }
        else if (target != null) // Single target damage only if enemy still exists
        {
            ApplyDamage(target);
        }
        
        if (data.ImpactEffect != null)
        {
            Instantiate(data.ImpactEffect, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
    
    void ApplyDamage(Transform targetTransform)
    {
        if (targetTransform == null) return;

        Enemy enemy = targetTransform.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
    }
    
    void ApplySplashDamage()
    {
        // Use the projectile's current position (where the enemy died) for the blast
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, splashRadius);
        float splashDamage = damage * 0.5f;
        
        foreach (Collider2D col in nearby)
        {
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                // If it's the original target, give full damage; others get splash
                float finalDamage = (col.transform == target) ? damage : splashDamage;
                enemy.TakeDamage(finalDamage);
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
        // Optional: Colliding with ANY enemy on the way triggers the hit early
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            // If it's a homing projectile, we might only want it to hit the assigned target
            // If it's a "dumb" projectile, hitting anything is fine.
            if (other.transform == target || !data.Homing)
            {
                Hit();
            }
        }
    }
}