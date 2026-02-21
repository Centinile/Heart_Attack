using UnityEngine;

// <summary>
// Runtime projectile component spawned by towers.
// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ProjectileData data;
    
    private Transform target;
    private float damage;
    private bool hasHit;
    private AttackType attackType;
    private float splashRadius;
    private DefenseTower sourceTower;
    

    // Initializes the projectile with its data and target.
    public void Initialize(Transform target, float damage, DefenseTower source, AttackType attackType = AttackType.SingleTarget, float splashRadius = 0f)
    {
        this.target = target;
        this.damage = damage;
        this.sourceTower = source;
        this.attackType = attackType;
        this.splashRadius = splashRadius;
        
        // Destroy after lifetime
        Destroy(gameObject, data.Lifetime);
        
        // Spawn trail effect
        if (data.TrailEffect != null)
        {
            Instantiate(data.TrailEffect, transform.position, Quaternion.identity, transform);
        }
        
        // Set sprite
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && data.Sprite != null)
        {
            spriteRenderer.sprite = data.Sprite;
        }
    }
    
    void Update()
    {
        if (hasHit || target == null) return;
        
        // Move towards target
        Vector3 direction = (target.position - transform.position).normalized;
        
        // Only rotate the projectile, not the tower
        if (data.Homing)
        {
            if (direction != Vector3.zero)
            {
                transform.up = direction;
            }
        }
        
        transform.position += direction * data.Speed * Time.deltaTime;
        
        // Check for hit based on distance
        float distance = Vector2.Distance(transform.position, target.position);
        if (distance < 0.5f)
        {
            Hit();
        }
    }
    
    void Hit()
    {
        if (hasHit) return;
        hasHit = true;
        
        switch (attackType)
        {
            case AttackType.SingleTarget:
                ApplyDamage(target);
                break;
                
            case AttackType.SplashDamage:
                ApplySplashDamage();
                break;
                
            case AttackType.MultiTarget:
            case AttackType.AllInRange:
                // These are handled by the tower
                break;
        }
        
        // Spawn impact effect
        if (data.ImpactEffect != null)
        {
            Instantiate(data.ImpactEffect, transform.position, Quaternion.identity);
        }
        
        Destroy(gameObject);
    }
    
    void ApplyDamage(Transform target)
    {
        // Check for Enemy component (not Building)
        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Debug.Log($"Projectile hit {target.name} for {damage} damage!");
        }
        else
        {
            Debug.LogWarning($"Target {target.name} has no Enemy component!");
        }
    }
    
    void ApplySplashDamage()
    {
        // Apply direct damage to main target
        ApplyDamage(target);
        
        // Apply reduced damage to nearby targets
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, splashRadius);
        float splashDamage = damage * 0.5f;
        
        foreach (Collider2D col in nearby)
        {
            if (col.transform == target) continue;
            
            Enemy enemy = col.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(splashDamage);
            }
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        
        // Check if we hit an enemy
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            Hit();
        }
    }
}